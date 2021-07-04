/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
#endif


namespace VSec.DotNet.CmsCore.Wrapper.Natives.Delegates
{
   #if !_WIN32
        using NativeInteger = System.Int64;
        using NativeUnsignedInteger = System.UInt64;
        #else
        using NativeInteger = System.Int32;
        using NativeUnsignedInteger = System.UInt32;
        #endif

    using System;
    using System.Runtime.InteropServices;

    public static class ReaderListDelegates
    {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeInteger PtrToMngInvokeGetCurSel();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeUnsignedInteger PtrToMngInvokeGetCount();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr PtrToMngInvokeGet(NativeUnsignedInteger idx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeAdd(IntPtr pReaderName);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeDel(NativeUnsignedInteger idx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeInteger PtrToMngInvokeFind(IntPtr pReaderName);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeSetCurSel(NativeInteger idx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeResetContent();
    }

}

