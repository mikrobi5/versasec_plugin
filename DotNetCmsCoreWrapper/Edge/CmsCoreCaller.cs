
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using VSec.DotNet.CmsCore.Wrapper.Crypto;
using VSec.DotNet.CmsCore.Wrapper.Extensions;
using VSec.DotNet.CmsCore.Wrapper.Models;
using VSec.DotNet.CmsCore.Wrapper.Natives;
using VSec.DotNet.CmsCore.Wrapper.Natives.Delegates;

namespace VSec.DotNet.CmsCore.Wrapper.Edge
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CmsCoreCaller : IDisposable
    {
        private static readonly Lazy<CmsCoreCaller> lazy = new Lazy<CmsCoreCaller>(() => new CmsCoreCaller());

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static CmsCoreCaller _Instance { get { return lazy.Value; } }

        private ulong _cmsCoreInstance = 0L;
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.
        private IntPtr _notifyPtr = IntPtr.Zero;
        private IntPtr _currentCardList = IntPtr.Zero;
        private CardStatusFunctionDelegates _notifyDelegates;

        // private IntPtr _currentCardHandle = IntPtr.Zero;

        private readonly CmsCoreFunctionWrapper _cmsCoreFunctions = null;

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized { get { return _cmsCoreInstance != 0L && _currentCardList != IntPtr.Zero; } }

        /// <summary>
        /// Gets or sets the CMS core card status change notify.
        /// </summary>
        /// <value>
        /// The CMS core card status change notify.
        /// </value>
        public CCmsCoreCardStatusChangeNotify CmsCoreCardStatusChangeNotify { get; set; }

        private CmsCoreCaller()
        {
            _cmsCoreFunctions = new CmsCoreFunctionWrapper();
            try
            {
                if (Initialize())
                {
                    CreateEventAdditionales();
//#if !_X64
                    try
                    {
                        AddCardEvent();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Fatal(ex, "Exception");
                    }
//#endif
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Exception");
                _cmsCoreInstance = 0L;
                _currentCardList = IntPtr.Zero;
                return;
            }

           
            

        }

        private bool Initialize()
        {
            if (_cmsCoreInstance != 0L && _currentCardList != IntPtr.Zero) return true;
            if (!_cmsCoreFunctions.CmsCore_Initialize(out _cmsCoreInstance, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                //Console.WriteLine($"CmsCore_Initialize failed! {error} {errorString}");
                Log.Logger.Error($"CmsCore_Initialize failed! {error} {errorString}");
                return false;
            }
            Log.Logger.Information($"Try to get CardList Handle from CmsCoreInstance {this._cmsCoreInstance}");
            if (!_cmsCoreFunctions.CmsCore_CardList_Get(_cmsCoreInstance, out _currentCardList, out var error2))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error2, out var errorString2);
                //Console.WriteLine($"CmsCore_CardList_Get failed! {error2} {errorString2}");
                Log.Logger.Error($"CmsCore_CardList_Get failed! {error2} {errorString2}");
                return false;
            }
            Log.Logger.Information($"CardList Handle {_currentCardList.ToString()}");
            return true;
        }


        private bool CreateCard(string readerName, out IntPtr cardHandle, out IntPtr coreCardHandle)
        {
            cardHandle = IntPtr.Zero;
            coreCardHandle = IntPtr.Zero;
            if (!_cmsCoreFunctions.CmsCore_CardList_GetCardPcsc(_currentCardList, readerName, out var hPcsc, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Trace.WriteLine($"CmsCore_CardList_GetCardPcsc failed. {errorString}");
                Log.Logger.Error($"CmsCore_CardList_GetCardPcsc failed! {error} {errorString}");
            }
            coreCardHandle = hPcsc;
            if (!_cmsCoreFunctions.CmsCore_Create_CmsCard(hPcsc, out var hCard, out error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                //Console.WriteLine($"Create card failed! {error}  {errorString}");
                Log.Logger.Error($"Create card failed! {error} {errorString}");
                return false;
            }
            cardHandle = hCard;
            return true;
        }

        private bool DeleteCard(IntPtr hCard)
        {
            if (!_cmsCoreFunctions.CmsCore_Delete_CmsCard(hCard, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Debug.WriteLine($"Delete card failed! {error} {errorString}");
                return false;
            }
            return true;
        }

        private void CreateEventAdditionales()
        {
            CmsCoreCardStatusChangeNotify = new CCmsCoreCardStatusChangeNotify();
            _notifyDelegates = CmsCoreCardStatusChangeNotify.GetFunctionDelegates();

        }

        private bool AddCardEvent()
        {
            _notifyPtr = NativeAdditionals.CreateCardStatusUnmanagedDelegate(_notifyDelegates);
            if (!_cmsCoreFunctions.CmsCore_CardList_AddCardStatusNotifier(_currentCardList, _notifyPtr, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Debug.WriteLine($"Add card notify failed! {error} {errorString}");
                _notifyPtr = IntPtr.Zero;
                return false;
            }
            //_cmsCoreCardStatusChangeNotify.RaiseCardAddedEvent += _cmsCoreCardStatusChangeNotify_RaiseCardAddedEvent;
            // _cmsCoreCardStatusChangeNotify.RaiseCardRemovedEvent += _cmsCoreCardStatusChangeNotify_RaiseCardRemovedEvent;
            return true;
        }

        //private void _cmsCoreCardStatusChangeNotify_RaiseCardRemovedEvent(object sender, CardEventArgs a)
        //{
        //    if (_currentCardList != a.CardHandle)
        //    {

        //        // RemoveCardEvent();
        //        // _currentCardList = a.CardHandle;
        //        //  AddCardEvent();
        //    }
        //}

        //private void _cmsCoreCardStatusChangeNotify_RaiseCardAddedEvent(object sender, CardEventArgs a)
        //{
        //    if (_currentCardList != a.CardHandle)
        //    {
        //        GetCardCsn(a.CardHandle, out var csn);
        //        //   RemoveCardEvent();
        //        //  _currentCardList = a.CardHandle;
        //        //   AddCardEvent();
        //    }
        //}

        private bool RemoveCardEvent()
        {
            if (_notifyPtr != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CardList_DelCardStatusNotifier(_currentCardList, _notifyPtr, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"Delete card notify failed! {error} {errorString}");
                    return false;
                }
            }
            _notifyPtr = IntPtr.Zero;
            return true;
        }

        private void Finish()
        {
            _cmsCoreFunctions.CmsCore_Finalize(_cmsCoreInstance);
            _cmsCoreInstance = 0;
            //Marshal.FreeHGlobal(_currentCardList);
            //ReleaseCard();
            _currentCardList = IntPtr.Zero;
            if (Marshal.AreComObjectsAvailableForCleanup())
            {
                Marshal.CleanupUnusedObjectsInCurrentContext();
            }
        }
        //###############################################################################################################################        
        /// <summary>
        /// Initializes the card.
        /// </summary>
        /// <param name="readerName">Name of the reader.</param>
        /// <param name="cardHandle">The card handle.</param>
        /// <returns></returns>
        public bool InitializeCard(string readerName, out IntPtr cardHandle, out IntPtr coreCardHandle)
        {
            if (!CreateCard(readerName, out cardHandle, out coreCardHandle))
            {
                cardHandle = IntPtr.Zero;
                coreCardHandle = IntPtr.Zero;
                return false;
            }

            return IdentifyCard(cardHandle);
        }

        /// <summary>
        /// Releases the card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <returns></returns>
        public bool ReleaseCard(IntPtr cardHandle)
        {
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_Delete_CmsCard(cardHandle, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"Delete card failed! {error} {errorString}");
                    Log.Logger.Error($"Delete card failed! {error} {errorString}");
                    return false;
                }
                Marshal.Release(cardHandle);
                cardHandle = IntPtr.Zero;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Identifies the card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <returns></returns>
        public bool IdentifyCard(IntPtr cardHandle)
        {
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_IdentifyCard(cardHandle, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    //Console.WriteLine($"Identify card failed! {error} {errorString}");
                    Log.Logger.Error($"Identify card failed! {error} {errorString}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the cards count.
        /// </summary>
        /// <param name="cardCount">The card count.</param>
        /// <returns></returns>
        public bool GetCardsCount(out int cardCount)
        {
            cardCount = 0;
            if (!_cmsCoreFunctions.CmsCore_CardList_GetAvailableCardsCount(_currentCardList, "", out var cardsCount, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Debug.WriteLine($"CmsCore_CardList_GetAvailableCardsCount failed! {error} {errorString}");
                Log.Logger.Error($"CmsCore_CardList_GetAvailableCardsCount failed! {error} {errorString}");
                return false;
            }
            cardCount = (int)cardsCount;
            return cardCount != 0;
        }

        /// <summary>
        /// Gets the name of the card reader.
        /// </summary>
        /// <param name="currentCardIndex">Index of the current card.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <returns></returns>
        public bool GetCardReaderName(int currentCardIndex, out string readerName)
        {
            readerName = string.Empty;
            if (!_cmsCoreFunctions.CmsCore_CardList_GetCardReaderName(_currentCardList, (ulong)currentCardIndex, "", out var nativeReaderName, CmsCoreFunctionWrapper.AllocCallback, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Debug.WriteLine($"CmsCore_CardList_GetCardReaderName failed! {error} {errorString}");
                Log.Logger.Error($"CmsCore_CardList_GetCardReaderName failed! {error} {errorString}");
                return false;
            }
            readerName = nativeReaderName;
            return !string.IsNullOrEmpty(readerName);
        }

        /// <summary>
        /// Determines whether [is card available] [the specified reader name].
        /// </summary>
        /// <param name="readerName">Name of the reader.</param>
        /// <returns>
        ///   <c>true</c> if [is card available] [the specified reader name]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCardAvailable(string readerName)
        {
            if (!_cmsCoreFunctions.CmsCore_CardList_IsCardAvailable(_currentCardList, readerName, out var nativeAvailable, out var error))
            {
                _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                Debug.WriteLine($"CmsCore_CardList_IsCardAvailable failed! {error} {errorString}");
                Log.Logger.Error($"CmsCore_CardList_IsCardAvailable failed! {error} {errorString}");
                return false;
            }
            return nativeAvailable;
        }

        /// <summary>
        /// Gets the card identifier.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardIdnetifier">The card idnetifier.</param>
        /// <returns></returns>
        public bool GetCardId(IntPtr cardHandle, out string cardIdnetifier)
        {
            cardIdnetifier = string.Empty;
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_Get_CARDID(cardHandle, out var cardId, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_Get_CARDID failed! {error} {errorString}");
                    Log.Logger.Error($"CmsCore_CmsCard_Get_CARDID failed! {error} {errorString}");
                    return false;
                }
                cardIdnetifier = cardId;
            }
            return true;
        }

        /// <summary>
        /// Gets the card attributes.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardAttributes">The card attributes.</param>
        /// <returns></returns>
        public bool GetCardAttributes(IntPtr cardHandle, out byte[] cardAttributes)
        {
            cardAttributes = new byte[0];
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_Pcsc_GetAttribute(cardHandle, out var nativeCardAttributes, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_Get_CARDID failed! {error} {errorString}");
                    Log.Logger.Error($"CmsCore_CmsCard_Get_CARDID failed! {error} {errorString}");
                    return false;
                }
                cardAttributes = new byte[nativeCardAttributes.Length];
                Array.Copy(nativeCardAttributes, cardAttributes, nativeCardAttributes.Length);
            }
            return true;
        }

        /// <summary>
        /// Gets the card CSN.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardSerialNumber">The card serial number.</param>
        /// <returns></returns>
        public bool GetCardCsn(IntPtr cardHandle, out string cardSerialNumber)
        {
            cardSerialNumber = string.Empty;
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_get_CSN_CSN(cardHandle, out var cardCsn, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_get_CSN_CSN failed! {error} {errorString}");
                    return false;
                }
                cardSerialNumber = cardCsn;
            }
            else
            {
                Debug.WriteLine($"Card not initializeds");
            }
            return !string.IsNullOrEmpty(cardSerialNumber);
        }

        /// <summary>
        /// Gets the card role tries.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="roleTries">The role tries.</param>
        /// <returns></returns>
        public bool GetCardRoleTries(IntPtr cardHandle, ulong role, out ulong roleTries)
        {
            roleTries = 0L;
            if (cardHandle != IntPtr.Zero)
            {
                try
                {
                    if (!_cmsCoreFunctions.CmsCore_CmsCard_GetRoleTriesLeft(cardHandle, role, out var tries, out var error))
                    {
                        _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                        Debug.WriteLine($"CmsCore_CmsCard_GetRoleTriesLeft failed! {error} {errorString}");
                        Log.Logger.Error($"CmsCore_CmsCard_GetRoleTriesLeft failed! {error} {errorString}");
                        return false;
                    }
                    roleTries = tries;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "GetCardRoleTries: ");
                    throw ex;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the card pin information.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinRole">The pin role.</param>
        /// <param name="pinInfo">The pin information.</param>
        /// <returns></returns>
        public bool GetCardPinInfo(IntPtr cardHandle, int pinRole, out PinInfo pinInfo)
        {
            pinInfo = null;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetPinInfo(cardHandle, (ulong)pinRole, out pinInfo, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetPinInfo failed! {error} {errorString}");
                    return false;
                }
                //if (!DeleteCard(_currentCardHandle))
                //{
                //    Debug.WriteLine($"GetCardPinInfo - close card failed");
                //    return false;
                //}
            }

            return true;
        }


        /// <summary>
        /// Gets the card pin policy1.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinRole">The pin role.</param>
        /// <param name="policyBytes">The policy bytes.</param>
        /// <returns></returns>
        public bool GetCardPinPolicy1(IntPtr cardHandle, int pinRole, out byte[] policyBytes)
        {
            policyBytes = null;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_PinPolicy1(cardHandle, (ulong)pinRole, out policyBytes, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_PinPolicy1 failed! {error} {errorString}");
                    return false;
                }

                //if (!DeleteCard(_currentCardHandle))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the card pin policy2.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinRole">The pin role.</param>
        /// <param name="policyBytes">The policy bytes.</param>
        /// <returns></returns>
        public bool GetCardPinPolicy2(IntPtr cardHandle, int pinRole, out byte[] policyBytes)
        {
            policyBytes = null;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_PinPolicy2(cardHandle, (ulong)pinRole, true, out policyBytes, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_PinPolicy2 failed! {error} {errorString}");
                    return false;
                }

                //if (!DeleteCard(_currentCardHandle))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the card admin tries.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="adminTries">The admin tries.</param>
        /// <returns></returns>
        public bool GetCardAdminTries(IntPtr cardHandle, out ulong adminTries)
        {
            adminTries = 0L;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetAdminTriesLeft(cardHandle, out adminTries, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetAdminTriesLeft failed! {error} {errorString}");
                    return false;
                }
                //if (!DeleteCard(_currentCardHandle))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the card free spaces.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardSpaces">The card spaces.</param>
        /// <returns></returns>
        public bool GetCardFreeSpaces(IntPtr cardHandle, out CardSpaces cardSpaces)
        {
            cardSpaces = null;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetFreeSpaces(cardHandle, out cardSpaces, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetFreeSpaces failed! {error} {errorString}");
                    return false;
                }
                //if (!DeleteCard(_currentCardHandle))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the size of the card key.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="keySpecifications">The key specifications.</param>
        /// <param name="cardKeySizes">The card key sizes.</param>
        /// <returns></returns>
        public bool GetCardKeySize(IntPtr cardHandle, ulong keySpecifications, out CardKeySizes cardKeySizes)
        {
            cardKeySizes = null;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetKeySize(cardHandle, keySpecifications, out var ab, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
                cardKeySizes = new CardKeySizes(ab);
                //if (!DeleteCard(_currentCardHandle))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the name of the card pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinRole">The pin role.</param>
        /// <param name="cardPinName">Name of the card pin.</param>
        /// <returns></returns>
        public bool GetCardPinName(IntPtr cardHandle, ulong pinRole, out string cardPinName)
        {
            cardPinName = string.Empty;
            //if (InitializeCard(readerName, out var hCard))
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetCardContainerPinList(cardHandle, true, out var pinSet, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"GetCardPinName - CmsCore_CmsCard_GetCardContainerPinListcard failed! {error} {errorString}");
                }
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetUnblockPinList(cardHandle, pinSet, out var unblockPin, out error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"GetCardPinName - CmsCore_CmsCard_GetUnblockPinListcard failed! {error} {errorString}");
                }
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetPinName(cardHandle, 0, unblockPin, out var ab, out error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"GetCardPinName - CmsCore_CmsCard_GetPinName card failed! {error} {errorString}");
                    return false;
                }
                cardPinName = ab;
                //if (!DeleteCard(hCard))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Gets the card cryptogram.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardCryptogram">The card cryptogram.</param>
        /// <returns></returns>
        public bool GetCardCryptogram(IntPtr cardHandle, out byte[] cardCryptogram)
        {
            cardCryptogram = null;
            // if (InitializeCard(readerName, out var hCard))
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_GetChallenge(cardHandle, out cardCryptogram, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
                //if (!DeleteCard(hCard))
                //{
                //    return false;
                //}
            }
            return true;
        }

        /// <summary>
        /// Logins the role.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="rolePin">The role pin.</param>
        /// <returns></returns>
        public bool LoginRole(IntPtr cardHandle, uint role, string rolePin)
        {
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_LoginRole(cardHandle, role, rolePin, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Logins the admin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <returns></returns>
        public bool LoginAdmin(IntPtr cardHandle)
        {
            if (cardHandle != IntPtr.Zero)
            {
                GetCardCryptogram(cardHandle, out var challengeBlob);
                var cTripleDES = new CmsTripleDES("000000000000000000000000000000000000000000000000");
                var externalOutput = cTripleDES.Encrypt(challengeBlob);
                //Console.WriteLine($"Challenge {string.Concat(challengeBlob.Select(b => b.ToString("X2")).ToArray())}");
                //Console.WriteLine($"Challenge Response {string.Concat(externalOutput.Select(b => b.ToString("X2")).ToArray())}");
                if (!_cmsCoreFunctions.CmsCore_CmsCard_LoginAdmin(cardHandle, externalOutput, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether [is role authenticated] [the specified card handle].
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="authenticated">if set to <c>true</c> [authenticated].</param>
        /// <returns>
        ///   <c>true</c> if [is role authenticated] [the specified card handle]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsRoleAuthenticated(IntPtr cardHandle, ulong role, out bool authenticated)
        {
            authenticated = false;
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_RoleAuthenticated(cardHandle, role, out authenticated, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Changes the role pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="oldPin">The old pin.</param>
        /// <param name="newPin">The new pin.</param>
        /// <returns></returns>
        public bool ChangeRolePin(IntPtr cardHandle, ulong role, string oldPin, string newPin)
        {
            if (cardHandle != IntPtr.Zero)
            {
                if (!_cmsCoreFunctions.CmsCore_CmsCard_ChangeRolePin(cardHandle, role, oldPin, newPin, out var error))
                {
                    _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                    Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Unblocks the user pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="newPin">The new pin.</param>
        /// <param name="triesLeft">The tries left.</param>
        /// <returns></returns>
        public bool UnblockUserPin(IntPtr cardHandle, string newPin, int triesLeft)
        {
            try
            {
                if (cardHandle != IntPtr.Zero)
                {
                    GetCardCryptogram(cardHandle, out var challengeBlob);
                    var cTripleDES = new CmsTripleDES("000000000000000000000000000000000000000000000000");
                    var externalOutput = cTripleDES.Encrypt(challengeBlob);
                    //Console.WriteLine($"Challenge {string.Concat(challengeBlob.Select(b => b.ToString("X2")).ToArray())}");
                    //Console.WriteLine($"Challenge Response {string.Concat(externalOutput.Select(b => b.ToString("X2")).ToArray())}");
                    if (!_cmsCoreFunctions.CmsCore_CmsCard_UnblockUserPin(cardHandle, externalOutput, newPin, triesLeft, out var error))
                    {
                        _cmsCoreFunctions.CmsCore_GetErrorDetails(error, out var errorString);
                        Debug.WriteLine($"CmsCore_CmsCard_GetKeySize failed! {error} {errorString}");
                        return false;
                    }
                }
            }
            finally
            {
                // DeleteCard(hCard);
            }
            return true;
        }


        /// <summary>
        /// Gets the readers.
        /// </summary>
        /// <param name="readers">The readers.</param>
        /// <returns></returns>
        public bool GetReaders(out CCmsCoreReaderList readers)
        {
            readers = new CCmsCoreReaderList();
            //readers = null;
            bool updateAv = false;

            //create delegates
            var functionDelegates = readers.GetFunctionDelegates();

            //create c++ object to pass back to cms core dll
            var newPtr = NativeAdditionals.CreateReaderListUnmanagedDelegate(functionDelegates);
            if (!new CmsCoreFunctionWrapper().CmsCore_CardList_UpdateReaderList(_currentCardList, newPtr, updateAv, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
            }

            if (readers.getCnt() == 0)
            {
                Finish();
                Log.Logger.Error("No Readers found");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the containers.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerIds">The container ids.</param>
        /// <returns></returns>
        public bool GetContainers(IntPtr cardHandle, out uint[] containerIds)
        {
            var coreProgress = new CCmsCoreProgress();
            // readers = null;
            bool updateAv = false;

            //create delegates
            var functionDelegates = coreProgress.GetFunctionDelegates();

            //create c++ object to pass back to cms core dll
            var newPtr = NativeAdditionals.CreateCmsCoreProgressUnmanagedDelegate(functionDelegates);

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersGet(cardHandle, newPtr, out containerIds, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the containers label.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="whichLabel">The which label.</param>
        /// <param name="containerLabel">The container label.</param>
        /// <returns></returns>
        public bool GetContainersLabel(IntPtr cardHandle, uint containerId, int whichLabel, out string containerLabel)
        {

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersGetLabel(cardHandle, containerId, whichLabel, out containerLabel, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the containers certificate.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="containerCertificate">The container certificate.</param>
        /// <returns></returns>
        public bool GetContainersCertificate(IntPtr cardHandle, uint containerId, out byte[] containerCertificate)
        {

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersGetCertificate(cardHandle, containerId, out containerCertificate, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether [is containers default] [the specified card handle].
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="isContainerIsDefault">if set to <c>true</c> [is container is default].</param>
        /// <returns>
        ///   <c>true</c> if [is containers default] [the specified card handle]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsContainersDefault(IntPtr cardHandle, uint containerId, out bool isContainerIsDefault)
        {
            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersGetIsDefault(cardHandle, containerId, out isContainerIsDefault, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Shows the containers certificates.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="enableImport">if set to <c>true</c> [enable import].</param>
        /// <returns></returns>
        public bool ShowContainersCertificates(IntPtr cardHandle, uint containerId, bool enableImport)
        {
            IntPtr hParentWnd = IntPtr.Zero;
            IntPtr pszTitle = IntPtr.Zero;
            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersShowCert(cardHandle, containerId, enableImport, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Sets the container as default.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <returns></returns>
        public bool SetContainerAsDefault(IntPtr cardHandle, uint containerId)
        {

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersSetIsDefault(cardHandle, containerId, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes the containers.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <returns></returns>
        public bool DeleteContainers(IntPtr cardHandle, uint containerId)
        {

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersDelete(cardHandle, containerId, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes all containers.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <returns></returns>
        public bool DeleteAllContainers(IntPtr cardHandle)
        {

            if (!new CmsCoreFunctionWrapper().CmsCore_CmsCard_ContainersDeleteAll(cardHandle, out var error))
            {
                new CmsCoreFunctionWrapper().CmsCore_GetErrorDetails(error, out string errorResult);
                return false;
            }
            return true;
        }


        #region IDisposable Support


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                    //remove events first
                    RemoveCardEvent();
                    Finish();
                }

                // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                // TODO: große Felder auf Null setzen.

                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        // ~CmsCoreCaller()
        // {
        //   // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
        //   Dispose(false);
        // }

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
