using System.Threading.Tasks;

namespace PipeCommunication.Interfaces
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public interface IPipeReader : IDisposable
    {
        /// <summary>
        /// Reads the message.
        /// </summary>
        /// <returns></returns>
        Task<string> ReadMessage();
    }
}