using Serilog;
using System;
using System.Collections.Generic;
using VSec.DotNet.CmsCore.Wrapper.Models;
using VSec.DotNet.CmsCore.Wrapper.Serilog.Extension;

namespace VSec.DotNet.CmsCore.Wrapper.Edge
{
    using System.Runtime.ExceptionServices;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CmsCoreSimples : IDisposable
    {
        private bool _initialized = false;

        /// <summary>
        /// Occurs when [raise card added event].
        /// </summary>
        public event CardEventHandler RaiseCardAddedEvent
        {
            add { Log.Information("add card add event"); if (CmsCoreCaller._Instance.IsInitialized) CmsCoreCaller._Instance.CmsCoreCardStatusChangeNotify.RaiseCardAddedEvent += value; }
            remove { if (CmsCoreCaller._Instance.IsInitialized) CmsCoreCaller._Instance.CmsCoreCardStatusChangeNotify.RaiseCardAddedEvent -= value; }
        }

        /// <summary>
        /// Occurs when [raise card removed event].
        /// </summary>
        public event CardEventHandler RaiseCardRemovedEvent
        {
            add { Log.Information("add card remove event"); if (CmsCoreCaller._Instance.IsInitialized) CmsCoreCaller._Instance.CmsCoreCardStatusChangeNotify.RaiseCardRemovedEvent += value; }
            remove { if (CmsCoreCaller._Instance.IsInitialized) CmsCoreCaller._Instance.CmsCoreCardStatusChangeNotify.RaiseCardRemovedEvent -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmsCoreSimples"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.NullReferenceException">Simple not initialized - native dll access not possible</exception>
        public CmsCoreSimples(ILogger logger = null)
        {
            SerilogExtension.AddLogger(logger);
            if (CmsCoreCaller._Instance.IsInitialized)
            {
                _initialized = true;
                Log.Logger.Information("Simple initialized");
            }
            else
            {
                Log.Logger.Error("Simple NOT initialized");
                throw new System.NullReferenceException("VSEC - Simple not initialized - native dll access not possible");
            }
        }

        /// <summary>
        /// Gets the cards.
        /// </summary>
        /// <returns></returns>
        public IList<SmartCard> GetCards()
        {
            if (!_initialized)
            {
                Log.Logger.Error("Simple NOT initialized");
                return null;
            }
            var cards = new List<SmartCard>();
            Log.Logger.Information($"GetCards - try to get cards");
            var readers = GetReaders();
            if (readers != null)
            {
                try
                {
                    Log.Logger.Information($"GetCards - go through cards");
                    for (int i = 0; i < readers.Count; i++)
                    {
                        Log.Logger.Information($"GetCards - check reader no. {i}");
                        var available = string.IsNullOrEmpty(readers[i].Name) ? false : CmsCoreCaller._Instance.IsCardAvailable(readers[i].Name);
                        Log.Logger.Information($"GetCards - check reader card available {available}");
                        if (CmsCoreCaller._Instance.InitializeCard(readers[i].Name, out var cardHandle, out var coreHandle))
                        {
                            Log.Logger.Information($"GetCards - new card with handle {cardHandle}");
                            cards.Add(new SmartCard(cardHandle, coreHandle) { Index = i + 1, IsAvailable = available, ReaderName = readers[i].Name });
                        }
                        else
                        {
                            Log.Logger.Error("GetCards - NO new card ");
                        }
                    }
                    Log.Logger.Information("GetCards - cards to array");
#if DEBUG
                    cards.Add(new SmartCard(IntPtr.Zero, IntPtr.Zero));
#endif
                   // var res = cards;
                    if (cards?.Count > 0)
                    {
                        Log.Logger.Information($"GetCards - fill up cards {cards?.Count}");
                        cards = FillUpCards(cards);
                    }
                    else
                    {
                        Log.Logger.Error($"GetCards - No Cards Found");
                    }
                    Log.Logger.Information($"GetCards - return  cards {cards?.Count}");

                    return cards.ToArray();
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "GetCards - exception");
                }
            }
            Log.Logger.Information("GetCards - return without cards");
            return null;
        }

        // [HandleProcessCorruptedStateExceptions]
        private List<SmartCard> FillUpCards(IList<SmartCard> cards)
        {
            Log.Logger.Information($"FillUpCard - start");
            var returnCards = new List<SmartCard>();
            try
            {
                Log.Logger.Information($"FillUpCard - Cards {cards?.Count}");
                foreach (var card in cards)
                {
                    try
                    {
                        CmsCoreCaller._Instance.GetCardCsn(card.Handle, out var cardCsn);
                        card.Csn = cardCsn;
                        Log.Logger.Information($"Card {card.Csn} - cardCsn {cardCsn}");
                        card.IsAvailable = CmsCoreCaller._Instance.IsCardAvailable(card.ReaderName);
                        //if (string.IsNullOrWhiteSpace(card.Csn) || !card.IsAvailable) continue;
                        Log.Logger.Information($"Card {card.Csn} - available");
                        if (!string.IsNullOrWhiteSpace(card.ReaderName) && !card.ReaderName.Contains("Yubikey"))
                        {
                            CmsCoreCaller._Instance.GetCardRoleTries(card.Handle, 1, out var rolesTries);
                            card.RoleTries = rolesTries;
                            Log.Logger.Information($"Card {card.Csn} - rolesTries {rolesTries}");


                            CmsCoreCaller._Instance.GetCardAdminTries(card.Handle, out var adminTries);
                            card.AdminTries = adminTries;
                            Log.Logger.Information($"Card {card.Csn} - adminTries {adminTries}");


                            CmsCoreCaller._Instance.GetCardId(card.Handle, out var cardIdentifier);
                            card.CardId = cardIdentifier;
                            Log.Logger.Information($"Card {card.Csn} - cardIdentifier {cardIdentifier}");

                            CmsCoreCaller._Instance.GetCardKeySize(card.Handle, 1, out var keysizes);
                            card.KeySizes = keysizes;
                            Log.Logger.Information($"Card {card.Csn} - keysizes {keysizes}");

                            CmsCoreCaller._Instance.GetCardFreeSpaces(card.Handle, out var cardSpaces);
                            card.FreeSpace = cardSpaces;
                            Log.Logger.Information($"Card {card.Csn} - cardSpaces {cardSpaces}");

                            CmsCoreCaller._Instance.GetCardPinPolicy1(card.Handle, 1, out var policyBytes1);
                            card.PolicyBytesOne = policyBytes1;
                            Log.Logger.Information($"Card {card.Csn} - policyBytes1 {policyBytes1}");
                            CmsCoreCaller._Instance.GetCardPinPolicy2(card.Handle, 1, out var policyBytes2);
                            card.PolicyBytesTwo = policyBytes2;
                            Log.Logger.Information($"Card {card.Csn} - policyBytes2 {policyBytes2}");

                        }

                        CmsCoreCaller._Instance.GetCardAttributes(card.PcScHandle, out var cardAttributes);

                        CmsCoreCaller._Instance.GetCardPinName(card.Handle, 1, out var pinName);
                        card.PinName = pinName;
                        Log.Logger.Information($"Card {card.Csn} - pinName {pinName}");

                        CmsCoreCaller._Instance.GetCardPinInfo(card.Handle, 1, out var pinInfo);
                        card.PinInfo = pinInfo;
                        Log.Logger.Information($"Card {card.Csn} - pinInfo {pinInfo}");

                        returnCards.Add(card);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "FillUpCards - exception");
                    }
                }
                Log.Logger.Information("FillUpCards - end");
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "FillUpCards - exception");
            }
            return returnCards;
        }

        /// <summary>
        /// Gets the readers.
        /// </summary>
        /// <returns></returns>
        public IList<CardReader> GetReaders()
        {
            if (!_initialized)
            {
                Log.Logger.Error("Simple NOT initialized");
                return null;
            }
            IList<CardReader> result = null;
            if (CmsCoreCaller._Instance.GetReaders(out var readersList))
            {
                Log.Logger.Information("GetReaders - readerslist found");
                if (CmsCoreCaller._Instance.GetCardsCount(out var cardCount))
                {
                    Log.Logger.Information($"GetReaders - cards found {cardCount}");
                    result = new List<CardReader>();
                    for (int i = 0; i < cardCount; i++)
                    {
                        Log.Logger.Information($"GetReaders - try to get card {i}");
                        if (CmsCoreCaller._Instance.GetCardReaderName(i, out var readersName))
                        {
                            Log.Logger.Information($"GetReaders - reader name {readersName}");
                            if (readersList.FindReader(readersName))
                            {
                                var reader = new CardReader
                                {
                                    Name = readersName
                                };
                                result.Add(reader);
                            }
                            else
                            {
                                Log.Logger.Error($"reader not found on index {i}");
                            }
                        }
                        else
                        {
                            Log.Logger.Error($"card reader name not found on index {i}");
                        }
                    }
                }
                else
                {
                    Log.Logger.Error("card count is zero");
                }
                return result;
            }
            else
            {
                Log.Logger.Error("no readers found");
            }
            return null;

        }

        /// <summary>
        /// to call from another assembly and test if the into call operates
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public string InstanceTestCall(string input = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return "I came from instanciated TestCall";
            }
            return input;
        }

        /// <summary>
        /// to call from another assembly and test if the into call operates
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string StaticTestCall(string input = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return "I came from static TestCall";
            }
            return input;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                    CmsCoreCaller._Instance.Dispose();
                }

                // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                // TODO: große Felder auf Null setzen.

                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        ~CmsCoreSimples()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(false);
        }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.        
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(true);
            // TODO: Auskommentierung der folgenden Zeile aufheben, wenn der Finalizer weiter oben überschrieben wird.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
