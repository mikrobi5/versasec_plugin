using Serilog;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace native_messaging_example_host
{
    /// <summary>
    /// 
    /// </summary>
    public class CmsCoreBridgeManager
    {
        /// <summary>
        /// The process
        /// </summary>
        private Process _process;

        /// <summary>
        /// The process name
        /// </summary>
        private string _processName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmsCoreBridgeManager"/> class.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        public CmsCoreBridgeManager(string processName = null)
        {
            _processName = processName ?? _processName;
            CheckForSettingsFile();
        }

        /// <summary>
        /// Checks for the settings file.
        /// </summary>
        private void CheckForSettingsFile()
        {
            if (string.IsNullOrWhiteSpace(this._processName))
            {
                if (File.Exists("settings.json"))
                {
                    Log.Logger.Information("File does exists");
                    ////Console.WriteLine("File does exists");
                    var lines = File.ReadAllLines("settings.json");
                    if (lines.Length > 0)
                    {
                        _processName = lines.First();
                        Log.Logger.Error($"process name set {this._processName}");
                        ////Console.WriteLine($"process name set {this._processName}");
                    }
                }
                else
                {
                    Log.Logger.Error("settings File does not exists");
                    throw new FileNotFoundException("settings file is missing");
                }
            }
        }

        /// <summary>
        /// Starts the process.
        /// </summary>
        /// <exception cref="FileNotFoundException">Cms Core Bridge not found</exception>
        public void StartProcess()
        {
#if !DEBUGG
            if (!File.Exists(_processName)) throw new FileNotFoundException("Cms Core Bridge not found");
            ////Console.WriteLine("StartProcess - File does exists");
            var brdigeFileInfo = new FileInfo(_processName);
            var processName = brdigeFileInfo.Name.Replace(brdigeFileInfo.Extension, "");
            ////Console.WriteLine($"StartProcess - check for process {processName}");
            foreach (var p in Process.GetProcessesByName(processName))
            {
                p.Kill();
            }
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _processName,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    CreateNoWindow = true
                }
            };
            if (_process.Start())
            {
                _process.Exited += _process_Exited;
            }
#endif
        }

        /// <summary>
        /// Handles the Exited event of the _process control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _process_Exited(object sender, EventArgs e)
        {
            Log.Logger.Information("CmsCoreBridge exited");
        }

        /// <summary>
        /// Stops the process.
        /// </summary>
        public void StopProcess()
        {
            Log.Logger.Information("CmsCoreBridge killed");
            _process.Kill();
        }
    }
}
