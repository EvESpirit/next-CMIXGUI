using System.Collections.Generic;

namespace nextCMIXGUI_WinUI.Core
{
    public static class Translations
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            {
                "English", new Dictionary<string, string>
                {
                    { "window_title", "nextCMIXGUI" },
                    { "input_file_folder", "Select a file or folder to compress" },
                    { "output_file", "Select an output file location" },
                    { "action", "Action:" },
                    { "compress", "Compress" },
                    { "extract", "Extract" },
                    { "preprocess", "Preprocess" },
                    { "cmix_version", "CMIX Version:" },
                    { "language", "Language:" },
                    { "english", "English" },
                    { "spanish", "Spanish" },
                    { "use_eng_dict", "Use english.dic" },
                    { "show_cmd_window", "Show CMD Window" },
                    { "start", "Start" },
                    { "save_log", "Save Log" },
                    { "github_link", "cmix by Byron Knoll" },
                    { "no_versions_found", "No cmix versions found in the 'exes' folder." },
                    { "task_running", "A task is already running. Please wait or cancel it." },
                    { "select_input_first", "Please select a valid input file or folder first." },
                    { "finished", "Task completed successfully!" },
                    { "log_saved", "Log file saved successfully." },
                    { "compress_input", "Select a file or folder to compress" },
                    { "compress_output", "Select an output file location" },
                    { "extract_input", "Select a .cmix file to extract" },
                    { "extract_output", "Select an output file location" },
                    { "preprocess_input", "Select a file or folder to preprocess" },
                    { "preprocess_output", "Select an output file location" },
                    { "whoever_moves_is_gay", "Whoever moves first is gay." },
                    { "overwrite_confirm_title", "Confirm Overwrite" },
                    { "overwrite_confirm_msg", "The file '{filename}' already exists.\nDo you want to overwrite it?" }
                }
            },
            {
                "Spanish", new Dictionary<string, string>
                {
                    { "window_title", "nextCMIXGUI" },
                    { "input_file_folder", "Seleccione un archivo o carpeta" },
                    { "output_file", "Seleccione una ubicación de salida" },
                    { "action", "Acción:" },
                    { "compress", "Comprimir" },
                    { "extract", "Extraer" },
                    { "preprocess", "Preprocesar" },
                    { "cmix_version", "Versión CMIX:" },
                    { "language", "Idioma:" },
                    { "english", "Inglés" },
                    { "spanish", "Español" },
                    { "use_eng_dict", "Usar english.dic" },
                    { "show_cmd_window", "Mostrar Ventana CMD" },
                    { "start", "Iniciar" },
                    { "save_log", "Guardar Registro" },
                    { "github_link", "cmix por Byron Knoll" },
                    { "no_versions_found", "No se encontraron versiones de cmix en la carpeta 'exes'." },
                    { "task_running", "Una tarea ya está en ejecución. Por favor espere o cancélela." },
                    { "select_input_first", "Por favor, seleccione primero un archivo o carpeta de entrada válido." },
                    { "finished", "¡Tarea completada con éxito!" },
                    { "log_saved", "Archivo de registro guardado con éxito." },
                    { "compress_input", "Seleccione archivo/carpeta a comprimir" },
                    { "compress_output", "Seleccione ubicación del archivo de salida" },
                    { "extract_input", "Seleccione archivo .cmix a extraer" },
                    { "extract_output", "Seleccione ubicación del archivo de salida" },
                    { "preprocess_input", "Seleccione archivo/carpeta a preprocesar" },
                    { "preprocess_output", "Seleccione ubicación del archivo de salida" },
                    { "whoever_moves_is_gay", "El que se mueva es gay." },
                    { "overwrite_confirm_title", "Confirmar Sobrescribir" },
                    { "overwrite_confirm_msg", "El archivo '{filename}' ya existe.\n¿Desea sobrescribirlo?" }
                }
            }
        };

        private static string _currentLang = "English";

        public static void SetLanguage(string lang)
        {
            if (_translations.ContainsKey(lang))
            {
                _currentLang = lang;
            }
        }

        public static string Get(string key)
        {
            if (_translations.TryGetValue(_currentLang, out var langDict))
            {
                if (langDict.TryGetValue(key, out var val))
                    return val;
            }
            return key;
        }
    }
}
