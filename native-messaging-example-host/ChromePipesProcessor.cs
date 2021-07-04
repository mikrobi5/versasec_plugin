
using PipeCommunication.Interfaces;

using Serilog;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace native_messaging_example_host
{
    using native_messaging_example_host.Interfaces;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IChromePipesProcessor" />
    public class ChromePipesProcessor : IChromePipesProcessor
    {
        /// <summary>
        /// The cancellation token source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The read cancellation token
        /// </summary>
        private readonly CancellationTokenSource _readCancellationToken;

        /// <summary>
        /// The write cancellation token
        /// </summary>
        private readonly CancellationTokenSource _writeCancellationToken;

        /// <summary>
        /// The read task
        /// </summary>
        private Task _readTask;

        /// <summary>
        /// The pipe reader
        /// </summary>
        private readonly IPipeReader _pipeReader;

        /// <summary>
        /// The pipe writer
        /// </summary>
        private readonly IPipeWriter _pipeWriter;

        /// <summary>
        /// The process should killed
        /// </summary>
        private readonly AutoResetEvent _processShouldKilled;

        /// <summary>
        /// Occurs when [pipe message received].
        /// </summary>
        public event PipeMessageProcessing PipeMessageReceived;

        /// <summary>
        /// The read message flag
        /// </summary>
        private bool _readMessageFlag = false;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool _disposedValue;
        

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromePipesManager"/> class.
        /// </summary>
        /// <param name="interprocessPipeManager">The interprocess pipe manager.</param>
        /// <param name="pipeReader">The pipe reader.</param>
        /// <param name="pipeWriter">The pipe writer.</param>
        public ChromePipesProcessor(IPipeReader pipeReader, IPipeWriter pipeWriter)
        {
            _pipeReader = pipeReader;
            _pipeWriter = pipeWriter;
            _cancellationTokenSource = new CancellationTokenSource();
            _readCancellationToken = new CancellationTokenSource();
            _writeCancellationToken = new CancellationTokenSource();
            _processShouldKilled = new AutoResetEvent(false);
            if (_readTask == null)
            {
                _readTask = new Task(this.ReadMessageFromPipe, this._readCancellationToken.Token);
            }
        }

        /// <summary>
        /// 2. Step 
        /// Reads the message from the pipe connected to the browser plugin
        /// </summary>
        public void ReadMessageFromPipe()
        {
            while (_readMessageFlag)
            {
                try
                {
                    Log.Information("try to read from stream");
                    var message = this._pipeReader.ReadMessage().Result;
                    Log.Information($"Readmessage {message}");
                    OnMessageReceived(message);
                    Task.Delay(1).Wait();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Chrome-ReadMessageFromPipe: ");
                }
            }
        }

        /// <summary>
        /// Starts the process messaging.
        /// </summary>
        public void StartProcessMessaging()
        {
            if (_readTask != null)
            {
                Log.Information("start chrome - communication");
                _readMessageFlag = true;
                _readTask.Start();
            }
        }

        /// <summary>
        /// Stops the process messaging.
        /// </summary>
        public void StopProcessMessaging()
        {
            _readMessageFlag = false;
            
            _readCancellationToken?.Cancel();
            _writeCancellationToken?.Cancel();
            _cancellationTokenSource?.Cancel();
            _pipeReader?.Dispose();
            //this._readTask?.Wait(5000);
            _readTask = null;
            //_readTask?.Dispose();
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        private void LogMessage(string msg)
        {
            Log.Information(msg);
        }

        /// <summary>
        /// Called when [message received].
        /// </summary>
        /// <param name="message">The message.</param>
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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
                //_watchDogThread.Dispose();
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
