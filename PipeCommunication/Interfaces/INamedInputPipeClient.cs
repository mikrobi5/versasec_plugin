namespace PipeCommunication.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStream" />
    /// <seealso cref="System.IDisposable" />
    public interface INamedOutputPipeClient : IStream, IDisposable
    {
        /// <summary>
        /// Waits for connection.
        /// </summary>
        void Connect();

        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task WriteMessage(string message);
    }
}