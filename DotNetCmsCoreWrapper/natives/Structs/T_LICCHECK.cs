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
    public class T_LICCHECK
    {
        public IntPtr pInData;
        public NativeUnsignedInteger dwInDataSize;
        public IntPtr pOutData;
        public NativeUnsignedInteger dwOutDataSize;
    };


}
