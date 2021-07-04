/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
using NativeInteger = System.Int32;
using NativeUnsignedInteger = System.UInt32;
#endif



namespace VSec.DotNet.CmsCore.Wrapper.Natives.Delegates
{
    using System;
    using System.Runtime.InteropServices;

    public static class CmsCoreProgressDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeDestructorCmsCoreProgress();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeShow(NativeInteger iWhat);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeSetRange(NativeInteger iStart, NativeInteger iEnd);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeSetPos(NativeInteger i);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeInteger PtrToMngInvokeSetStep(NativeInteger i);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeStepIt();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeOnStart();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeOnEnd();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeSetMsg(IntPtr pMsg, NativeInteger id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeProgress(IntPtr? pMsg = null, NativeInteger idx = 0);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeSetRemainingTime(IntPtr pMsg);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeWaitCursor(bool bOn);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeUnsignedInteger PtrToMngInvokeStatusTakeSnapshot();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PtrToMngInvokeStatusRevertToSnapshot(NativeUnsignedInteger wdId);
    }

}

