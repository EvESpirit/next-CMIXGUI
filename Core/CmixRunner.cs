using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace nextCMIXGUI_WinUI.Core
{
    public class RunConfig
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string Action { get; set; } = "Compress";
        public bool UseDict { get; set; } = true;
        public string VersionKey { get; set; } = "";
        public bool ShowCmd { get; set; } = false;
        public long InputFileSize { get; set; } = 0;
        public long PretrainingFileSize { get; set; } = 0;
    }

    public class ProgressData
    {
        public string Type { get; set; } // "pretrain" or "main"
        public double Percent { get; set; }
        public double Speed { get; set; }
        public double Eta { get; set; }
        public double PretrainFinishedTime { get; set; }
    }

    public class CmixRunner
    {
        public event Action<string, string> OnLog;
        public event Action<ProgressData> OnProgress;
        public event Action<bool, long> OnFinish; // bool wasCancelled, long outputFileSize

        private Process _currentProcess;
        private CancellationTokenSource _cts;
        private long _taskStartTimeTicks;
        private long _mainProgressStartTimeTicks;
        private double _pretrainSmoothedSpeed;
        private double _mainSmoothedSpeed;
        private const double EMA_ALPHA = 0.4;
        private RunConfig _config;
        private static readonly object _logLock = new object();

        public async Task RunAsync(RunConfig config)
        {
            _config = config;
            _cts = new CancellationTokenSource();
            _taskStartTimeTicks = Stopwatch.GetTimestamp();
            _mainProgressStartTimeTicks = 0;
            _pretrainSmoothedSpeed = 0.0;
            _mainSmoothedSpeed = 0.0;

            await Task.Run(() => ProcessingThread(_cts.Token));
        }

        public void Cancel()
        {
            Log("Cancellation requested by user.", "WARN");
            _cts?.Cancel();
            try
            {
                _currentProcess?.Kill(true);
            }
            catch (Exception ex)
            {
                Log($"Could not terminate process: {ex.Message}", "ERROR");
            }
        }

        private void Log(string message, string level = "INFO")
        {
            try
            {
                lock (_logLock)
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cmix_debug.log"), $"[{level}] {message}\n");
                }
            }
            catch { }
            OnLog?.Invoke(message, level);
        }

        private void ProcessingThread(CancellationToken token)
        {
            string actionFlag = _config.Action switch
            {
                "Extract" => "-d",
                "Preprocess" => "-s",
                _ => "-c"
            };

            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exes", _config.VersionKey, "cmix.exe");
            
            // In case running from debugger where 'exes' isn't in bindir
            if (!File.Exists(exePath))
            {
                // Fallback to searching up exactly as python script does
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "exes")))
                {
                    dir = dir.Parent;
                }
                if (dir != null) exePath = Path.Combine(dir.FullName, "exes", _config.VersionKey, "cmix.exe");
            }

            string arguments = $"{actionFlag} ";
            if (_config.UseDict && _config.Action != "Extract")
            {
                arguments += "english.dic ";
            }
            arguments += $"\"{_config.InputPath}\" \"{_config.OutputPath}\"";

            long outputSize = 0;

            try
            {
                _currentProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = _config.ShowCmd,
                        CreateNoWindow = !_config.ShowCmd,
                        RedirectStandardOutput = !_config.ShowCmd,
                        RedirectStandardError = !_config.ShowCmd,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    }
                };

                Log($"Started CMIX process: {exePath} {arguments}", "INFO");

                if (!_config.ShowCmd)
                {
                    _currentProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) Log(e.Data, "ERROR");
                    };
                }

                _currentProcess.Start();

                Task outputTask = null;
                Task errorTask = null;
                if (!_config.ShowCmd)
                {
                    outputTask = Task.Run(() => ReadOutputAsync(_currentProcess.StandardOutput, token));
                    errorTask = Task.Run(() => ReadOutputAsync(_currentProcess.StandardError, token));
                }

                _currentProcess.WaitForExit();
                outputTask?.Wait();
                errorTask?.Wait();

                if (!token.IsCancellationRequested)
                {
                    if (_currentProcess.ExitCode == 0)
                    {
                        if (File.Exists(_config.OutputPath))
                        {
                            outputSize = new FileInfo(_config.OutputPath).Length;
                        }
                    }
                    else
                    {
                        Log($"Process exited with error code {_currentProcess.ExitCode}", "ERROR");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    Log($"Error during processing: {ex.Message}", "ERROR");
            }
            finally
            {
                _currentProcess = null;
                OnFinish?.Invoke(token.IsCancellationRequested, outputSize);
            }
        }

        private async Task ReadOutputAsync(StreamReader stream, CancellationToken token)
        {
            var buffer = new char[1024];
            var sb = new System.Text.StringBuilder();
            
            while (!stream.EndOfStream && !token.IsCancellationRequested)
            {
                try 
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) break;

                    for (int i = 0; i < read; i++)
                    {
                        char c = buffer[i];
                        if (c == '\r' || c == '\n')
                        {
                            if (sb.Length > 0)
                            {
                                try { ParseCmixOutput(sb.ToString()); } catch { }
                                sb.Clear();
                            }
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }
                }
                catch { break; }
            }
            
            if (sb.Length > 0 && !token.IsCancellationRequested)
            {
                try { ParseCmixOutput(sb.ToString()); } catch { }
            }
        }

        private void ParseCmixOutput(string line)
        {
            Log(line);
            if (string.IsNullOrWhiteSpace(line)) return;
            string cleaned = line.Trim().ToLowerInvariant();

            var pretrainMatch = Regex.Match(cleaned, @"pretraining:\s*([\d\.]+)%");
            if (pretrainMatch.Success)
            {
                double percent = double.Parse(pretrainMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                double elapsed = Stopwatch.GetElapsedTime(_taskStartTimeTicks).TotalSeconds;
                double speed = 0, eta = 0;

                if (elapsed > 0.5 && percent > 0.1 && _config.PretrainingFileSize > 0)
                {
                    double bytesDone = (percent / 100.0) * _config.PretrainingFileSize;
                    double instantSpeed = bytesDone / elapsed;
                    _pretrainSmoothedSpeed = _pretrainSmoothedSpeed == 0.0 ? instantSpeed : (instantSpeed * EMA_ALPHA) + (_pretrainSmoothedSpeed * (1 - EMA_ALPHA));
                    double remainingBytes = _config.PretrainingFileSize - bytesDone;
                    eta = _pretrainSmoothedSpeed > 1 ? remainingBytes / _pretrainSmoothedSpeed : double.PositiveInfinity;
                    speed = _pretrainSmoothedSpeed;
                }

                OnProgress?.Invoke(new ProgressData { Type = "pretrain", Percent = percent, Speed = speed, Eta = eta });
            }

            var progressMatch = Regex.Match(cleaned, @"progress:\s*([\d\.]+)%");
            if (progressMatch.Success)
            {
                double percent = double.Parse(progressMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                
                double pretrainFinishedTime = 0;
                if (_mainProgressStartTimeTicks == 0)
                {
                    _mainProgressStartTimeTicks = Stopwatch.GetTimestamp();
                }
                pretrainFinishedTime = Stopwatch.GetElapsedTime(_taskStartTimeTicks, _mainProgressStartTimeTicks).TotalSeconds;

                double elapsed = Stopwatch.GetElapsedTime(_mainProgressStartTimeTicks).TotalSeconds;
                double speed = 0, eta = 0;

                if (elapsed > 0.5 && percent > 0.1 && _config.InputFileSize > 0)
                {
                    double bytesDone = (percent / 100.0) * _config.InputFileSize;
                    double instantSpeed = bytesDone / elapsed;
                    _mainSmoothedSpeed = _mainSmoothedSpeed == 0.0 ? instantSpeed : (instantSpeed * EMA_ALPHA) + (_mainSmoothedSpeed * (1 - EMA_ALPHA));
                    double remainingBytes = _config.InputFileSize - bytesDone;
                    eta = _mainSmoothedSpeed > 1 ? remainingBytes / _mainSmoothedSpeed : double.PositiveInfinity;
                    speed = _mainSmoothedSpeed;
                }

                OnProgress?.Invoke(new ProgressData { Type = "main", Percent = percent, Speed = speed, Eta = eta, PretrainFinishedTime = pretrainFinishedTime });
            }
        }
    }
}
