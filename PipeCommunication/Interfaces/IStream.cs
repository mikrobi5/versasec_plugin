using System.IO;

namespace PipeCommunication.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStream
    {
        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <returns>return the readable stream</returns>
        Stream GetStream();

        /// <summary>
        /// Sets the position of the Stream back to zero.
        /// </summary>
        void SetPositionBackToZero();
    }
}