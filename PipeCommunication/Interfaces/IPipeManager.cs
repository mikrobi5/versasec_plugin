namespace PipeCommunication.Interfaces
{
    using System.Threading;

    /// <summary>
    /// interface for pipemangers
    /// </summary>
    public interface IPipeManager
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IPipeManager"/> is proxying.
        /// </summary>
        /// <value>
        ///   <c>true</c> if proxying; otherwise, <c>false</c>.
        /// </value>
        bool Proxying { get; set; }

        /// <summary>
        /// Gets the cancellation token source.
        /// </summary>
        /// <value>
        /// The cancellation token source.
        /// </value>
        CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Starts the processing.
        /// </summary>
        /// <returns></returns>
        bool StartProcessing();

        /// <summary>
        /// Stops the processing.
        /// </summary>
        /// <returns></returns>
        bool StopProcessing();

        /// <summary>
        /// Checks the received message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        bool CheckReceivedMessage(string msg);
    }
}