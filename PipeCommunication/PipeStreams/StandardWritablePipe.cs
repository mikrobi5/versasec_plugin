using PipeCommunication.Interfaces;

using System;
using System.IO;

namespace PipeCommunication
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStandardWritablePipe" />
    public class StandardWritablePipe : IStandardWritablePipe
    {
        private bool disposedValue;

        private Stream _currentStream = Console.OpenStandardOutput();

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardWritablePipe"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public StandardWritablePipe()
        {
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>
        /// return the readable stream
        /// </returns>
        public Stream GetStream()
        {
            return this._currentStream;
        }

        /// <summary>
        /// Gets the content of the stream.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[] GetStreamContent()
        {
            throw new NotImplementedException();
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
                _currentStream.Close();
                
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~StandardWritablePipe()
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
