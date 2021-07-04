using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PipeCommunication.Interfaces;

namespace PipeCommunication
{
    using Serilog;

    /// <summary>
    /// class contains the system std in pipe
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStandardReadablePipe" />
    public class StandardReadablePipe : IStandardReadablePipe
    {
        private bool disposedValue;

        private Stream _currentStream = Console.OpenStandardInput();

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardReadablePipe"/> class.
        /// </summary>
        public StandardReadablePipe()
        {
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            return _currentStream;
        }

        /// <summary>
        /// nothing happens here
        /// </summary>
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
                _currentStream.Dispose();
                disposedValue = true;
            }
        }

        // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        // ~ReadableStream()
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
