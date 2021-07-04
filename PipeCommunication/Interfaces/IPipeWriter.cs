using System.Threading.Tasks;

namespace PipeCommunication.Interfaces
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public interface IPipeWriter : IDisposable
    {
        /// <summary>
        /// Writes the message.
        /// </summary>
        /// <param name="stdOut">The standard out.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        Task WriteMessage(string message);
    }
}