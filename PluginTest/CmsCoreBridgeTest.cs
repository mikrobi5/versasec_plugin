namespace PluginTest
{
    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;

    using PipeCommunication;
    using PipeCommunication.Interfaces;

    using System;
    using System.Diagnostics;
    using System.IO;

    public class CmsCoreBridgeTest
    {
        private ServiceProvider _serviceProvider;
        private INamedInputPipeServer _iNamedInputPipeServer;
        private INamedOutputPipeServer _iNamedOutputPipeServer;
        private INamedOutputPipeClient _iNamedOutputPipeClient;
        private INamedInputPipeClient _iNamedInputPipeClient;
        private IStandardReadablePipe _rsm;

        [SetUp]
        public void Setup()
        {
            //InterprocessPipeProcessor
            var services = new ServiceCollection();
            services.AddSingleton<INamedOutputPipeServer>(new NamedOutputPipeServer("USS-Pipe-Out"));
            services.AddSingleton<INamedInputPipeServer>(new NamedInputPipeServer("USS-Pipe-In"));
            services.AddSingleton<INamedOutputPipeClient>(new NamedOutputPipeClient("USS-Pipe-In"));
            services.AddSingleton<INamedInputPipeClient>(new NamedInputPipeClient("USS-Pipe-Out"));
            _serviceProvider = services.BuildServiceProvider();
            _iNamedInputPipeServer = (INamedInputPipeServer)_serviceProvider.GetService(typeof(INamedInputPipeServer));
            _iNamedOutputPipeServer = (INamedOutputPipeServer)_serviceProvider.GetService(typeof(INamedOutputPipeServer));
            _iNamedOutputPipeClient = (INamedOutputPipeClient)_serviceProvider.GetService(typeof(INamedOutputPipeClient));
            _iNamedInputPipeClient = (INamedInputPipeClient)_serviceProvider.GetService(typeof(INamedInputPipeClient));
            //StartCmsCoreBridge(@"e:\Projects\VSec\browser_plugin\VsecNativeMessageHostConsoleNetCore\native-messaging-example-host\CmsCoreBridge\bin\x64\Debug\netcoreapp3.1\CmsCoreBridge.exe");
            _iNamedInputPipeClient.Connect();
            
            _iNamedOutputPipeServer.WaitForConnection();
            //_iNamedInputPipeServer.WaitForConnection();
        }

        private void StartCmsCoreBridge(string processName)
        {
            if (!File.Exists(processName)) throw new FileNotFoundException("Cms Core Bridge not found");
            var _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    CreateNoWindow = true
                }
            };
            if (_process.Start())
            {
            }
        }

        [Test]
        public void Send_To_Core_Bridge_Test()
        {
            _iNamedOutputPipeServer.WriteMessage("Test 1234");
            var  res =_iNamedInputPipeClient.ReadMessage().Result;
        }
    }
}
