using Serilog;

using System;
using System.Threading.Tasks;

namespace native_messaging_example_host
{
    using Microsoft.Extensions.DependencyInjection;

    using native_messaging_example_host.Interfaces;

    using PipeCommunication;
    using PipeCommunication.Interfaces;

    using System.IO;

    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        // private static ChromePipesManager _test;
        private static ServiceProvider _serviceProvider;
        private static IPipeManager _pipeManager;

        /// <summary>
        /// Creates the dependencies.
        /// </summary>
        private static void CreateDependencies()
        {
            var services = new ServiceCollection();
            services.AddTransient<IPipeReader, PipeReader>();
            services.AddTransient<IPipeWriter, PipeWriter>();
            services.AddTransient<IIpcPipesProcessor, InterprocessPipeProcessor>();
            services.AddTransient<IChromePipesProcessor, ChromePipesProcessor>();
            services.AddSingleton<INamedOutputPipeServer>(new NamedOutputPipeServer("USS-Pipe-Out"));
            services.AddSingleton<INamedInputPipeServer>(new NamedInputPipeServer("USS-Pipe-In"));
            services.AddTransient<IStandardWritablePipe, StandardWritablePipe>();
            services.AddTransient<IStandardReadablePipe, StandardReadablePipe>();
            services.AddTransient<IPipeManager, PipesManager>();
            _serviceProvider = services.BuildServiceProvider();
            _pipeManager = (IPipeManager)_serviceProvider.GetService(typeof(IPipeManager));
            Log.Logger.Information($"provider created");
        }

        /// <summary>
        /// Creates the logging.
        /// </summary>
        private static void CreateLogging()
        {
            var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Versasec", "browser_host.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logFile, rollingInterval: RollingInterval.Day).CreateLogger();
            Log.Logger.Information("native host started");
        }

        /// <summary>
        /// Starts the local processing.
        /// </summary>
        private static void StartLocalProcessing()
        {
            try
            {
                _pipeManager.Proxying = true;
                _pipeManager.StartProcessing();
                Log.Logger.Information($"processing started");
                var mainWaitHandle = Task.Delay(-1, _pipeManager.CancellationTokenSource.Token);
                Task.WaitAll(mainWaitHandle);
            }
            catch (TaskCanceledException tcex)
            {
                Log.Logger.Error(tcex, $"App Aborted-TaskCanceledException");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"App Aborted-Exception");
            }
            finally
            {
                StopLocalProcessing();
            }
        }

        /// <summary>
        /// Stops the local processing.
        /// </summary>
        private static void StopLocalProcessing()
        {
            // _test.StopProcessMessaging();
            Log.Logger.Information("Native host app stopped");
            _pipeManager.StopProcessing();
            Log.Logger.Information($"Kill the process");
            _pipeManager = null;
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        [STAThread]
        public static int Main(string[] args)
        {
            CreateLogging();
            foreach (var arg in args)
            {
                Log.Logger.Information($"args: {arg}");
            }
            CreateDependencies();
            //var testCancelToken = new CancellationTokenSource();
            StartLocalProcessing();
            return 0;
        }
    }
}
