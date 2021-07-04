namespace PipeCommunication.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStream" />
    /// <seealso cref="System.IDisposable" />
    public interface INamedOutputPipeServer : IStream, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        bool IsConnected { get; }

        /// <summary>
        /// Waits for connection.
        /// </summary>
        void WaitForConnection();

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task WriteMessage(string message);
    }
}