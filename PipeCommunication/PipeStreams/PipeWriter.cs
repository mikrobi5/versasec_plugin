using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipeCommunication.Interfaces;

namespace PipeCommunication
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IPipeWriter" />
    public class PipeWriter : IPipeWriter
    {
        /// <summary>
        /// The writeable stream
        /// </summary>
        private readonly IStandardWritablePipe _writableStream;

        /// <summary>
        /// The stream
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// The write cancellation token
        /// </summary>
        private readonly CancellationTokenSource _writeCancellationToken;

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeWriter"/> class.
        /// </summary>
        /// <param name="writeableStream">The writable stream.</param>
        public PipeWriter(IStandardWritablePipe writableStream)
        {
            _writableStream = writableStream;
            _stream = _writableStream.GetStream();
            _writeCancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public async Task WriteMessage(string message)
        {
            try
            {
                Log.Logger.Information($"PipeWriter: ++++ write message {message}");
                var lengthBuffer = new List<byte>
                {
                    (byte)((message.Length >> 0) & 0xFF),
                    (byte)((message.Length >> 8) & 0xFF),
                    (byte)((message.Length >> 16) & 0xFF),
                    (byte)((message.Length >> 24) & 0xFF)
                };
                lengthBuffer.AddRange(Encoding.Default.GetBytes(message));
                await _stream.WriteAsync(lengthBuffer.ToArray(), 0, lengthBuffer.ToArray().Length, _writeCancellationToken.Token);
                await _stream.FlushAsync(_writeCancellationToken.Token);
                Log.Logger.Information("PipeWriter: ---- write message");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "PipeWriter: write message");
            }
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
                _writableStream.Dispose();
                
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~PipeWriter()
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
