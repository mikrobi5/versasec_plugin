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
    /// <seealso cref="PipeCommunication.Interfaces.INamedInputPipeClient" />
    public class NamedInputPipeClient : INamedInputPipeClient
    {
        private NamedPipeClientStream _resultStreamIn;
        private CancellationTokenSource _readCancellationToken;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedInputPipeClient"/> class.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public NamedInputPipeClient(string pipeName = "USS-Pipe-In")
        {
            _resultStreamIn = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
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
            return _resultStreamIn;
        }

        //public byte[] GetStreamContent()
        //{
        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// Sets the position of the Stream back to zero.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void SetPositionBackToZero()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        public async Task<string> ReadMessage()
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
                _readCancellationToken.Cancel();
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

        /// <summary>
        /// Waits for connection.
        /// </summary>
        public void Connect()
        {
            if (!_resultStreamIn.IsConnected)
            {
                _resultStreamIn.Connect();
            }
            else
            {
                Log.Logger.Information("ReadablePipeClient already connected");
            }
        }
    }
}
