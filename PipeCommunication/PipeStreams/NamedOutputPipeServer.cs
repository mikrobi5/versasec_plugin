using System;
using System.IO;
using PipeCommunication.Interfaces;

namespace PipeCommunication
{
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Serilog;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.INamedOutputPipeServer" />
    public class NamedOutputPipeServer : INamedOutputPipeServer
    {
        /// <summary>
        /// The result stream out
        /// </summary>
        private NamedPipeServerStream _resultStreamOut;
        private string _pipeName;

        /// <summary>
        /// The write cancellation token
        /// </summary>
        private CancellationTokenSource _writeCancellationToken;

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
        public bool IsConnected => _resultStreamOut.IsConnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedOutputPipeServer"/> class.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public NamedOutputPipeServer(string pipeName = "USS-Pipe-Out")
        {
            _resultStreamOut = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
           _pipeName = pipeName;
            _writeCancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>
        /// return the readable stream
        /// </returns>
        public Stream GetStream()
        {
           // _resultStreamOut = new NamedPipeServerStream(_pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            return _resultStreamOut;
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
            this._resultStreamOut.WaitForConnection();
        }

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public async Task WriteMessage(string message)
        {
            try
            {
                Log.Logger.Information($"NamedOutputPipeServer: ++++ write message {message}");
                var lengthBuffer = new List<byte>
                                       {
                                           (byte)((message.Length >> 0) & 0xFF),
                                           (byte)((message.Length >> 8) & 0xFF),
                                           (byte)((message.Length >> 16) & 0xFF),
                                           (byte)((message.Length >> 24) & 0xFF)
                                       };
                lengthBuffer.AddRange(Encoding.Default.GetBytes(message));
                await _resultStreamOut.WriteAsync(lengthBuffer.ToArray(), 0, lengthBuffer.ToArray().Length, _writeCancellationToken.Token);
                await _resultStreamOut.FlushAsync(_writeCancellationToken.Token);
                Log.Logger.Information("NamedOutputPipeServer: ---- write message");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "NamedOutputPipeServer: write message");
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
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NamedWritablePipe()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
