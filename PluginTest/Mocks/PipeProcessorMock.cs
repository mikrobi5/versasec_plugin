using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PluginTest.Mocks
{
    using PipeCommunication.Interfaces;

    public class PipeProcessorMock : IPipeProcessor
    {
        // private readonly byte[] _buffer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationTokenSource _readCancellationToken;
        private readonly CancellationTokenSource _writeCancellationToken;
        private readonly Task _readTask;
        private readonly IPipeReader _pipeReader;
        private readonly IPipeWriter _pipeWriter;
        private readonly AutoResetEvent _processShouldKilled;
        public event PipeMessageProcessing PipeMessageReceived;
        private bool _disposedValue;
        private Task _watchDogThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromePipesManager"/> class.
        /// </summary>
        /// <param name="interprocessPipeManager">The interprocess pipe manager.</param>
        /// <param name="pipeReader">The pipe reader.</param>
        /// <param name="pipeWriter">The pipe writer.</param>
        public PipeProcessorMock(IPipeReader pipeReader, IPipeWriter pipeWriter)
        {
            _pipeReader = pipeReader;
            _pipeWriter = pipeWriter;
            _cancellationTokenSource = new CancellationTokenSource();
            _readCancellationToken = new CancellationTokenSource();
            _writeCancellationToken = new CancellationTokenSource();
            _processShouldKilled = new AutoResetEvent(false);
            //if (_readTask == null)
            //{
            //    _readTask = new Task(ReadMessage, this._readCancellationToken.Token);
            //}

            //_watchDogThread = Task.Factory.StartNew(this.WatchDogThread, this._cancellationTokenSource.Token);
        }

        private void WatchDogThread()
        {
            while (true)
            {
                if (!_processShouldKilled.WaitOne(10 * 1000))
                {
                    Log.Logger.Information($"event NOT signled");
                    _cancellationTokenSource.Cancel();
                }
                else
                {
                    Log.Logger.Information($"event signled");
                }
            }
        }



        /// <summary>
        /// 2. Step 
        /// Reads the message from the pipe connected to the browser plugin
        /// </summary>
        public void ReadMessageFromPipe()
        {

            Log.Logger.Information("try to read from stream");
            var message = this._pipeReader.ReadMessage().Result;
            Log.Logger.Information($"Readmessage {message}");
            OnMessageReceived(message);
            Task.Delay(1).Wait();

        }

        /// <summary>
        /// Starts the process messaging.
        /// </summary>
        public void StartProcessMessaging()
        {
            if (_readTask != null)
            {
                Log.Logger.Information("start chrome - communication");
                _readTask.Start();
            }
        }

        /// <summary>
        /// Stops the process messaging.
        /// </summary>
        public void StopProcessMessaging()
        {
            _readCancellationToken.Cancel();
            _writeCancellationToken.Cancel();
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void LogMessage(string msg)
        {
            Log.Logger.Information(msg);
        }

        private void OnMessageReceived(string message)
        {
            this.PipeMessageReceived(message);
        }

        /// <summary>
        /// Writes the message to pipe.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void WriteMessageToPipe(string outputStream)
        {
            _pipeWriter.WriteMessage(outputStream);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                }

                StopProcessMessaging();
                _cancellationTokenSource.Dispose();
                _readCancellationToken.Dispose();
                _writeCancellationToken.Dispose();
                _readTask.Dispose();
                _pipeWriter.Dispose();
                _processShouldKilled.Dispose();
                PipeMessageReceived = null;
                _watchDogThread.Dispose();
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                _disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~ChromePipesProcessor()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void StopReadTask()
        {
            throw new NotImplementedException();
        }
    }
}

