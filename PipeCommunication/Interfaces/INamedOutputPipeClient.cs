namespace PipeCommunication.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IStream" />
    /// <seealso cref="System.IDisposable" />
    public interface INamedInputPipeClient : IStream, IDisposable
    {
        /// <summary>
        /// Waits for connection.
        /// </summary>
        void Connect();

        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns></returns>
        Task<string> ReadMessage();
    }
}