using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces.ICmsCoreCardStatusChangeNotify" />
    public class CCmsCoreCardStatusChangeNotify : ICmsCoreCardStatusChangeNotify
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CCmsCoreCardStatusChangeNotify"/> class.
        /// </summary>
        public CCmsCoreCardStatusChangeNotify()
        {
        }

        /// <summary>
        /// Called when [card insert].
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        public override void OnCardInsert(IntPtr cardHandle)
        {
            OnRaiseCardAddedEvent(new CardEventArgs("Card Added") { CardHandle = cardHandle });
            Trace.WriteLine("Card inseretd");
            Log.Logger.Error($"Card inserted {cardHandle}");
        }

        /// <summary>
        /// Called when [card remove].
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        public override void OnCardRemove(IntPtr cardHandle)
        {
            OnRaiseCardRemovedEvent(new CardEventArgs("Card Removed") { CardHandle = cardHandle });
            Trace.WriteLine("Card removed");
            Log.Logger.Error($"Card removed {cardHandle}");
        }

        /// <summary>
        /// Occurs when [raise card added event].
        /// </summary>
        public event CardEventHandler RaiseCardAddedEvent;
        /// <summary>
        /// Occurs when [raise card removed event].
        /// </summary>
        public event CardEventHandler RaiseCardRemovedEvent;
        /// <summary>
        /// Raises the <see cref="E:RaiseCardAddedEvent" /> event.
        /// </summary>
        /// <param name="e">The <see cref="CardEventArgs"/> instance containing the event data.</param>
        protected void OnRaiseCardAddedEvent(CardEventArgs e)
        {
            RaiseCardAddedEvent?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the <see cref="E:RaiseCardRemovedEvent" /> event.
        /// </summary>
        /// <param name="e">The <see cref="CardEventArgs"/> instance containing the event data.</param>
        protected void OnRaiseCardRemovedEvent(CardEventArgs e)
        {
            RaiseCardRemovedEvent?.Invoke(this, e);
        }
    }
}
