namespace native_messaging_example_host.Interfaces
{
    using PipeCommunication.Interfaces;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PipeCommunication.Interfaces.IPipeProcessor" />
    public interface IIpcPipesProcessor : IPipeProcessor
    {
        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; }

    }
}