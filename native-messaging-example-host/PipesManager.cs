using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace native_messaging_example_host
{
    using native_messaging_example_host.Interfaces;

    using PipeCommunication.Interfaces;
    using PipeCommunication.Models;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IPipeManager" />
    public class PipesManager : IPipeManager
    {
        /// <summary>
        /// The CMS core manager
        /// </summary>
        private CmsCoreBridgeManager _cmsCoreManager;

        /// <summary>
        /// The chrome pipes processor
        /// </summary>
        private IChromePipesProcessor _chromePipesProcessor;

        /// <summary>
        /// The interprocess pipe processor
        /// </summary>
        private readonly IIpcPipesProcessor _interprocessPipeProcessor;

        /// <summary>
        /// The process should killed
        /// </summary>
        private AutoResetEvent _processShouldKilled = new AutoResetEvent(false);

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IPipeManager" /> is proxying.
        /// </summary>
        /// <value>
        ///   <c>true</c> if proxying; otherwise, <c>false</c>.
        /// </value>
        public bool Proxying { get; set; } = false;

        /// <summary>
        /// Gets the cancellation token source.
        /// </summary>
        /// <value>
        /// The cancellation token source.
        /// </value>
        public CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// The watch dog flag
        /// </summary>
        private bool _watchDogFlag = false;

        /// <summary>
        /// The watch dog thread
        /// </summary>
        private Task _watchDogThread;

        private bool _restartIsInProgress;

        /// <summary>
        /// The event message
        /// </summary>
        public static EventMessage EventMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromePipesManager"/> class.
        /// </summary>
        /// <param name="interprocessPipeManager">The interprocess pipe manager.</param>
        /// <param name="pipeReader">The pipe reader.</param>
        /// <param name="pipeWriter">The pipe writer.</param>
        public PipesManager(IChromePipesProcessor chromePipesProcessor, IIpcPipesProcessor interprocessPipeProcessor)
        {
            _chromePipesProcessor = chromePipesProcessor;
            _interprocessPipeProcessor = interprocessPipeProcessor;

            _chromePipesProcessor.PipeMessageReceived += this._chromePipesProcessor_PipeMessageReceived;
            _interprocessPipeProcessor.PipeMessageReceived += this._interprocessPipeProcessor_PipeMessageReceived;

            CancellationTokenSource = new CancellationTokenSource();

            StartWatchDog();
        }

        /// <summary>
        /// Starts the watch dog thread.
        /// </summary>
        private void StartWatchDog()
        {
            _watchDogFlag = true;
            Task.Factory.StartNew(WatchDogThread, CancellationTokenSource.Token);
        }

        /// <summary>
        /// Watches the dog thread.
        /// </summary>
        private void WatchDogThread()
        {
            while (_watchDogFlag)
            {
                if (!_processShouldKilled.WaitOne(10 * 1000))
                {
                    Log.Logger.Information("Ping/Pong Message NOT signaled");
#if !DEBUG
                    CancellationTokenSource.Cancel();
                    _watchDogFlag = false;
#endif
                }
                else
                {
                    Log.Logger.Information("Ping/Pong Message event signaled");
                }

                if (!_interprocessPipeProcessor.IsConnected)
                {
                    Log.Logger.Error("CMS core bridge no longer connected");
//                    Task.Factory.StartNew(() =>
//                    {
//                        if (!_restartIsInProgress)
//                        {
//                            _restartIsInProgress = true;
//                            //TODO: reinitial cmscore manager und ipc pipe manager
//                            _cmsCoreManager.StopProcess();
//                            _cmsCoreManager = null;
//                            _interprocessPipeProcessor.StopReadTask();
//#if DEBUG
//            _cmsCoreManager = new CmsCoreBridgeManager(@"e:\Projects\VSec\browser_plugin\host\CmsCoreBridge.exe");
//#else
//                            _cmsCoreManager = new CmsCoreBridgeManager();
//#endif
//                            Task.Delay(1000).Wait();
//                            _cmsCoreManager.StartProcess();
//                            _interprocessPipeProcessor.StartProcessMessaging();
//                            _restartIsInProgress = false;
//                        }
//                    });
                }
            }
        }

        /// <summary>
        /// Interprocesses the pipe processor pipe message received.
        /// </summary>
        /// <param name="message">The message.</param>
        private void _interprocessPipeProcessor_PipeMessageReceived(string message)
        {
            this._chromePipesProcessor.WriteMessageToPipe(message);
        }

        /// <summary>
        /// Chromes the pipes processor pipe message received.
        /// </summary>
        /// <param name="message">The message.</param>
        private void _chromePipesProcessor_PipeMessageReceived(string message)
        {
            CheckReceivedMessage(message);
        }

        /// <summary>
        /// Passes the through.
        /// </summary>
        /// <param name="message">The message.</param>
        private void PassThrough(string message)
        {
            Log.Logger.Information($"pass through {message}");
            if (!_restartIsInProgress)
                _interprocessPipeProcessor.WriteMessageToPipe(message);
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void LogMessage(string msg)
        {
            Log.Logger.Information(msg);
        }

        /// <summary>
        /// 3. Step
        /// Checks the received message - from browser plgun
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        public bool CheckReceivedMessage(string msg)
        {
            try
            {
                var jsonMessage = JsonConvert.DeserializeObject<JObject>(msg);
                int messageId = int.Parse(jsonMessage["id"].ToString());
                string cardCommand = jsonMessage["message"]["Command"].ToString();
                string outputStream = string.Empty;

                if (cardCommand.Equals("PING"))
                {
                    LogMessage("PING received");
                    outputStream = $"{{ \"id\" : {messageId.ToString()} , \"data\" : {{ \"control\" : \"PONG\" }}}}";
                    _chromePipesProcessor.WriteMessageToPipe(outputStream);
                    _processShouldKilled.Set();
                    _processShouldKilled.Reset();
                    return true;
                }

                if (cardCommand.Equals("Events"))
                {
                    LogMessage("Events received");
                    if (EventMessage != null)
                    {
                        outputStream = $"{{ \"id\" : {messageId.ToString()} , \"data\" : {{ \"Events\" : \"{EventMessage.Message}\", \"Severity\" : \"{EventMessage.Severity.ToString()}\"}}}}";
                        _chromePipesProcessor.WriteMessageToPipe(outputStream);
                        EventMessage = null; //string.Empty;
                        return true;
                    }
                }

                PassThrough(msg);
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CheckReceivedMessage: ");
                return false;
            }
        }

        /// <summary>
        /// Starts the processing.
        /// </summary>
        /// <returns></returns>
        public bool StartProcessing()
        {
            //throw new NotImplementedException();
            //if (_cmsCoreManager == null)
            //{
            //    return false;
            //}
#if DEBUG
            _cmsCoreManager = new CmsCoreBridgeManager(@"e:\Projects\VSec\browser_plugin\host\CmsCoreBridge.exe");
#else
            _cmsCoreManager = new CmsCoreBridgeManager();
#endif
            _cmsCoreManager.StartProcess();
            _interprocessPipeProcessor.StartProcessMessaging();
            _chromePipesProcessor.StartProcessMessaging();
            return true;
        }

        /// <summary>
        /// Stops the processing.
        /// </summary>
        /// <returns></returns>
        public bool StopProcessing()
        {
            LogMessage("PipeManager: Stop Processing");
            CancellationTokenSource.Cancel();
            _watchDogFlag = false;
            _watchDogThread?.Wait(5000);
            _watchDogThread?.Dispose();
            LogMessage("Stop Inter-process Processing");
            _interprocessPipeProcessor.StopProcessMessaging();
            LogMessage("Stop Chrome Processing");
            _chromePipesProcessor.StopProcessMessaging();
            _cmsCoreManager.StopProcess();
            return true;
        }
    }
}
