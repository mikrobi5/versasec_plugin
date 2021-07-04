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
        using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;
        using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
        using System.Runtime.InteropServices;
        [StructLayout(LayoutKind.Sequential)]
        public class PIN_CACHE_POLICY
        {
            public NativeUnsignedInteger dwVersion;
            public PIN_CACHE_POLICY_TYPE PinCachePolicyType;
            public NativeUnsignedInteger dwPinCachePolicyInfo;
        };

    
}
