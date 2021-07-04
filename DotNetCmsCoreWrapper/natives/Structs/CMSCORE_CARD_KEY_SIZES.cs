/*
*  auto generate dll import file 
*  structure from c++ header file
*
*/
namespace VSec.DotNet.CmsCore.Wrapper.Natives.Structs
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
    [StructLayout(LayoutKind.Sequential)]
    public class CMSCORE_CARD_KEY_SIZES
    {
        public NativeUnsignedInteger dwVersion;
        public NativeUnsignedInteger dwMinimumBitlen;
        public NativeUnsignedInteger dwDefaultBitlen;
        public NativeUnsignedInteger dwMaximumBitlen;
        public NativeUnsignedInteger dwIncrementalBitlen;
    };


}
