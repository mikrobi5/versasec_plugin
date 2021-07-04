using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using PipeCommunication;
using PipeCommunication.Interfaces;

using System.Diagnostics;
using System.Threading.Tasks;

namespace PluginTest
{

    using PluginTest.Mocks;

    public class PipeReaderTest
    {
        private ServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddTransient<IPipeReader, PipeReader>();
            services.AddSingleton<IStandardReadablePipe, ReadableStreamMock>();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Test]
        //[ConsoleInput("Test")]
        public async Task Read_From_Stream_Positiv_Test()
        {
            var test = (IPipeReader)this._serviceProvider.GetService(typeof(IPipeReader));// new PipeReader();
            var wsm = (IStandardReadablePipe)this._serviceProvider.GetService(typeof(IStandardReadablePipe));
            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 100; i++)
            {
                sw.Reset();
                sw.Start();
                var resString = await test.ReadMessage();
                Trace.WriteLine($"time took {sw.ElapsedMilliseconds.ToString()} {sw.ElapsedTicks.ToString()}");
                Assert.IsNotNull(resString);
                Assert.IsNotEmpty(resString);
                wsm.SetPositionBackToZero();
                //Task.Delay(100).Wait();
            }
        }

        [Test]
        //[ConsoleInput("Test")]
        public async Task Read_From_Stream_Negative_Test()
        {
            var test = (IPipeReader)this._serviceProvider.GetService(typeof(IPipeReader));// new PipeReader();
            //var wsm = (IStandardReadablePipe)this._serviceProvider.GetService(typeof(IStandardReadablePipe));
            var resString = await test.ReadMessage();
            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 100; i++)
            {
                sw.Reset();
                sw.Start();
                resString = await test.ReadMessage();
                Trace.WriteLine($"time took {sw.ElapsedMilliseconds.ToString()} {sw.ElapsedTicks.ToString()}");
                Assert.IsNotNull(resString);
                Assert.IsEmpty(resString);
                //wsm.SetPositionBackToZero();
                //Task.Delay(100).Wait();
            }
        }

        //[Test]
        //public void Write_To_Stream_Test()
        //{
        //    var test = (IPipeWriter)_serviceProvider.GetService(typeof(IPipeWriter));
        //    var wsm = (IStandardWritablePipe)_serviceProvider.GetService(typeof(IStandardWritablePipe));
        //    var message = "Das ist ein megalanger Teststring, um das lesen aus einem Stream zu testen.";

        //    var sw = new Stopwatch();
        //    for (int i = 0; i < 100; i++)
        //    {

        //        sw.Start();
        //        //  var resString = test.ReadMessage(rs.GetStream()).Result;
        //        test.WriteMessage(message);
        //        ReadOnlySpan<byte> testArray = wsm.GetStreamContent()[4..];
        //        var resString = Encoding.Default.GetString(testArray);
        //        Trace.WriteLine(string.Format("time took {0} {1}", sw.ElapsedMilliseconds.ToString(), sw.ElapsedTicks.ToString()));
        //        sw.Reset();
        //        Assert.IsNotNull(resString);
        //        Assert.IsNotEmpty(resString);
        //        Assert.IsTrue(resString.Equals(message));
        //        Task.Delay(1000).Wait();
        //    }
        //    //Assert.Pass();
        //}

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