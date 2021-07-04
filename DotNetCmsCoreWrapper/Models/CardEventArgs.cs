using System;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="a">The <see cref="CardEventArgs"/> instance containing the event data.</param>
    public delegate void CardEventHandler(object sender, CardEventArgs a);

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class CardEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CardEventArgs"/> class.
        /// </summary>
        /// <param name="s">The s.</param>
        public CardEventArgs(string s)
        {
            msg = s;
        }
        private string msg;
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message
        {
            get { return msg; }
        }
        /// <summary>
        /// Gets or sets the card handle.
        /// </summary>
        /// <value>
        /// The card handle.
        /// </value>
        public IntPtr CardHandle { get; set; }
    }
}
