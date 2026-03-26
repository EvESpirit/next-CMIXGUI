using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextCmixGui.Core;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;

namespace NextCmixGui.Views
{
    public sealed partial class MainPage : Page
    {
        private AppSettings _settings;
        private CmixRunner _runner;
        private bool _isProcessing = false;
        
        private string _previousInput = "";
        private string _previousOutput = "";

        public MainPage()
        {
            this.InitializeComponent();
            _settings = ConfigManager.LoadSettings();
            ApplyLanguage();
            LoadVersions();
            LoadSettings();
        }

        private void SaveSettings()
        {
            ConfigManager.SaveSettings(_settings);
        }

        private void ApplyLanguage()
        {
            lblInput.Text = Translations.Get("input_file_folder");
            lblOutput.Text = Translations.Get("output_file");
            lblVersion.Text = Translations.Get("cmix_version");
            lblAction.Text = Translations.Get("action");
            radCompress.Content = Translations.Get("compress");
            radExtract.Content = Translations.Get("extract");
            radPreprocess.Content = Translations.Get("preprocess");
            chkUseDict.Content = Translations.Get("use_eng_dict");
            lblPlaceholder.Text = Translations.Get("input_file_folder");
            btnStart.Content = Translations.Get("start");
            btnCancel.Content = "Cancel"; 
            lblCancelled.Text = "TASK CANCELLED";
        }

        private void LoadVersions()
        {
            var exeDir = Path.Combine(AppContext.BaseDirectory, "exes");
            if (Directory.Exists(exeDir))
            {
                var versions = Directory.GetDirectories(exeDir)
                                        .Select(Path.GetFileName)
                                        .Where(d => File.Exists(Path.Combine(exeDir, d, "cmix.exe")))
                                        .OrderByDescending(d => int.TryParse(d, out int v) ? v : 0)
                                        .ToList();
                
                cmbVersion.ItemsSource = versions;

                if (versions.Contains(_settings.Version))
                    cmbVersion.SelectedItem = _settings.Version;
                else if (versions.Count > 0)
                    cmbVersion.SelectedIndex = 0;
            }
        }

        private void LoadSettings()
        {
            radLangEn.IsChecked = _settings.Language == "English";
            radLangEs.IsChecked = _settings.Language == "es";
            
            radCompress.IsChecked = _settings.Action == "Compress";
            radExtract.IsChecked = _settings.Action == "Extract";
            radPreprocess.IsChecked = _settings.Action == "Preprocess";

            chkUseDict.IsChecked = _settings.UseDict;
            chkShowCmd.IsChecked = _settings.ShowCmd;

            txtInput.Text = _previousInput;
            txtOutput.Text = _previousOutput;
        }

        private void OnLanguageChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            
            if (radLangEs.IsChecked == true)
            {
                Translations.SetLanguage("Spanish");
                _settings.Language = "es";
            }
            else
            {
                Translations.SetLanguage("English");
                _settings.Language = "English";
            }
            SaveSettings();
            ApplyLanguage();
        }

        private void OnActionChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            
            if (radCompress.IsChecked == true) _settings.Action = "Compress";
            else if (radExtract.IsChecked == true) _settings.Action = "Extract";
            else if (radPreprocess.IsChecked == true) _settings.Action = "Preprocess";
            SaveSettings();
        }

        private void OnOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            
            _settings.UseDict = chkUseDict.IsChecked == true;
            _settings.ShowCmd = chkShowCmd.IsChecked == true;
            SaveSettings();
        }

        private void OnVersionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            
            if (cmbVersion.SelectedItem is string version)
            {
                _settings.Version = version;
                SaveSettings();
            }
        }

        private void OnInputPathChanged(object sender, TextChangedEventArgs e)
        {
            if (_settings == null) return;
            
            _previousInput = txtInput.Text;
            
            if (string.IsNullOrWhiteSpace(txtOutput.Text) && !string.IsNullOrWhiteSpace(txtInput.Text))
            {
                string input = txtInput.Text;
                try
                {
                    if (File.Exists(input))
                    {
                        if (_settings.Action == "Compress" || _settings.Action == "Preprocess")
                        {
                            txtOutput.Text = input + ".cmix";
                        }
                        else if (_settings.Action == "Extract" && input.EndsWith(".cmix", StringComparison.OrdinalIgnoreCase))
                        {
                            txtOutput.Text = input.Substring(0, input.Length - 5);
                        }
                    }
                    else if (Directory.Exists(input))
                    {
                        txtOutput.Text = input + ".cmix";
                    }
                }
                catch { }
            }
        }

        private async void OnBrowseInput(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                txtInput.Text = file.Path;
            }
        }

        private async void OnBrowseOutput(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            var window = (Application.Current as App)?.MainWindow;
            if (window != null)
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeChoices.Add("CMIX Archive", new List<string>() { ".cmix" });
            picker.FileTypeChoices.Add("All Files", new List<string>() { "." });

            if (!string.IsNullOrEmpty(txtInput.Text))
            {
                picker.SuggestedFileName = Path.GetFileName(txtInput.Text) + ".cmix";
            }

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                txtOutput.Text = file.Path;
                _previousOutput = file.Path;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    txtInput.Text = items[0].Path;
                }
            }
        }

        private void OnStartClicked(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            if (string.IsNullOrWhiteSpace(txtInput.Text) || string.IsNullOrWhiteSpace(txtOutput.Text)) return;
            if (cmbVersion.SelectedItem == null) return;
            
            _previousInput = txtInput.Text;
            _previousOutput = txtOutput.Text;

            string exePath = Path.Combine(AppContext.BaseDirectory, "exes", cmbVersion.SelectedItem.ToString(), "cmix.exe");
            if (!File.Exists(exePath)) return;

            string options = "";
            if (_settings.UseDict) options += "-d english.dic ";

            _isProcessing = true;
            btnStart.IsEnabled = false;
            btnCancel.IsEnabled = true;
            
            lblPlaceholder.Visibility = Visibility.Collapsed;
            borderCancelled.Visibility = Visibility.Collapsed;
            lblRatio.Visibility = Visibility.Collapsed;
            gridProgress.Visibility = Visibility.Visible;

            barPretrain.Value = 0;
            lblPretrainPercent.Text = "0.00%";
            lblPretrainStats.Text = "Wait...";

            barMain.Value = 0;
            lblMainPercent.Text = "0.00%";
            lblMainStats.Text = "Wait...";

            lblInputName.Text = "Name: " + Path.GetFileName(txtInput.Text);
            lblInputPath.Text = "Path: " + txtInput.Text;
            lblOutputName.Text = "Name: " + Path.GetFileName(txtOutput.Text);
            lblOutputPath.Text = "Path: " + txtOutput.Text;

            _runner = new CmixRunner();
            _runner.OnProgress += Runner_OnProgress;
            _runner.OnFinish += Runner_OnFinish;
            
            long inputSize = 0;
            if (File.Exists(txtInput.Text))
                inputSize = new FileInfo(txtInput.Text).Length;

            lblInputSize.Text = "Size: " + inputSize + " bytes";

            var runConfig = new RunConfig
            {
                InputPath = txtInput.Text,
                OutputPath = txtOutput.Text,
                Action = _settings.Action,
                UseDict = _settings.UseDict,
                VersionKey = cmbVersion.SelectedItem.ToString(),
                ShowCmd = _settings.ShowCmd,
                InputFileSize = inputSize,
                PretrainingFileSize = inputSize // Rough estimate for cmix
            };

            _ = _runner.RunAsync(runConfig);
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            if (!_isProcessing) return;
            _runner?.Cancel();
            btnCancel.IsEnabled = false;
        }

        private void Runner_OnProgress(ProgressData e)
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                if (e.Type == "pretrain")
                {
                    barPretrain.Value = e.Percent;
                    lblPretrainPercent.Text = $"{e.Percent:F2}%";
                    
                    if (e.Eta > 0 && e.Eta < 31536000)
                    {
                        var ts = TimeSpan.FromSeconds(e.Eta);
                        lblPretrainStats.Text = $"Speed: {(e.Speed / 1024):F2} KB/s - ETA: {ts:hh\\:mm\\:ss}";
                    }
                    else
                        lblPretrainStats.Text = $"Speed: {(e.Speed / 1024):F2} KB/s";
                }
                else
                {
                    barMain.Value = e.Percent;
                    lblMainPercent.Text = $"{e.Percent:F2}%";
                    
                    if (e.Eta > 0 && e.Eta < 31536000)
                    {
                        var ts = TimeSpan.FromSeconds(e.Eta);
                        lblMainStats.Text = $"Speed: {(e.Speed / 1024):F2} KB/s - ETA: {ts:hh\\:mm\\:ss}";
                    }
                    else
                        lblMainStats.Text = $"Speed: {(e.Speed / 1024):F2} KB/s";
                }
            });
        }

        private void Runner_OnFinish(bool wasCancelled, long outputSize)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isProcessing = false;
                btnStart.IsEnabled = true;
                btnCancel.IsEnabled = false;

                if (wasCancelled)
                {
                    borderCancelled.Visibility = Visibility.Visible;
                }
                else if (outputSize > 0)
                {
                    long inputSize = 0;
                    if (File.Exists(txtInput.Text))
                        inputSize = new FileInfo(txtInput.Text).Length;
                        
                    if (inputSize > 0)
                    {
                        double ratio = (double)outputSize / inputSize * 100.0;
                        lblRatio.Text = $"Ratio: {ratio:F2}%";
                        lblRatio.Visibility = Visibility.Visible;
                    }
                    lblOutputSize.Text = "Size: " + outputSize + " bytes";
                }
            });
        }
    }
}
