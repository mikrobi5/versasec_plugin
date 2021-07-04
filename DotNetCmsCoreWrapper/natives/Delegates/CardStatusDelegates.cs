/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
#endif


namespace VSec.DotNet.CmsCore.Wrapper.Natives.Delegates
{
    using System;
    using System.Runtime.InteropServices;
    public static class CardStatusDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngOnCardInsert(IntPtr cardHandle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngOnCardRemove(IntPtr cardHandle);
    }

}

