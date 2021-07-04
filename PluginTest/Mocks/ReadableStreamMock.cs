namespace PluginTest.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using PipeCommunication.Interfaces;

    /// <summary>
    /// The readable stream mock.
    /// </summary>
    public sealed class ReadableStreamMock : IStandardReadablePipe
    {
        /// <summary>
        /// The disposed value
        /// </summary>
        private bool _disposedValue = false;

        /// <summary>
        /// The stream
        /// </summary>
        private MemoryStream _stream = null;

        /// <summary>
        /// The message
        /// </summary>
        private string _message = "This is a mega long test string to test reading from a stream.";

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <returns>the Stack representation of the list of bytes containing the message</returns>
        private ReadOnlySpan<byte> GetMessage()
        {
            var messageBuffer = new List<byte>
                                      {
                                          (byte)((_message.Length >> 0) & 0xFF),
                                          (byte)((_message.Length >> 8) & 0xFF),
                                          (byte)((_message.Length >> 16) & 0xFF),
                                          (byte)((_message.Length >> 24) & 0xFF)
                                      };
            messageBuffer.AddRange(Encoding.Default.GetBytes(_message));
            return messageBuffer.ToArray();
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            this._stream = new MemoryStream();
            this._stream.Write(this.GetMessage());
            this._stream.Flush();
            this._stream.Position = 0;
            return this._stream;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                }

                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                this._disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~ReadableStreamMock()
        // {
        //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetPositionBackToZero()
        {
            this._stream.Position = 0;
        }
    }


}