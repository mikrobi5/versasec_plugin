/*
*  auto generate dll import file 
* 
*/
namespace VSec.DotNet.CmsCore.Wrapper.Natives
{
#if !_WIN32
        using NativeInteger = System.Int64;
        using NativeUnsignedInteger = System.UInt64;
#else
    using NativeInteger = System.Int32;
    using NativeUnsignedInteger = System.UInt32;
#endif
    using System;
    using System.Collections.Generic;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Reflection;
    using System.IO;
        using System.Runtime.ExceptionServices;

        using global::Serilog;

    sealed internal class LoadCmsCoreDll : AssemblyLoadContext
    {
        [DllImport("libdl.dylib")]
        static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl.dylib")]
        static extern IntPtr dlerror();

        [DllImport("libdl")]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        static internal String szLoadedPath = null;

        static internal String szLoadedLib = null;

        public LoadCmsCoreDll()
        {
            // System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += Default_Resolving;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {

            GetsystemDependingLibraryName(ref unmanagedDllName);
            GetsystemDependingLibraryFile(ref unmanagedDllName);

            if (!File.Exists(unmanagedDllName))
            {
                return IntPtr.Zero;
            }
            Console.WriteLine("Loading the Native dll from {0}\n ", unmanagedDllName);
            Log.Logger.Information($"Loading the Native dll from {unmanagedDllName}\n ");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !RuntimeInformation.FrameworkDescription.Contains("Mono"))
            {
                Log.Logger.Information($"Loading Native dll normal way ");
                return LoadUnmanagedDllFromPath(unmanagedDllName);
            }
            Log.Logger.Information($"Loading Native dll posix way ");
            return LoadPosixLibrary(unmanagedDllName);
        }

        private void GetsystemDependingLibraryFile(ref string unmanagedDllName)
        {
            var systemFolders = DetermineSystemPath();

            foreach (var folders in systemFolders)
            {
                var tempFile = Path.Combine(folders, unmanagedDllName);
                Log.Logger.Information($"{Assembly.GetExecutingAssembly().Location}");
                Log.Logger.Information($"{Directory.GetCurrentDirectory()}");
                
                if (File.Exists(tempFile))
                {
                    var fi = new FileInfo(tempFile);
                    unmanagedDllName = fi.FullName;
                    break;
                }
                else
                {
                    Log.Logger.Error($"file {tempFile} not found");
                }

            }
        }

        private string[] DetermineSystemPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[] { "." };
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new[] { ".", "/usr/local/lib", "/usr/local/lib64", "/usr/lib", "/usr/lib64" };
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[] { ".", "~/lib", "/usr/local/lib", "/usr/lib" };
            }
            return null;
        }

        private void GetsystemDependingLibraryName(ref string unmanagedDllName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                unmanagedDllName = $"{unmanagedDllName}.dll";
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                unmanagedDllName = $"lib{unmanagedDllName}.so";
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                unmanagedDllName = $"lib{unmanagedDllName}.dylib";
                return;
            }
        }

        internal bool Init(string dllName)
        {

            var nativeDllPointer = LoadUnmanagedDll(dllName);

            if (nativeDllPointer != IntPtr.Zero)
            {
                Console.WriteLine("Library handle written value is a non zero {0}\n", nativeDllPointer);
                Log.Logger.Information($"Library handle written value is a non zero {nativeDllPointer}");
                return true;
            }
            Log.Logger.Error($"Library handle load not successful {dllName}");
            return false;
        }

        private Assembly Default_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            return null;
        }

        static IntPtr LoadPosixLibrary(string unmanagedDllName)
        {
            const int RTLD_NOW = 2;

            if (File.Exists(unmanagedDllName))
            {
                var addr = dlopen(unmanagedDllName, RTLD_NOW);
                if (addr == IntPtr.Zero)
                {
                    var error = Marshal.PtrToStringAnsi(dlerror());
                    throw new Exception($"dlopen failed: {unmanagedDllName} {error}");
                }
                return addr;
            }

            throw new Exception($"dlopen failed: unable to locate library {unmanagedDllName}");
        }


    }

    /// <summary>
    /// 
    /// </summar
    
    public static class CmsCoreNativeImports
    {
        const string _cmsCoreLib = "cmsCoreDll";

        /// <summary>
        /// Initializes the <see cref="CmsCoreNativeImports"/> class.
        /// </summary>
        static CmsCoreNativeImports()
        {
            var test = new LoadCmsCoreDll();
            if (!test.Init(_cmsCoreLib))
            {
                throw new NullReferenceException("VSec native DLL not found - or cannot be loaded");
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CmsCallback(int size);

        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_Initialize(out NativeUnsignedInteger pOutInst, out IntPtr pOutErrStr, CmsCallback p);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_Finalize(NativeUnsignedInteger hInst);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_GetErrorDetails(NativeUnsignedInteger err, out IntPtr pOutErrStr, CmsCallback p);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_GetConfigParam(IntPtr pszCfgFile, IntPtr pszKey, out IntPtr pOutValue, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_GetLicenseParam(IntPtr pszLicFile, IntPtr pszPubKey, IntPtr pszKey, out IntPtr pOutValue, CmsCallback p, out NativeUnsignedInteger pRetErr);

        //CmsCore_CardList_Get(HCMSCOREINST hInst, ICmsCoreCardList* pOuthCardList, CMSCORE_ERR* pRetErr, fkt_alloc p);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_Get(NativeUnsignedInteger hInst, out IntPtr pOuthCardList, out IntPtr pOutErrStr, CmsCallback p);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_UpdateReaderList(IntPtr hCardList, IntPtr pReaderList, bool bCardPrinterOnly, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_GetAvailableCardsCnt(IntPtr hCardList, IntPtr pszReaderToSkip, out IntPtr outCnt, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_GetCardPcsc(IntPtr hCardList, IntPtr pszReaderName, out IntPtr pOutPcsc, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_clearCaches(IntPtr hCardList, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_GetCardReaderName(IntPtr hCardList, NativeUnsignedInteger Idx, IntPtr pszReaderToSkip, out IntPtr pOutReaderName, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_isCardAvailable(IntPtr hCardList, IntPtr pszReaderName, out IntPtr pOutAvail, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_isValid(IntPtr hCardList, IntPtr hPcsc, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_AddCardStatusNotifier(IntPtr hCardList, IntPtr hStatusNotifier, out IntPtr pRetErr, CmsCallback p);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardList_DelCardStatusNotifier(IntPtr hCardList, IntPtr p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CardSupported(NativeUnsignedInteger hInst, IntPtr atr, NativeUnsignedInteger atrSize, out IntPtr pOutCardType, out IntPtr pOutCardName, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_Pcsc_GetAtr(IntPtr hPcsc, out IntPtr pOutData, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_Create_CmsCard(IntPtr hPcsc, out IntPtr pOuthCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_Delete_CmsCard(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_identifyCard(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_lockCard(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_unlockCard(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_emptyCache(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_get_CSN_CARDID(IntPtr hCard, out IntPtr pOutCSN, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_get_CSN_CSN(IntPtr hCard, out IntPtr pOutCSN, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_getChallenge(IntPtr hCard, out IntPtr pOutCryptogram, out IntPtr pOutCryptogramSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_unblockUserPin(IntPtr hCard, IntPtr cryptogram, NativeUnsignedInteger cryptogramSize, IntPtr newPin, NativeInteger iTriesLeft, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_getRoleTriesLeft(IntPtr hCard, NativeUnsignedInteger bRole, out IntPtr pOutCnt, out IntPtr pRetErr, CmsCallback p);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_getAdminTriesLeft(IntPtr hCard, out IntPtr pOutCnt, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_changeRolePin(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr oldPin, IntPtr newPin, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_loginRole(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pin, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_roleAuthenticated(IntPtr hCard, NativeUnsignedInteger bRole, out IntPtr pOutAuthenticated, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_loginAdmin(IntPtr hCard, IntPtr cryptogram, NativeUnsignedInteger cryptogramSize, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_setAdminKey(IntPtr hCard, IntPtr cryptogram, NativeUnsignedInteger cryptogramSize, IntPtr newKey, NativeUnsignedInteger newKeySize, NativeInteger iNewTries, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_blockRolePin(IntPtr hCard, NativeUnsignedInteger bRole, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CmsCore_CmsCard_correctFileName(IntPtr hCard, IntPtr pszDirName, IntPtr pszFileName, out IntPtr pOutName, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_readFile(IntPtr hCard, IntPtr pszDirName, IntPtr pszFileName, out IntPtr pOutData, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_writeFile(IntPtr hCard, IntPtr pszDirName, IntPtr pszFileName, IntPtr pInData, NativeUnsignedInteger inDataSize, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_createFile(IntPtr hCard, IntPtr pszDirName, IntPtr pszFileName, NativeInteger dwInitialSize, CARD_FILE_ACCESS_CONDITION ac, bool bFailIfExists, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_deleteFile(IntPtr hCard, IntPtr pszDirName, IntPtr pszFileName, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_createDirectory(IntPtr hCard, IntPtr pszDirName, bool bAdminOnly, bool bFailIfExists, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_deleteDirectory(IntPtr hCard, IntPtr pszDirName, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_listFiles(IntPtr hCard, IntPtr pszDirName, out IntPtr pOutStr, out IntPtr dwOutCnt, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_createContainer(IntPtr hCard, byte bIdx, bool bKeyImport, NativeInteger keySpec, NativeInteger dwKeySize, IntPtr pInKey, NativeUnsignedInteger inKeySize, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_deleteContainer(IntPtr hCard, byte bIdx, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_rsaDecrypt(IntPtr hCard, byte bIdx, NativeInteger iKeySpec, IntPtr pInData, NativeUnsignedInteger inDataSize, out IntPtr pOutData, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_readPinPolicy1(IntPtr hCard, NativeUnsignedInteger bRole, out IntPtr pOutData, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_readPinPolicy2(IntPtr hCard, NativeUnsignedInteger bRole, bool bReadTriesCounter, out IntPtr pOutData, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_writePinPolicy(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pData, NativeUnsignedInteger dwDataSize, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_readCardProperty(IntPtr hCard, NativeUnsignedInteger bRole, NativeUnsignedInteger dwProp, ref NativeUnsignedInteger dwOutVal, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_writeCardProperty(IntPtr hCard, NativeUnsignedInteger bRole, NativeUnsignedInteger dwProp, ref NativeUnsignedInteger dwVal, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_PinPolicyCheckPin(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pin, bool bConfirmed, out IntPtr pOutResStr, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_getPinName(IntPtr hCard, NativeUnsignedInteger bRole, NativeUnsignedInteger unblockPinList, out IntPtr pOutName, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CmsCore_CmsCard_getKeySizes(IntPtr hCard, NativeUnsignedInteger dwKeySpec, IntPtr pOutKeySizes, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_getFreeSpaces(IntPtr hCard, ref NativeUnsignedInteger dwBytesAvaiable, ref NativeUnsignedInteger dwKeyContainerAvailable, ref NativeUnsignedInteger dwMaxKeyContainers, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_getCardContainerPinList(IntPtr hCard, bool bAddPrimaryUserPin, out IntPtr pOut, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_getUnblockPinList(IntPtr hCard, NativeUnsignedInteger pinList, out IntPtr pOut, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_PinPolicyCheckPin(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pin, bool bConfirmed, bool bIncludeConfirm, out IntPtr pOutResult, out IntPtr pOutResStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_SSO_Set(IntPtr hCard, NativeUnsignedInteger bRole, bool bEnabled, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_SSO_Get(IntPtr hCard, NativeUnsignedInteger bRole, out IntPtr bOutEnabled, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersGet(IntPtr hCard, out IntPtr pOutIds, out IntPtr pOutSize, CmsCallback p, IntPtr pProgress, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersGetLabel(IntPtr hCard, NativeUnsignedInteger contId, NativeInteger iWhich, out IntPtr pOutResStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersGetCert(IntPtr hCard, NativeUnsignedInteger contId, out IntPtr pOutCert, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersGetIsDefault(IntPtr hCard, NativeUnsignedInteger contId, out IntPtr pOutIsDefault, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersShowCert(IntPtr hCard, NativeUnsignedInteger contId, IntPtr hParentWnd, IntPtr pszTitle, bool bEnableImport, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersSetIsDefault(IntPtr hCard, NativeUnsignedInteger contId, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersDelete(IntPtr hCard, NativeUnsignedInteger contId, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersDeleteAll(IntPtr hCard, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersImportCert(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pszRolePin, IntPtr pszFilename, IntPtr pszPin, NativeUnsignedInteger contId, NativeInteger iKeySpec, out IntPtr pOutErrStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_ContainersImportCerts(IntPtr hCard, NativeUnsignedInteger bRole, IntPtr pszRolePin, IntPtr pbCertData, NativeUnsignedInteger dwCertDataSize, ref NativeUnsignedInteger dwOutAddedCnt, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_readPinInfo(IntPtr hCard, NativeUnsignedInteger bRole, ref PIN_INFO pOutData, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CmsCard_writePinInfo(IntPtr hCard, NativeUnsignedInteger bRole, out IntPtr pOutData, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CheckForUpdates(IntPtr pszUrl, IntPtr pszXmlNode, ref bool bOutUpdateAvailable, out IntPtr pOutMsgStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_CheckForUpdates1(IntPtr pHttpSend, IntPtr pszUrl, IntPtr pszXmlNode, ref bool bOutUpdateAvailable, out IntPtr pOutMsgStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_GenLicenseUpgradeChallenge(byte bWhich, out IntPtr pOutMsgStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_ApplyLicenseUpgradeResponse(IntPtr pszResp, bool bInstall, ref bool bOk, out IntPtr pOutMsgStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_P12Parse(IntPtr pszP12File, IntPtr pszPwd, ref NativeUnsignedInteger dwOutInst, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_P12GetCnt(NativeUnsignedInteger dwInst, ref NativeUnsignedInteger dwOutCnt, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_P12GetLabel(NativeUnsignedInteger dwInst, NativeUnsignedInteger dwIdx, out IntPtr pOutMsgStr, CmsCallback p, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_P12MarkToDelete(NativeUnsignedInteger dwInst, NativeUnsignedInteger dwIdx, out NativeUnsignedInteger pRetErr);
        [DllImport(_cmsCoreLib)]
        public static extern bool CmsCore_P12GetCert(NativeUnsignedInteger dwInst, out IntPtr pOutCerts, out IntPtr pOutDataSize, CmsCallback p, out NativeUnsignedInteger pRetErr);

    }

}

