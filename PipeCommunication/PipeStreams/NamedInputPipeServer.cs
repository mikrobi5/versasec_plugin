using PipeCommunication.Interfaces;

using System;
using System.IO;

namespace PipeCommunication
{
    using Serilog;

    using System.IO.Pipes;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.INamedInputPipeServer" />
    public class NamedInputPipeServer : INamedInputPipeServer
    {
        private string _pipeName;

        /// <summary>
        /// The result stream in
        /// </summary>
        private NamedPipeServerStream _resultStreamIn;

        /// <summary>
        /// The read cancellation token
        /// </summary>
        private CancellationTokenSource _readCancellationToken;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected => _resultStreamIn.IsConnected;

        /// <summary>
        /// Gets or sets a value indicating whether [was connected].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [was connected]; otherwise, <c>false</c>.
        /// </value>
        public bool WasConnected { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedInputPipeServer"/> class.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public NamedInputPipeServer(string pipeName = "USS-Pipe-In")
        {
            _pipeName = pipeName;
            _resultStreamIn = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _readCancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>
        /// return the readable stream
        /// </returns>
        public Stream GetStream()
        {
          //  _resultStreamIn = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            return _resultStreamIn;
        }

        /// <summary>
        /// Sets the position of the Stream back to zero.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void SetPositionBackToZero()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Waits for connection.
        /// </summary>
        public void WaitForConnection()
        {
            //if (WasConnected)
            //{
            //    _resultStreamIn.Disconnect();
            //    _resultStreamIn.Dispose();
            //    _resultStreamIn.Close();
            //    _resultStreamIn = null;
            //    _resultStreamIn = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            //}
            this._resultStreamIn.WaitForConnection();
        }


        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        /// <returns></returns>
        private async Task<int> GetMessageLength()
        {
            var lengthBuffer = new byte[4];
            await _resultStreamIn.ReadAsync(lengthBuffer, 0, lengthBuffer.Length, _readCancellationToken.Token);
            return BitConverter.ToInt32(lengthBuffer, 0);
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        public async Task<string> ReadMessage()
        {

            if (_resultStreamIn.IsConnected)
            {
                var messageLength = await this.GetMessageLength();
                byte[] buffer = new byte[messageLength];
                //var message = string.Empty;
                Log.Information("wait for message");
                await _resultStreamIn.ReadAsync(buffer, 0, messageLength, _readCancellationToken.Token);
                var message = Encoding.ASCII.GetString(buffer);
                Log.Information($"message received {message}");
                return message;
            }

            return "Disconnected";
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
                _readCancellationToken?.Cancel();
                _resultStreamIn?.Disconnect();
                _resultStreamIn?.Dispose();
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~NamedReadablePipe()
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
    }
}
