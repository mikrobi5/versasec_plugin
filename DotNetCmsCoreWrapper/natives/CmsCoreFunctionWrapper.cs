
namespace VSec.DotNet.CmsCore.Wrapper.Natives
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    using global::Serilog;

    using VSec.DotNet.CmsCore.Wrapper.Models;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;

#if !_WIN32
    using NativeInteger = System.Int64;
    using NativeUnsignedInteger = System.UInt64;
#else
    using NativeInteger = System.Int32;
    using NativeUnsignedInteger = System.UInt32;
#endif

    /// <summary>
    /// mapps the native cms functions to more c# like behaviour
    /// </summary>
    public class CmsCoreFunctionWrapper
    {
        /// <summary>
        /// Allocs memory nativly to fill different objects
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public static IntPtr AllocCallback(int size)
        {
            IntPtr pointer = Marshal.AllocHGlobal(size * IntPtr.Size);
            return pointer;
        }

        /// <summary>
        /// initialize the CMS core 
        /// </summary>
        /// <param name="instanceHandle">The instance handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_Initialize(out ulong instanceHandle, out ulong errorCode)
        {
            instanceHandle = 0L;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_Initialize(out var nativeInstanceHandle, out var nativeError, AllocCallback))
            {
                errorCode = 0;
                var errorString = Marshal.PtrToStringUni(nativeError);
                return false;
            }
            instanceHandle = (ulong)nativeInstanceHandle;
            return true;
        }

        /// <summary>
        /// finalize the CMS core
        /// </summary>
        /// <param name="instanceHandle">The instance handle.</param>
        /// <returns></returns>
        public bool CmsCore_Finalize(ulong instanceHandle)
        {
            if (instanceHandle == 0L) return false;
            return CmsCoreNativeImports.CmsCore_Finalize((NativeUnsignedInteger)instanceHandle);
        }

        /// <summary>
        /// gets detailed CMS core error .
        /// </summary>
        /// <param name="err">The error.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public bool CmsCore_GetErrorDetails(ulong err, out string errorMessage)
        {
            errorMessage = null;
            var castedError = (NativeUnsignedInteger)err;
            if (!CmsCoreNativeImports.CmsCore_GetErrorDetails(castedError, out IntPtr pOutErrStr, AllocCallback))
            {
                return false;
            }
            errorMessage = Marshal.PtrToStringUni(pOutErrStr);
            return string.IsNullOrEmpty(errorMessage);
        }

        /// <summary>
        /// gets CMS core configuration parameters - not working yet
        /// </summary>
        /// <param name="configFile">The configuration file.</param>
        /// <param name="configKey">The configuration key.</param>
        /// <param name="configValue">The configuration value.</param>
        /// <param name="p">The p.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_GetConfigParameters(string configFile, string configKey, out string configValue, CmsCoreNativeImports.CmsCallback p, out ulong errorCode)
        {
            configValue = "";
            var pointerConfigFile = Marshal.StringToHGlobalUni(configFile);
            var pointerConfigKey = Marshal.StringToHGlobalUni(configKey);
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_GetConfigParam(pointerConfigFile, pointerConfigKey, out var intPtr, p, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            configValue = Marshal.PtrToStringUni(intPtr);
            return !string.IsNullOrEmpty(configValue);
        }

        /// <summary>
        /// get CMS core license parameters.
        /// </summary>
        /// <param name="licenseFile">The license file.</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="privateKey">The private key.</param>
        /// <param name="configValue">The configuration value.</param>
        /// <param name="p">The p.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_GetLicenseParameters(string licenseFile, string publicKey, string privateKey, out string configValue, CmsCoreNativeImports.CmsCallback p, out ulong errorCode)
        {
            configValue = "";
            var pointerLicenseFile = Marshal.StringToHGlobalUni(licenseFile);
            var pointerPublicKey = Marshal.StringToHGlobalUni(publicKey);
            var pointerPrivateKey = Marshal.StringToHGlobalUni(privateKey);
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_GetLicenseParam(pointerLicenseFile, pointerPublicKey, pointerPrivateKey, out var intPtr, p, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            configValue = Marshal.PtrToStringUni(intPtr);
            return !string.IsNullOrEmpty(configValue);
        }

        /// <summary>
        /// get CMS core card list
        /// </summary>
        /// <param name="instanceHandle">The instance handle.</param>
        /// <param name="currentCardList">The current card list.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_Get(ulong instanceHandle, out IntPtr currentCardList, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CardList_Get((NativeUnsignedInteger)instanceHandle, out currentCardList, out var nativeError, AllocCallback);
            errorCode = 0;
            var configValue = Marshal.PtrToStringUni(nativeError);
            return result;
        }

        /// <summary>CMSs the core card list update reader list.</summary>
        /// <param name="cardListHandle">The card list handle.</param>
        /// <param name="readerListHandle">The reader list handle.</param>
        /// <param name="cardPrinterOnly">if set to <c>true</c> [card printer only].</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_UpdateReaderList(IntPtr cardListHandle, IntPtr readerListHandle, bool cardPrinterOnly, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CardList_UpdateReaderList(cardListHandle, readerListHandle, cardPrinterOnly, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core card list get available cards count.
        /// </summary>
        /// <param name="cardListHandle">The card list handle.</param>
        /// <param name="readerToSkip">The reader to skip.</param>
        /// <param name="readersCount">The readers count.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_GetAvailableCardsCount(IntPtr cardListHandle, string readerToSkip, out ulong readersCount, out ulong errorCode)
        {
            var pointerSkipReader = Marshal.StringToHGlobalUni(readerToSkip);
            readersCount = 0;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CardList_GetAvailableCardsCnt(cardListHandle, pointerSkipReader, out var outCnt, out var nativeError))
            {
                errorCode = nativeError;
                Marshal.FreeHGlobal(pointerSkipReader);
                return false;
            }
            Marshal.FreeHGlobal(pointerSkipReader);
            readersCount = (ulong)outCnt;
            return true;
        }

        /// <summary>
        /// CMSs the core card list get card PCSC.
        /// </summary>
        /// <param name="cardListHandle">The card list handle.</param>
        /// <param name="pszReaderName">Name of the PSZ reader.</param>
        /// <param name="pOutPcsc">The p out PCSC.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_GetCardPcsc(IntPtr cardListHandle, string pszReaderName, out IntPtr pOutPcsc, out ulong errorCode)
        {
            var pointerSkipReader = Marshal.StringToHGlobalUni(pszReaderName);
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CardList_GetCardPcsc(cardListHandle, pointerSkipReader, out pOutPcsc, out var nativeError))
            {
                errorCode = nativeError;
                Marshal.FreeHGlobal(pointerSkipReader);
                return false;
            }
            Marshal.FreeHGlobal(pointerSkipReader);
            return true;
        }

        /// <summary>
        /// CMSs the core card list is card available.
        /// </summary>
        /// <param name="cardListHandle">The card list handle.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <param name="pOutAvail">if set to <c>true</c> [p out avail].</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_IsCardAvailable(IntPtr cardListHandle, string readerName, out bool pOutAvailable, out ulong errorCode)
        {
            var pointerReaderName = Marshal.StringToHGlobalUni(readerName);
            errorCode = 0;
            pOutAvailable = false;
            if (!CmsCoreNativeImports.CmsCore_CardList_isCardAvailable(cardListHandle, pointerReaderName, out var pOutAvail, out var nativeError))
            {
                errorCode = nativeError;
                Marshal.FreeHGlobal(pointerReaderName);
                return false;
            }
            pOutAvailable = Convert.ToBoolean((int)pOutAvail);
            Marshal.FreeHGlobal(pointerReaderName);
            return true;
        }


        public bool CmsCore_CardList_AddCardStatusNotifier(IntPtr cardListHandle, IntPtr p, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CardList_AddCardStatusNotifier(cardListHandle, p, out IntPtr nativeError, AllocCallback))
            {
                var readerName = Marshal.PtrToStringUni(nativeError);
                errorCode = 0;
                return false;
            }
            return true;
        }


        public bool CmsCore_CardList_DelCardStatusNotifier(IntPtr cardListHandle, IntPtr p, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CardList_DelCardStatusNotifier(cardListHandle, p, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }



        /// <summary>
        /// CMSs the name of the core card list get card reader.
        /// </summary>
        /// <param name="cardListHandle">The card list handle.</param>
        /// <param name="index">The index.</param>
        /// <param name="readerToSkip">The reader to skip.</param>
        /// <param name="readerName">Name of the reader.</param>
        /// <param name="p">The p.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CardList_GetCardReaderName(IntPtr cardListHandle, ulong index, string readerToSkip, out string readerName, CmsCoreNativeImports.CmsCallback p, out ulong errorCode)
        {
            readerName = "";
            errorCode = 0;
            var pointerSkipReader = Marshal.StringToHGlobalUni(readerToSkip);
            var pOutReaderName = Marshal.StringToHGlobalUni(readerName);
            if (!CmsCoreNativeImports.CmsCore_CardList_GetCardReaderName(cardListHandle, (NativeUnsignedInteger)index, pointerSkipReader, out pOutReaderName, p, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            readerName = Marshal.PtrToStringUni(pOutReaderName);
            Marshal.FreeHGlobal(pOutReaderName);
            return true;
        }

        /// <summary>
        /// CMSs the core PCSC get attribute.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardAtr">The card atr.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_Pcsc_GetAttribute(IntPtr cardHandle, out byte[] cardAtr, out ulong errorCode)
        {
            cardAtr = null;
            errorCode = 0;
            if (cardHandle == IntPtr.Zero) return false;
            if (!CmsCoreNativeImports.CmsCore_Pcsc_GetAtr(cardHandle, out IntPtr pOutData, out IntPtr pOutDataSize, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
            }
            var size = (uint)pOutDataSize;
            cardAtr = new byte[size];
            Marshal.Copy(pOutData, cardAtr, 0, (int)size);
            Marshal.FreeHGlobal(pOutData);
            return cardAtr != null && cardAtr.Length > 0;
        }

        /// <summary>
        /// CMSs the core CMS card get cardid.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="cardId">The card identifier.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_Get_CARDID(IntPtr hCard, out string cardId, out ulong errorCode)
        {
            cardId = null;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_get_CSN_CARDID(hCard, out IntPtr pOutCSN, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            cardId = Marshal.PtrToStringUni(pOutCSN);
            Marshal.FreeHGlobal(pOutCSN);
            return !string.IsNullOrEmpty(cardId);
        }

        /// <summary>
        /// CMSs the core CMS card get CSN CSN.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cardCsn">The card CSN.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_get_CSN_CSN(IntPtr cardHandle, out string cardCsn, out ulong errorCode)
        {
            cardCsn = null;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_get_CSN_CSN(cardHandle, out IntPtr pOutCSN, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;

            }
            cardCsn = Marshal.PtrToStringUni(pOutCSN);
            Marshal.FreeHGlobal(pOutCSN);
            return !string.IsNullOrEmpty(cardCsn);
        }

        /// <summary>
        /// CMSs the core CMS card get admin tries left.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="adminTriesCount">The admin tries count.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetAdminTriesLeft(IntPtr cardHandle, out ulong adminTriesCount, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_getAdminTriesLeft(cardHandle, out var triesLeft, out var nativeError);
            //adminTriesCount = (ulong)Marshal.ReadInt64(triesLeft);
            adminTriesCount = (ulong)triesLeft;
            errorCode = nativeError;
            return result;
        }


        /// <summary>
        /// CMSs the core CMS card get role tries left.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="roleTriesCount">The role tries count.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetRoleTriesLeft(IntPtr cardHandle, ulong role, out ulong roleTriesCount, out ulong errorCode)
        {
            try
            {
                
                var result = CmsCoreNativeImports.CmsCore_CmsCard_getRoleTriesLeft(cardHandle, (uint)role, out var triesLeft, out var nativeError, AllocCallback);
                roleTriesCount = (ulong)triesLeft;
                var errorMessage = Marshal.PtrToStringUni(nativeError);
                errorCode = 0;
                return result;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CmsCore_CmsCard_GetRoleTriesLeft");
                throw ex;
            }
        }


        /// <summary>
        /// CMSs the core CMS card create file.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="dwInitialSize">Initial size of the dw.</param>
        /// <param name="ac">The ac.</param>
        /// <param name="bFailIfExists">if set to <c>true</c> [b fail if exists].</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_CreateFile(IntPtr hCard, string directoryName, string fileName, long dwInitialSize, CARD_FILE_ACCESS_CONDITION ac, bool bFailIfExists, out ulong errorCode)
        {
            var directoryPointer = Marshal.StringToHGlobalUni(directoryName);
            var fileNamePointer = Marshal.StringToHGlobalUni(fileName);
            errorCode = 0;
            //IntPtr pRetErr = new IntPtr(returnedError);
            var result = CmsCoreNativeImports.CmsCore_CmsCard_createFile(hCard, directoryPointer, fileNamePointer, (NativeInteger)dwInitialSize, ac, bFailIfExists, out NativeUnsignedInteger pRetErr);
            errorCode = pRetErr;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card get directories.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="pOutString">The p out string.</param>
        /// <param name="directoryCount">The directory count.</param>
        /// <param name="p">The p.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetDirectories(IntPtr hCard, out IEnumerable<string> pOutString, out ulong directoryCount, CmsCoreNativeImports.CmsCallback p, out ulong errorCode)
        {
            throw new NotImplementedException();
            //directoryCount = 0;
            //var result = CmsCoreNativeImports.CmsCore_CmsCard_getDirectories(hCard, out IntPtr pOutStr, out var dwOutCnt, p, out var nativeError);
            //IntPtr[] pIntPtrArray = new IntPtr[dwOutCnt];
            //var managedStringArray = new string[dwOutCnt];
            //Marshal.Copy(pOutStr, pIntPtrArray, 0, (int)dwOutCnt);
            //errorCode = 0;
            //for (NativeUnsignedInteger i = 0; i < dwOutCnt; i++)
            //{
            //    managedStringArray[i] = Marshal.PtrToStringUni(pIntPtrArray[i]);
            //}
            //pOutString = managedStringArray;
            //Marshal.FreeCoTaskMem(pOutStr);
            //directoryCount = dwOutCnt;
            //return result;
        }

        /// <summary>
        /// CMSs the core CMS card create directory.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="bAdminOnly">if set to <c>true</c> [b admin only].</param>
        /// <param name="bFailIfExists">if set to <c>true</c> [b fail if exists].</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_CreateDirectory(IntPtr hCard, string directoryName, bool bAdminOnly, bool bFailIfExists, out ulong errorCode)
        {
            var stringPointer = Marshal.StringToHGlobalUni(directoryName);
            errorCode = 0;
            var result = CmsCoreNativeImports.CmsCore_CmsCard_createDirectory(hCard, stringPointer, bAdminOnly, bFailIfExists, out var nativeError);
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card delete directory.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_DeleteDirectory(IntPtr hCard, string directoryName, out ulong errorCode)
        {
            errorCode = 0;
            var stringPointer = Marshal.StringToHGlobalUni(directoryName);
            var result = CmsCoreNativeImports.CmsCore_CmsCard_deleteDirectory(hCard, stringPointer, out var nativeError);
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card list files.
        /// </summary>
        /// <param name="hCard">The h card.</param>
        /// <param name="pszDirName">Name of the PSZ dir.</param>
        /// <param name="pOutString">The p out string.</param>
        /// <param name="fileCount">The file count.</param>
        /// <param name="p">The p.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_ListFiles(IntPtr hCard, string pszDirName, out string pOutString, out ulong fileCount, CmsCoreNativeImports.CmsCallback p, out ulong errorCode)
        {
            errorCode = 0;
            fileCount = 0;
            var stringPointer = Marshal.StringToHGlobalUni(pszDirName);
            var result = CmsCoreNativeImports.CmsCore_CmsCard_listFiles(hCard, stringPointer, out IntPtr pOutStr, out var dwOutCnt, p, out var nativeError);
            fileCount = (ulong)Marshal.ReadInt64(dwOutCnt);
            pOutString = Marshal.PtrToStringUni(pOutStr);
            return result;
        }

        /// <summary>
        /// CMSs the core create CMS card.
        /// </summary>
        /// <param name="pcscHandle">The PCSC handle.</param>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_Create_CmsCard(IntPtr pcscHandle, out IntPtr cardHandle, out ulong errorCode)
        {
            errorCode = 0;
            var result = CmsCoreNativeImports.CmsCore_Create_CmsCard(pcscHandle, out cardHandle, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core delete CMS card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_Delete_CmsCard(IntPtr cardHandle, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_Delete_CmsCard(cardHandle, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card identify card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_IdentifyCard(IntPtr cardHandle, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_identifyCard(cardHandle, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card lock card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_LockCard(IntPtr cardHandle, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_lockCard(cardHandle, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card unlock card.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_UnlockCard(IntPtr cardHandle, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_unlockCard(cardHandle, out var nativeError);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card get card container pin list.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="addPrimaryUserPin">if set to <c>true</c> [add primary user pin].</param>
        /// <param name="pOut">The p out.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetCardContainerPinList(IntPtr cardHandle, bool addPrimaryUserPin, out ulong pOut, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_getCardContainerPinList(cardHandle, addPrimaryUserPin, out var output, out var nativeError);
            errorCode = nativeError;
            //pOut = (ulong)Marshal.ReadInt64(output);
            pOut = (ulong)output;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card get unblock pin list.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinList">The pin list.</param>
        /// <param name="pOut">The p out.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetUnblockPinList(IntPtr cardHandle, ulong pinList, out ulong pOut, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_getUnblockPinList(cardHandle, (NativeUnsignedInteger)pinList, out var output, out var nativeError);
            errorCode = nativeError;
            //pOut = (ulong)Marshal.ReadInt64(output);
            pOut = (ulong)output;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card get free spaces.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pOut">The p out.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetFreeSpaces(IntPtr cardHandle, out CardSpaces pOut, out ulong errorCode)
        {
            NativeUnsignedInteger availableBytes = 0, availableKeyContainer = 0, maxKeyContainer = 0;
            pOut = null;
            if (CmsCoreNativeImports.CmsCore_CmsCard_getFreeSpaces(cardHandle, ref availableBytes, ref availableKeyContainer, ref maxKeyContainer, out var nativeError))
            {
                errorCode = 0;
                pOut = new CardSpaces()
                {
                    AvailableBytes = (NativeInteger)availableBytes,
                    AvailableKeyContainer = (NativeInteger)availableKeyContainer,
                    MaxKeyContainer = (NativeInteger)maxKeyContainer
                };
                return true;
            }
            errorCode = nativeError;
            return false;
        }

        /// <summary>
        /// CMSs the name of the core CMS card get pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="unblockPinList">The unblock pin list.</param>
        /// <param name="pinName">Name of the pin.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetPinName(IntPtr cardHandle, ulong role, ulong unblockPinList, out string pinName, out ulong errorCode)
        {
            pinName = string.Empty;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_getPinName(cardHandle, (NativeUnsignedInteger)role, (NativeUnsignedInteger)unblockPinList, out var pinNameNative, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            pinName = Marshal.PtrToStringUni(pinNameNative);
            return true;
        }

        /// <summary>
        /// CMSs the core CMS card get pin information.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="pinInfo">The pin information.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetPinInfo(IntPtr cardHandle, ulong role, out PinInfo pinInfo, out ulong errorCode)
        {
            errorCode = 0;
            pinInfo = new PinInfo();
            var currentPinInfo = new PIN_INFO();
            currentPinInfo.dwVersion = 6;
            var structMarshalled = Marshal.AllocHGlobal(Marshal.SizeOf(currentPinInfo));
            Marshal.StructureToPtr(currentPinInfo, structMarshalled, false);
            if (!CmsCoreNativeImports.CmsCore_CmsCard_readPinInfo(cardHandle, (NativeUnsignedInteger)role, ref currentPinInfo, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            //currentPinInfo = Marshal.PtrToStructure<PIN_INFO>(structMarshalled);
            FillUpOutPinInfo(currentPinInfo, pinInfo);
            return true;
        }

        /// <summary>
        /// Fills up out pin information.
        /// </summary>
        /// <param name="currentPinInfo">The current pin information.</param>
        /// <param name="pinInfo">The pin information.</param>
        private void FillUpOutPinInfo(PIN_INFO currentPinInfo, PinInfo pinInfo)
        {
            pinInfo.ChangePermission = (uint)currentPinInfo.dwChangePermission;
            pinInfo.Flags = (uint)currentPinInfo.dwFlags;
            pinInfo.UnblockPermission = (uint)currentPinInfo.dwUnblockPermission;
            pinInfo.Version = (uint)currentPinInfo.dwVersion;
            pinInfo.PinCachePolicy = new PinCachePolicy(currentPinInfo.PinCachePolicy);
        }

        /// <summary>
        /// CMSs the core CMS card get unblock pinlist.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="pinList">The pin list.</param>
        /// <param name="unblockPinList">The unblock pin list.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetUnblockPinlist(IntPtr cardHandle, ulong pinList, out ulong unblockPinList, out ulong errorCode)
        {
            var result = CmsCoreNativeImports.CmsCore_CmsCard_getUnblockPinList(cardHandle, (NativeUnsignedInteger)pinList, out var nativeUnblockPinList, out var nativeError);
            unblockPinList = (ulong)Marshal.ReadInt64(nativeUnblockPinList);
            errorCode = nativeError;
            return result;
        }

        /// <summary>
        /// CMSs the core CMS card pin policy1.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="policyBlob">The policy BLOB.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_PinPolicy1(IntPtr cardHandle, ulong role, out byte[] policyBlob, out ulong errorCode)
        {
            policyBlob = null;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_readPinPolicy1(cardHandle, (NativeUnsignedInteger)role, out var data, out var dataSizeAsPointer, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            //var size = (uint)dataSizeAsPointer;
            //policyBlob = new byte[size];
            //for (int i = 0; i < size; i++)
            //{
            //    policyBlob[i] = Marshal.ReadByte(data, i);
            //}
            var size = (uint)dataSizeAsPointer;
            policyBlob = new byte[size];
            Marshal.Copy(data, policyBlob, 0, (int)size);
            return true;
        }


        /// <summary>
        /// CMSs the core CMS card pin policy2.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="readTriesCounter">if set to <c>true</c> [read tries counter].</param>
        /// <param name="policyBlob">The policy BLOB.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_PinPolicy2(IntPtr cardHandle, ulong role, bool readTriesCounter, out byte[] policyBlob, out ulong errorCode)
        {
            policyBlob = null;
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_readPinPolicy2(cardHandle, (NativeUnsignedInteger)role, readTriesCounter, out var data, out var dataSize, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            //var size = (uint)dataSize;
            //policyBlob = new byte[size];
            //for (int i = 0; i < size; i++)
            //{
            //    policyBlob[i] = Marshal.ReadByte(data, i);
            //}
            var size = (uint)dataSize;
            policyBlob = new byte[size];
            Marshal.Copy(data, policyBlob, 0, (int)size);
            return true;
        }

        /// <summary>
        /// CMSs the size of the core CMS card get key.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="keySpecifications">The key specifications.</param>
        /// <param name="cardKeySize">Size of the card key.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetKeySize(IntPtr cardHandle, ulong keySpecifications, out CMSCORE_CARD_KEY_SIZES cardKeySize, out ulong errorCode)
        {
            cardKeySize = new CMSCORE_CARD_KEY_SIZES();
            errorCode = 0;
            IntPtr structMarshalled = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CMSCORE_CARD_KEY_SIZES)));
            if (!CmsCoreNativeImports.CmsCore_CmsCard_getKeySizes(cardHandle, (NativeUnsignedInteger)keySpecifications, structMarshalled, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            cardKeySize = Marshal.PtrToStructure<CMSCORE_CARD_KEY_SIZES>(structMarshalled);
            return true;
        }


        /// <summary>
        /// CMSs the core CMS card get challenge.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cryptogram">The cryptogram.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_GetChallenge(IntPtr cardHandle, out byte[] cryptogram, out ulong errorCode)
        {
            cryptogram = null;
            errorCode = 0;
            IntPtr structMarshalled = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CMSCORE_CARD_KEY_SIZES)));
            if (!CmsCoreNativeImports.CmsCore_CmsCard_getChallenge(cardHandle, out var crypto, out var sizeAsPointer, AllocCallback, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            int size = (int)sizeAsPointer;
            cryptogram = new byte[size];
            Marshal.Copy(crypto, cryptogram, 0, size);
            return true;
        }

        //PIN / PUK
        //Verify

        /// <summary>
        /// CMSs the core CMS card login role.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="rolePin">The role pin.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_LoginRole(IntPtr cardHandle, uint role, string rolePin, out NativeUnsignedInteger errorCode)
        {
            errorCode = 0;
            IntPtr pin = Marshal.StringToHGlobalUni(rolePin);
            var result = CmsCoreNativeImports.CmsCore_CmsCard_loginRole(cardHandle, (NativeUnsignedInteger)role, pin, out var nativeError);
            if (!result)
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }

        /// <summary>
        /// CMSs the core CMS card role authenticated.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="authenticated">if set to <c>true</c> [authenticated].</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_RoleAuthenticated(IntPtr cardHandle, ulong role, out bool authenticated, out ulong errorCode)
        {
            errorCode = 0;
            authenticated = false;
            IntPtr structMarshalled = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CMSCORE_CARD_KEY_SIZES)));
            if (!CmsCoreNativeImports.CmsCore_CmsCard_roleAuthenticated(cardHandle, (NativeUnsignedInteger)role, out var authenticatedAsPointer, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            authenticated = Convert.ToBoolean((int)authenticatedAsPointer);
            return true;
        }

        /// <summary>
        /// CMSs the core CMS card unblock user pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cryptogram">The cryptogram.</param>
        /// <param name="newPin">The new pin.</param>
        /// <param name="triesLeft">The tries left.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_UnblockUserPin(IntPtr cardHandle, byte[] cryptogram, string newPin, int triesLeft, out ulong errorCode)
        {
            errorCode = 0;
            IntPtr nativeCryptogram = Marshal.AllocHGlobal(cryptogram.Length);
            Marshal.Copy(cryptogram, 0, nativeCryptogram, cryptogram.Length);
            NativeUnsignedInteger cryptogramSize = (NativeUnsignedInteger)cryptogram.Length;
            IntPtr nativeNewPin = Marshal.StringToHGlobalUni(newPin);
            if (!CmsCoreNativeImports.CmsCore_CmsCard_unblockUserPin(cardHandle, nativeCryptogram, cryptogramSize, nativeNewPin, triesLeft, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }


        /// <summary>
        /// CMSs the core CMS card login admin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cryptogram">The cryptogram.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_LoginAdmin(IntPtr cardHandle, byte[] cryptogram, out ulong errorCode)
        {

            errorCode = 0;
            IntPtr crypto = Marshal.AllocHGlobal(cryptogram.Length);
            Marshal.Copy(cryptogram, 0, crypto, cryptogram.Length);
            NativeUnsignedInteger cryptoSize = (NativeUnsignedInteger)cryptogram.Length;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_loginAdmin(cardHandle, crypto, cryptoSize, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }

            return true;
        }
        //Change

        //	0000	        
        /// <summary>
        /// CMSs the core CMS card change role pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="oldPin">The old pin.</param>
        /// <param name="newPin">The new pin.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_ChangeRolePin(IntPtr cardHandle, ulong role, string oldPin, string newPin, out ulong errorCode)
        {
            errorCode = 0;
            IntPtr nativeOldPin = Marshal.StringToHGlobalUni(oldPin);
            IntPtr nativeNewPin = Marshal.StringToHGlobalUni(newPin);
            if (!CmsCoreNativeImports.CmsCore_CmsCard_changeRolePin(cardHandle, (NativeUnsignedInteger)role, nativeOldPin, nativeNewPin, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }

        /// <summary>
        /// CMSs the core CMS card set admin key.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cryptogram">The cryptogram.</param>
        /// <param name="newPin">The new pin.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_SetAdminKey(IntPtr cardHandle, byte[] cryptogram, string newPin, out ulong errorCode)
        {
            errorCode = 0;
            IntPtr crypto = Marshal.AllocHGlobal(cryptogram.Length);
            Marshal.Copy(cryptogram, 0, crypto, cryptogram.Length);
            NativeUnsignedInteger cryptoSize = (NativeUnsignedInteger)cryptogram.Length;
            IntPtr nativeNewPin = Marshal.StringToHGlobalUni(newPin);
            NativeUnsignedInteger pinSize = (NativeUnsignedInteger)newPin.Length;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_setAdminKey(cardHandle, crypto, cryptoSize, nativeNewPin, pinSize, 5, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }
        //Unblock
        //Note: Unblock using Challenge/response and unblock using PUC        
        /// <summary>
        /// CMSs the core CMS card unblock user pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="cryptogram">The cryptogram.</param>
        /// <param name="newPin">The new pin.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_UnblockUserPin(IntPtr cardHandle, byte[] cryptogram, string newPin, out ulong errorCode)
        {
            errorCode = 0;
            IntPtr crypto = Marshal.AllocHGlobal(cryptogram.Length);
            Marshal.Copy(cryptogram, 0, crypto, cryptogram.Length);
            NativeUnsignedInteger cryptoSize = (NativeUnsignedInteger)cryptogram.Length;
            IntPtr nativeNewPin = Marshal.StringToHGlobalUni(newPin);
            NativeUnsignedInteger pinSize = (NativeUnsignedInteger)newPin.Length;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_unblockUserPin(cardHandle, crypto, cryptoSize, nativeNewPin, 5, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }
        /// <summary>
        /// CMSs the core CMS card block role pin.
        /// </summary>
        /// <param name="cardHandle">The card handle.</param>
        /// <param name="role">The role.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns></returns>
        public bool CmsCore_CmsCard_BlockRolePin(IntPtr cardHandle, ulong role, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_blockRolePin(cardHandle, (NativeUnsignedInteger)role, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }


        public bool CmsCore_CmsCard_ContainersGet(IntPtr cardHandle, IntPtr cmsCoreProgress, out uint[] Ids, out ulong errorCode)
        {
            errorCode = 0;
            Ids = null;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersGet(cardHandle, out IntPtr nativeIds, out var sizeAsPointer, AllocCallback, cmsCoreProgress, out var nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            var size = Marshal.ReadInt32(sizeAsPointer);
            IntPtr[] pIntPtrArray = new IntPtr[size];
            Ids = new uint[size];
            Marshal.Copy(nativeIds, pIntPtrArray, 0, (int)size);
            errorCode = 0;
            for (int i = 0; i < size; i++)
            {
                Ids[i] = (uint)pIntPtrArray[i];
            }
            return true;
        }

        public bool CmsCore_CmsCard_ContainersGetLabel(IntPtr cardHandle, uint containerId, int whichLabel, out string containerLabel, out ulong errorCode)
        {
            errorCode = 0;
            containerLabel = string.Empty;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersGetLabel(cardHandle, containerId, whichLabel, out IntPtr pOutResStr, AllocCallback, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            containerLabel = Marshal.PtrToStringUni(pOutResStr);
            return true;
        }

        public bool CmsCore_CmsCard_ContainersGetCertificate(IntPtr cardHandle, uint containerId, out byte[] certificate, out ulong errorCode)
        {
            errorCode = 0;
            certificate = null;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersGetCert(cardHandle, containerId, out IntPtr pOutCert, out var pOutDataSize, AllocCallback, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            //   containerLabel = Marshal.PtrToStringUni(pOutResStr);
            var size = Marshal.ReadInt32(pOutDataSize);
            certificate = new byte[size];
            Marshal.Copy(pOutCert, certificate, 0, (int)size);
            return true;
        }

        public bool CmsCore_CmsCard_ContainersGetIsDefault(IntPtr cardHandle, uint containerId, out bool isDefault, out ulong errorCode)
        {
            errorCode = 0;
            isDefault = false;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersGetIsDefault(cardHandle, containerId, out var pOutIsDefault, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            isDefault = Convert.ToBoolean(Marshal.ReadInt32(pOutIsDefault));
            return true;
        }

        public bool CmsCore_CmsCard_ContainersShowCert(IntPtr cardHandle, uint containerId, bool enableImport, out ulong errorCode)
        {
            errorCode = 0;
            IntPtr hParentWnd = IntPtr.Zero;
            IntPtr pszTitle = IntPtr.Zero;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersShowCert(cardHandle, containerId, hParentWnd, pszTitle, enableImport, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }



        public bool CmsCore_CmsCard_ContainersSetIsDefault(IntPtr cardHandle, uint containerId, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersSetIsDefault(cardHandle, containerId, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }


        public bool CmsCore_CmsCard_ContainersDelete(IntPtr cardHandle, uint containerId, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersDelete(cardHandle, containerId, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }


        public bool CmsCore_CmsCard_ContainersDeleteAll(IntPtr cardHandle, out ulong errorCode)
        {
            errorCode = 0;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersDeleteAll(cardHandle, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }

        public bool CmsCore_CmsCard_ContainersImportCertificate(IntPtr cardHandle, CertificateImport certificateImport, out ulong errorCode)
        {
            // internal static extern bool CmsCore_CmsCard_ContainersImportCert(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pszRolePin, IntPtr pszFilename, IntPtr pszPin, NativeUnsignedInteger contId, int iKeySpec, out IntPtr pOutErrStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
            errorCode = 0;
            NativeUnsignedInteger bRole = certificateImport.UserRole;
            IntPtr pszRolePin = Marshal.StringToHGlobalUni(certificateImport.RolePin);
            IntPtr pszFilename = Marshal.StringToHGlobalUni(certificateImport.CertificateFilename);
            IntPtr pszPin = Marshal.StringToHGlobalUni(certificateImport.CertificatePin);
            NativeUnsignedInteger contId = certificateImport.ContainerId;
            NativeInteger iKeySpec = certificateImport.KeySpec;
            if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersImportCert(cardHandle, bRole, pszRolePin, pszFilename, pszPin, contId, iKeySpec, out var pOutErrStr, AllocCallback, out NativeUnsignedInteger nativeError))
            {
                errorCode = nativeError;
                return false;
            }
            return true;
        }

        public bool CmsCore_CmsCard_ContainersImportCerts(IntPtr cardHandle, out ulong errorCode)
        {
            // internal static extern bool CmsCore_CmsCard_ContainersImportCerts(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pszRolePin, IntPtr pbCertData, NativeUnsignedInteger dwCertDataSize, ref NativeUnsignedInteger dwOutAddedCnt, out NativeUnsignedInteger pRetErr);
            errorCode = 0;
            //if (!CmsCoreNativeImports.CmsCore_CmsCard_ContainersImportCerts(cardHandle, out NativeUnsignedInteger nativeError))
            //{
            //    errorCode = nativeError;
            //    return false;
            //}
            return true;
        }



    }


}
