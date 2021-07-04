/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
#endif

namespace VSec.DotNet.CmsCore.Wrapper.Natives
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using VSec.DotNet.CmsCore.Wrapper.Natives.Delegates;

    //sealed internal class LoadNativeAdditionals : AssemblyLoadContext
    //{
    //    static internal String szLoadedPath = null;
    //    static internal String szLoadedLib = null;
    //    protected override Assembly Load(AssemblyName assemblyName)
    //    {
    //        return null;
    //    }

    //    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    //    {

    //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //        {
    //            unmanagedDllName = $"{unmanagedDllName}.dll";
    //        }
    //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //        {
    //            unmanagedDllName = $"{unmanagedDllName}.so";
    //        }
    //        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //        {
    //            unmanagedDllName = $"{unmanagedDllName}.dylib";
    //        }
    //        //szLoadedPath = "/root/testClibld/bin/Debug/netcoreapp1.1/";
    //        szLoadedPath = "/root/testClibld/bin/Debug/netcoreapp1.1/lib/";

    //        szLoadedLib = Path.Combine(szLoadedPath, unmanagedDllName);

    //        if (File.Exists(unmanagedDllName))
    //        {

    //            Console.WriteLine("Loading the Native dll from {0}\n ", unmanagedDllName);
    //            return base.LoadUnmanagedDll(unmanagedDllName);
    //        }

    //        if (File.Exists(szLoadedLib))
    //        {

    //            Console.WriteLine("Loading the Native dll from {0}\n ", szLoadedLib);

    //        }
    //        return LoadUnmanagedDllFromPath(szLoadedLib);
    //    }

    //    internal void Init(string dllName)
    //    {

    //        IntPtr hClidrvr = LoadUnmanagedDll(dllName);

    //        if (hClidrvr != IntPtr.Zero)
    //            Console.WriteLine("LIbrary handle written value is a non zero {0}\n", hClidrvr);

    //    }
    //}

    /// <summary>
    /// imports native functionality to create objects and their delegate functions
    /// </summary>
    public static class NativeAdditionals
    {
        const string _cmsCoreAdditionals = "CmsCoreAdditionals";

        static NativeAdditionals()
        {
           var loader = new LoadCmsCoreDll();
           loader.Init(_cmsCoreAdditionals);
        }


        //private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        //{
        //    IntPtr libHandle = IntPtr.Zero;
        //    if (libraryName == _cmsCoreAdditionals)
        //    {
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //        {
        //            NativeLibrary.TryLoad("CmsCoreAdditionals.dll", out libHandle);
        //        }
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //        {
        //            NativeLibrary.TryLoad("CmsCoreAdditionals", out libHandle);
        //        }
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //        {
        //            NativeLibrary.TryLoad("CmsCoreAdditionals.dylib", out libHandle);
        //        }
        //    }
        //    return libHandle;
        //}

        /// <summary>
        /// Creates the reader list unmanaged delegate. creates native delegate object to get be called in c#
        /// </summary>
        /// <param name="inManagedCallback">The in managed callback.</param>
        /// <returns></returns>
        [DllImport(_cmsCoreAdditionals)]
        public static extern IntPtr CreateReaderListUnmanagedDelegate(ReaderListFunctionDelegates inManagedCallback);

        [DllImport(_cmsCoreAdditionals)]
        public static extern void DestroyReaderListUnmanagedDelegate(IntPtr inManagedCallback);

        [DllImport(_cmsCoreAdditionals)]
        public static extern IntPtr CreateCmsCoreProgressUnmanagedDelegate(CmsCoreProgressFunctionDelegates inManagedCallback);

        [DllImport(_cmsCoreAdditionals)]
        public static extern void DestroyCmsCoreProgressUnmanagedDelegate(IntPtr inManagedCallback);

        /// <summary>
        /// Creates the card status unmanaged delegate.
        /// </summary>
        /// <param name="inManagedCallback">The in managed callback.</param>
        /// <returns></returns>
        ///         libCmsCoreAdditionals.dylib
        [DllImport(_cmsCoreAdditionals)]
        public static extern IntPtr CreateCardStatusUnmanagedDelegate(CardStatusFunctionDelegates inManagedCallback);

        [DllImport(_cmsCoreAdditionals)]
        public static extern void DestroyCardStatusUnmanagedDelegate(IntPtr inManagedCallback);
    }

}

