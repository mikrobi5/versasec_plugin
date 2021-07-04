namespace PipeCommunication.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// event message defined severity
    /// </summary>
    public enum Severity
    {
        Positive,
        Negative
    }

    /// <summary>
    /// 
    /// </summary>
    public class EventMessage
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        /// <value>
        /// The severity.
        /// </value>
        public Severity Severity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventMessage"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity.</param>
        public EventMessage(string message, Severity severity)
        {
            Message = message;

            Severity = severity;
        }
    }
}
