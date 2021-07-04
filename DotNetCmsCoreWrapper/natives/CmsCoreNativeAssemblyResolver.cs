/*
 */

namespace VSec.DotNet.CmsCore.Wrapper.Natives
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// to resolve platform independent library assambly 
    /// </summary>
    //public static class CmsCoreNativeAssemblyResolver
    //{
    //    const string _cmsCoreLib = "cmsCoreDll";
    //    const string _cmsCoreAdditionals = "CmsCoreAdditionals";
    //    private static IntPtr _localCmsCoreNativeLibrary = IntPtr.Zero;
    //    private static IntPtr _localCmsAddNativeLibrary = IntPtr.Zero;
    //    public static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    //    {
    //        //IntPtr libHandle = IntPtr.Zero;
    //        if (libraryName == _cmsCoreLib)
    //        {
    //            if (_localCmsCoreNativeLibrary == IntPtr.Zero)
    //            {
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //                {
    //                    NativeLibrary.TryLoad("cmsCoreDll.dll", out _localCmsCoreNativeLibrary);
    //                }
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //                {
    //                    NativeLibrary.TryLoad("cmsCore", out _localCmsCoreNativeLibrary);
    //                }
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //                {
    //                    NativeLibrary.TryLoad("cmsCore.dylib", out _localCmsCoreNativeLibrary);
    //                }
    //            }
    //            return _localCmsCoreNativeLibrary;
    //        }
    //        if (libraryName == _cmsCoreAdditionals)
    //        {
    //            if (_localCmsAddNativeLibrary == IntPtr.Zero)
    //            {
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //                {
    //                    NativeLibrary.TryLoad("CmsCoreAdditionals.dll", out _localCmsAddNativeLibrary);
    //                }
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //                {
    //                    NativeLibrary.TryLoad("CmsCoreAdditionals", out _localCmsAddNativeLibrary);
    //                }
    //                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //                {
    //                    NativeLibrary.TryLoad("CmsCoreAdditionals.dylib", out _localCmsAddNativeLibrary);
    //                }
    //            }
    //            return _localCmsAddNativeLibrary;
    //        }
    //        return IntPtr.Zero;
    //    }

    //}

}

