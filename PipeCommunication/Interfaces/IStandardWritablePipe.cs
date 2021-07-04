namespace PipeCommunication.Interfaces
{
    using System;

    public interface IStandardWritablePipe : IStream, IDisposable
    {
        /// <summary>
        /// Gets the content of the stream.
        /// </summary>
        /// <returns></returns>
        byte[] GetStreamContent();
    }
}