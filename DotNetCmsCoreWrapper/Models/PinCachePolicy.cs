using VSec.DotNet.CmsCore.Wrapper.Natives;
using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    #if !_WIN32
        using NativeInteger = System.Int64;
        using NativeUnsignedInteger = System.UInt64;
    #else
            using NativeInteger = System.Int32;
            using NativeUnsignedInteger = System.UInt32;
    #endif

    public class PinCachePolicy
    {
        public PinCachePolicy(PIN_CACHE_POLICY pinCachePolicy)
        {
            Version = (uint)pinCachePolicy.dwVersion;
            PinCachePolicyType = pinCachePolicy.PinCachePolicyType;
            PinCachePolicyInfo = (uint)pinCachePolicy.dwPinCachePolicyInfo;
        }

        public uint Version { get; set; }
        public PIN_CACHE_POLICY_TYPE PinCachePolicyType { get; set; }
        public uint PinCachePolicyInfo { get; set; }
    }
}
