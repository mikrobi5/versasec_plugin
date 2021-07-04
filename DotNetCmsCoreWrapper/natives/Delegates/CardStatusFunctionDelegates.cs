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
    using static VSec.DotNet.CmsCore.Wrapper.Natives.Delegates.CardStatusDelegates;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class CardStatusFunctionDelegates
    {
        public PtrToMngOnCardInsert OnCardInsert;
        public PtrToMngOnCardRemove OnCardRemove;
    };

}

