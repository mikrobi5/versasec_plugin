using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using PipeCommunication;
using PipeCommunication.Interfaces;

using System;
using System.Diagnostics;
using System.Text;

namespace PluginTest
{
   

    using PluginTest.Mocks;

    public class PipeProcessorTest
    {
        private ServiceProvider _serviceProvider;
        private IStandardWritablePipe _wsm;
        private IStandardReadablePipe _rsm;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddTransient<IPipeWriter, PipeWriter>();
            services.AddSingleton<IStandardWritablePipe, WritableStreamMock>();
            services.AddTransient<IPipeReader, PipeReader>();
            services.AddSingleton<IStandardReadablePipe, ReadableStreamMock>();
            services.AddSingleton<IPipeProcessor, PipeProcessorMock>();
            _serviceProvider = services.BuildServiceProvider();
            _wsm = (IStandardWritablePipe)_serviceProvider.GetService(typeof(IStandardWritablePipe));
            _rsm = (IStandardReadablePipe)_serviceProvider.GetService(typeof(IStandardReadablePipe));
        }

        [Test]
        public void Write_To_Stream_Positiv_Test()
        {
            var test = (IPipeProcessor)_serviceProvider.GetService(typeof(IPipeProcessor));
            test.PipeMessageReceived += this.Test_PipeMessageReceived;
            test.ReadMessageFromPipe();
        }

        private void Test_PipeMessageReceived(string message)
        {
            Assert.IsNotEmpty(message);
        }

        [Test]
        public void Write_To_Stream_Negative_Test()
        {
            var test = (IPipeWriter)_serviceProvider.GetService(typeof(IPipeWriter));
            
            var message = "";

            var sw = new Stopwatch();
            for (int i = 0; i < 100; i++)
            {

                sw.Start();
                //  var resString = test.ReadMessage(rs.GetStream()).Result;
                test.WriteMessage(message);
                var testArray = _wsm.GetStreamContent()[4..];
                var resString = Encoding.Default.GetString(testArray);
                Trace.WriteLine(string.Format("time took {0} {1}", sw.ElapsedMilliseconds.ToString(), sw.ElapsedTicks.ToString()));
                sw.Reset();
                Assert.IsNotNull(resString);
                Assert.IsEmpty(resString);
                Assert.IsTrue(resString.Equals(message));
                //Task.Delay(1000).Wait();
            }
            //Assert.Pass();
        }

        //[Test]
        //public void Test_Read_From_Card()
        //{
        //    //var test = new CmsCoreSimples();
        //    //var cards = test.GetCards();
        //    var _interprocessPipeManager = new InterprocessPipeManager();
        //    _interprocessPipeManager.StartProcessMessaging();
        //    //var test = (IPipeManager)_serviceProvider.GetService(typeof(IPipeManager));
        //    _interprocessPipeManager.CheckReceivedMessage("{\"id\" : 1, \"message\" : {\"Command\":\"RCCRC\"}}");
        //}
    }
}