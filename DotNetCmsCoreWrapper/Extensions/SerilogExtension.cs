using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Linq;

namespace VSec.DotNet.CmsCore.Wrapper.Serilog.Extension
{
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    /// <summary>
    /// extension to add serilog to logging pipeline
    /// </summary>
    public static class SerilogExtension
    {
        private const int _kiloByteSize = 1024;
        private static readonly string[] _sizeSuffixes = new[] { "KB", "MB", "GB", "TB", "PB" };
        private static readonly int[] _sizeExponent = new[] { 1, 2, 3, 4, 5 };

        /// <summary>
        /// Withes the caller.
        /// </summary>
        /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
        /// <returns></returns>
        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<CallerEnricher>();
        }

        /// <summary>
        /// Gets the size of the log file from app configuration, if no size is defined a default size of 50 mb is taken
        /// </summary>
        /// <returns>return the size of bytes</returns>
        private static long GetFileSize()
        {
            var configFileSize = "50MB";

            var regex = new Regex(@"(\d+)(([mM]|[gG]|[kK]|[tT]|[pP])[bB])?");
            var matches = regex.Matches(configFileSize);
            var newSize = 0L;
            if (matches.Count > 0)
            {
                if (matches[0].Groups.Count > 2)
                {
                    if (long.TryParse(matches[0].Groups[1].ToString(), out var sizeNumber))
                    {
                        var sizeSuffix = matches[0].Groups[2];
                        if (_sizeSuffixes.Contains(sizeSuffix.ToString().ToUpper()))
                        {
                            var index = Array.IndexOf(_sizeSuffixes, sizeSuffix.ToString().ToUpper());
                            newSize = (long)(sizeNumber * Math.Pow(_kiloByteSize, _sizeExponent[index]));
                        }
                    }
                }
                else if (matches[0].Groups.Count > 1)
                {
                    if (long.TryParse(matches[0].Groups[1].ToString(), out var sizeNumber))
                    {
                        newSize = (long)(sizeNumber * Math.Pow(_kiloByteSize, _sizeExponent[0]));
                    }
                }
            }
            else
            {
                Trace.WriteLine("no matches found");
            }

            return newSize;
        }

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="name">The name of the logger - only used for backward compatibility</param>
        /// <returns>signals if log creation was successful</returns>
        public static bool AddLogger(string name)
        {

            var fileSize = GetFileSize();
            Trace.WriteLine($"Filesize: {fileSize}");
            var fileName = GetFileName();
            Trace.WriteLine($"Filename: {fileName}");
            var logEventLevel = GetLogLevel();
            Trace.WriteLine($"LogLevel: {logEventLevel}");
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(fileName, fileSizeLimitBytes: fileSize, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({Class}.{Method}) {Message}{NewLine}{Exception}")
            .Enrich.FromLogContext()
            .Enrich.WithCaller()
            .CreateLogger();
            Trace.WriteLine("Logger created");
            if (Log.Logger != null)
            {
                Log.Logger.Information("#################  New Run ##################");
                Log.Logger.Information("");
                Trace.WriteLine("Logger created return true");
                return true;
            }
            Trace.WriteLine("Logger not created");
            return false;

        }

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public static bool AddLogger(ILogger logger)
        {
            if (logger != null)
            {
                Log.Logger = logger;
                if (Log.Logger != null)
                {
                    Log.Logger.Information("#################  New Run ##################");
                    Log.Logger.Information("");
                    return true;
                }
            }
            else
            {

                var fileSize = GetFileSize();
                Trace.WriteLine($"Filesize: {fileSize}");
                var fileName = GetFileName();
                Trace.WriteLine($"Filename: {fileName}");
                var logEventLevel = GetLogLevel();
                Trace.WriteLine($"LogLevel: {logEventLevel}");
                //Log.Logger = new LoggerConfiguration()
                //    .MinimumLevel.Debug()
                //    .WriteTo.File(fileName, fileSizeLimitBytes: fileSize, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day,
                //        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({Class}.{Method}) {Message}{NewLine}{Exception}")
                //    .Enrich.FromLogContext()
                //    .Enrich.WithCaller()
                //    .CreateLogger();
                Trace.WriteLine("Logger created");
                if (Log.Logger != null)
                {
                    Log.Logger.Information("#################  New Run ##################");
                    Log.Logger.Information("");
                    Trace.WriteLine("Logger created return true");
                    return true;
                }
                Trace.WriteLine("Logger not created");
                return false;
            }


            return false;
        }

        /// <summary>
        /// Gets the name of the file from configuration 
        /// </summary>
        /// <returns></returns>
        private static string GetFileName()
        {
            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            }
            var configPath = "DotNetWrapper";//@"C:\Working\test";
            var logPath = configPath;
            if (!IsFullPath(configPath))
            {
                logPath = Path.Combine(systemFolder, "versasec", configPath);
            }

            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            var fileName = "default_log.log";

            var logfileName = Path.Combine(logPath, fileName);

            return logfileName;
        }

        /// <summary>
        /// Determines whether given path is full path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if the specified path is an full path; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsFullPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                   && path.IndexOfAny(Path.GetInvalidPathChars().ToArray()) == -1
                   && Path.IsPathRooted(path)
                   && !Path.GetPathRoot(path).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the log level from configuration
        /// </summary>
        /// <returns>Serilog LogeventLevel</returns>
        private static LogEventLevel GetLogLevel()
        {
            return LogEventLevel.Debug;
        }
    }
}
