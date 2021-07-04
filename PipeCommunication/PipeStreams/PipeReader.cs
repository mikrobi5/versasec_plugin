using PipeCommunication.Interfaces;

using Serilog;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeCommunication
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IPipeReader" />
    public class PipeReader : IPipeReader
    {
        /// <summary>
        /// The readable stream
        /// </summary>
        private IStandardReadablePipe _readableStream;

        /// <summary>
        /// The standard in
        /// </summary>
        private Stream _stdIn;

        /// <summary>
        /// The read cancellation token
        /// </summary>
        private CancellationTokenSource _readCancellationToken;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;
        private Task<int> _readTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeReader"/> class.
        /// </summary>
        /// <param name="readableStream">The readable stream.</param>
        public PipeReader(IStandardReadablePipe readableStream)
        {
            _readableStream = readableStream;
            _stdIn = this._readableStream.GetStream();
            _readCancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        /// <returns></returns>
        private async Task<int> GetMessageLength()
        {
            var lengthBuffer = new byte[4];
            try
            {
                _readTask = _stdIn.ReadAsync(lengthBuffer, 0, lengthBuffer.Length, _readCancellationToken.Token);
                await this._readTask.ConfigureAwait(false); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return BitConverter.ToInt32(lengthBuffer, 0);
        }

        /// <summary>
        /// Reads the message.
        /// </summary>
        public async Task<string> ReadMessage()
        {
            var message = string.Empty;
            try
            {
                var messageLength = await this.GetMessageLength();
                byte[] buffer = new byte[messageLength];
                Log.Information("wait for message");
                await _stdIn.ReadAsync(buffer, 0, messageLength, _readCancellationToken.Token).ConfigureAwait(false); 
                message = Encoding.ASCII.GetString(buffer);
                Log.Information($"message received {message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return message;
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
                _readTask.GetAwaiter();
                this._readTask = null;
               // this._stdIn.WriteByte(0);
                _readableStream.Dispose();
                _readableStream = null;
                this._stdIn.Close();
                this._stdIn.Dispose();
                this._stdIn = null;
                _readCancellationToken.Dispose();
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~PipeReader()
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
