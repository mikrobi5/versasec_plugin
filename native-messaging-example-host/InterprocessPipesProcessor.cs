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
    /// <seealso cref="IIpcPipesProcessor" />
    public class InterprocessPipeProcessor : IIpcPipesProcessor
    {
        /// <summary>
        /// The readable pipe
        /// </summary>
        private readonly INamedInputPipeServer _readablePipe;

        /// <summary>
        /// The writable pipe
        /// </summary>
        private readonly INamedOutputPipeServer _writablePipe;

        /// <summary>
        /// The read cancellation token
        /// </summary>
        private CancellationTokenSource _readCancellationToken;

        /// <summary>
        /// The watch dog cancellation token
        /// </summary>
        private CancellationTokenSource _watchDogCancellationToken;

        /// <summary>
        /// The read task
        /// </summary>
        private Task _readTask;

        /// <summary>
        /// The connection watch dog
        /// </summary>
        private Task _connectionWatchDog;

        /// <summary>
        /// The read task flag
        /// </summary>
        private bool _readTaskFlag = false;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// The is connected
        /// </summary>
        private bool _isConnected = true;

        /// <summary>
        /// Occurs when [pipe message received].
        /// </summary>
        public event PipeMessageProcessing PipeMessageReceived;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IPipeManager" /> is proxying.
        /// </summary>
        /// <value>
        ///   <c>true</c> if proxying; otherwise, <c>false</c>.
        /// </value>
        public bool Proxying { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterprocessPipeProcessor"/> class.
        /// </summary>
        /// <param name="readablePipe">The readable pipe.</param>
        /// <param name="writablePipe">The writable pipe.</param>
        public InterprocessPipeProcessor(INamedInputPipeServer readablePipe, INamedOutputPipeServer writablePipe)
        {
            _readablePipe = readablePipe;
            _writablePipe = writablePipe;
            //_writablePipe.

        }

        /// <summary>
        /// Starts the process messaging.
        /// </summary>
        public void StartProcessMessaging()
        {
            _isConnected = true;
           // _readablePipe.GetStream();
            
            Log.Logger.Information("Waiting for connection on named pipe USS-Pipe-In");
            _readablePipe.WaitForConnection();
            _readablePipe.WasConnected = false;
            Log.Logger.Information("connection established");
            Log.Logger.Information("Waiting for connection on named pipe USS-Pipe-Out");
            //_writablePipe.GetStream();
            _writablePipe.WaitForConnection();
            Log.Logger.Information("connection established");
            _isConnected = true;
            PipesManager.EventMessage = new EventMessage("Connection to CMS Core established", Severity.Positive);//"Connection to CMS Core established";

            if (_readTask == null)
            {
                _readCancellationToken = new CancellationTokenSource();
                _readTask = new Task(this.ReadMessageFromPipe, this._readCancellationToken.Token);
                _readTaskFlag = true;
                _readTask.Start();
            }

            if (_connectionWatchDog == null)
            {
                _watchDogCancellationToken = new CancellationTokenSource();
                _connectionWatchDog = new Task(ConnectionWatchDog, this._watchDogCancellationToken.Token);
                _connectionWatchDog.Start();
            }
        }

        /// <summary>
        /// Stops the process messaging.
        /// </summary>
        public void StopProcessMessaging()
        {
            _readTaskFlag = false;
            this._readablePipe.Dispose();
            this._writablePipe.Dispose();
            _watchDogCancellationToken?.Cancel();
            StopReadTask();
        }

        /// <summary>
        /// Stops the read task.
        /// </summary>
        public void StopReadTask()
        {
            _readCancellationToken?.Cancel();
            _readTaskFlag = false;
            while (!(bool)_readTask?.Wait(5000))
            {
                

            }

            _readablePipe.WasConnected = true;
            _readTask?.Dispose();
        }

        /// <summary>
        /// Writes the message to pipe - connected to the cms core bridge
        /// </summary>
        /// <param name="message">The message.</param>
        public async void WriteMessageToPipe(string message)
        {
            try
            {
                Log.Logger.Information($"++++ write message {message}");
                this._writablePipe.WriteMessage(message);
                Log.Logger.Information("---- write message");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "write message");
            }
        }

        private void ConnectionWatchDog()
        {
            while (_readTaskFlag)
            {
                try
                {
                    if (!_readablePipe.IsConnected)
                    {
                        PipesManager.EventMessage = new EventMessage("Connection to CMS Core lost", Severity.Negative);
                        _isConnected = false;
                        _readablePipe.WasConnected = true;
                    }

                    if (!_writablePipe.IsConnected)
                    {
                        PipesManager.EventMessage = new EventMessage("Connection to CMS Core lost", Severity.Negative);
                        _isConnected = false;
                    }
                    Task.Delay(1).Wait();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "IPC-ReadMessage: ");
                }
            }
        }

        /// <summary>
        /// Reads the message from the pipe - connected to the cms core bridge
        /// </summary>
        public async void ReadMessageFromPipe()
        {
            while (_readTaskFlag)
            {
                try
                {
                    Log.Logger.Information("try to read from stream");
                    var message = this._readablePipe.ReadMessage().Result;
                    Log.Logger.Information($"Readmessage {message}");
                    OnMessageReceived(message);
                    Task.Delay(1).Wait();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "IPC-ReadMessage: ");
                }
            }
            Log.Logger.Information("ReadMessageFromPipe - leaving");
        }

        /// <summary>
        /// Called when [pipe message received].
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessageReceived(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                PipeMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                }
                this.StopProcessMessaging();
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~InterprocessPipeProcessor()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
