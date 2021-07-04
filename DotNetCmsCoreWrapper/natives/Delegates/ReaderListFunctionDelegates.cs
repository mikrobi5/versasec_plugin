/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
#endif


namespace VSec.DotNet.CmsCore.Wrapper.Natives.Delegates
{
    using System.Runtime.InteropServices;
    using static VSec.DotNet.CmsCore.Wrapper.Natives.Delegates.ReaderListDelegates;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class ReaderListFunctionDelegates
    {
        public PtrToMngInvokeGetCount GetCount;
        public PtrToMngInvokeGet Get;
        public PtrToMngInvokeAdd Add;
        public PtrToMngInvokeDel Del;
        public PtrToMngInvokeFind Find;
        public PtrToMngInvokeGetCurSel GetCurrentSelected;
        public PtrToMngInvokeSetCurSel SetCurrentSelected;
        public PtrToMngInvokeResetContent ResetContent;
    };

}

