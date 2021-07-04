namespace PipeCommunication.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStream" />
    /// <seealso cref="System.IDisposable" />
    public interface INamedInputPipeServer : IStream, IDisposable
    {

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        bool IsConnected { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [was connected].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [was connected]; otherwise, <c>false</c>.
        /// </value>
        bool WasConnected { get; set; }

        /// <summary>
        /// Waits for connection.
        /// </summary>
        void WaitForConnection();

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns></returns>
        Task<string> ReadMessage();
    }
}