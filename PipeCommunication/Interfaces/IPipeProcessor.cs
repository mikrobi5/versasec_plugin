namespace PipeCommunication.Interfaces
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">The message.</param>
    public delegate void PipeMessageProcessing(string message);


    /// <summary>
    /// interface for IPipeProcessor
    /// </summary>
    public interface IPipeProcessor : IDisposable
    {
        /// <summary>
        /// Occurs when [pipe message received].
        /// </summary>
        public event PipeMessageProcessing PipeMessageReceived;

        /// <summary>
        /// Starts the process messaging.
        /// </summary>
        void StartProcessMessaging();

        /// <summary>
        /// Stops the process messaging.
        /// </summary>
        void StopProcessMessaging();

        /// <summary>
        /// Stops the read task.
        /// </summary>
        void StopReadTask();

        /// <summary>
        /// Writes the message to pipe.
        /// </summary>
        /// <param name="message">The message.</param>
        void WriteMessageToPipe(string message);

        /// <summary>
        /// Reads the message from pipe.
        /// </summary>
        /// <returns></returns>
        void ReadMessageFromPipe();
    }
}