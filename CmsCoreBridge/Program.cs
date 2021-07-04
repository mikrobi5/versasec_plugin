using Serilog;

using System;
using System.Threading;

namespace CmsCoreBridge
{
    using Microsoft.Extensions.DependencyInjection;

    using PipeCommunication;
    using PipeCommunication.Interfaces;

    using System.IO;
    using System.Linq;

    using VSec.DotNet.CmsCore.Wrapper.Edge;

    class Program
    {
        /// <summary>
        /// The interprocess pipe manager
        /// </summary>
        private static IIpcPipesProcessor _interprocessPipeManager;

        /// <summary>
        /// The service provider
        /// </summary>
        private static ServiceProvider _serviceProvider;

        /// <summary>
        /// Creates the dependencies.
        /// </summary>
        private static void CreateDependencies()
        {
            var services = new ServiceCollection();
            services.AddTransient<IIpcPipesProcessor, InterprocessPipeProcessor>();
            services.AddSingleton<INamedOutputPipeClient>(new NamedOutputPipeClient("USS-Pipe-In"));
            services.AddSingleton<INamedInputPipeClient>(new NamedInputPipeClient("USS-Pipe-Out"));
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates the logger.
        /// </summary>
        private static void CreateLogger()
        {
            var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Versasec", "core_bridge.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logFile, rollingInterval: RollingInterval.Day).CreateLogger();
            Log.Logger.Information("CMS Core Bridge starting");

        }

        /// <summary>
        /// Starts the local processing.
        /// </summary>
        private static void StartLocalProcessing()
        {
            _interprocessPipeManager = (IIpcPipesProcessor)_serviceProvider.GetService(typeof(IIpcPipesProcessor));// InterprocessPipesProcessor();
            Log.Logger.Information("CMS Core Bridge start processing");
            _interprocessPipeManager.StartProcessMessaging();
            Console.ReadLine();
        }

        /// <summary>
        /// Locals the debugging.
        /// </summary>
        private static void LocalDebugging()
        {
            Console.WriteLine("Test is running");
            var _cmsCoreSimples = new CmsCoreSimples(Log.Logger);
            _cmsCoreSimples.RaiseCardAddedEvent += _cmsCoreSimples_RaiseCardAddedEvent;
            _cmsCoreSimples.RaiseCardRemovedEvent += _cmsCoreSimples_RaiseCardRemovedEvent;
            var cards = _cmsCoreSimples.GetCards();
            Console.WriteLine("Wait now");
            Thread.Sleep(2000);
            Console.WriteLine($"found card -  {cards?.FirstOrDefault()?.Csn}");
        }

        private static void _cmsCoreSimples_RaiseCardRemovedEvent(object sender, VSec.DotNet.CmsCore.Wrapper.Models.CardEventArgs a)
        {
            Console.WriteLine(a.Message);
        }

        private static void _cmsCoreSimples_RaiseCardAddedEvent(object sender, VSec.DotNet.CmsCore.Wrapper.Models.CardEventArgs a)
        {
            Console.WriteLine(a.Message);
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"{string.Join(" ", args)}");
                CreateLogger();
                CreateDependencies();

                if (args.Any() && args.First().Equals("test"))
                {
                    LocalDebugging();
                    Console.WriteLine("Test is finished");
                    return;
                }

                StartLocalProcessing();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"CMS Core Bridge exiting {e.Message} {e.InnerException?.Message} {e.StackTrace}");
                Console.WriteLine($"CMS Core Bridge exiting {e.Message} {e.InnerException?.Message} {e.StackTrace}");
            }
        }
    }
}
