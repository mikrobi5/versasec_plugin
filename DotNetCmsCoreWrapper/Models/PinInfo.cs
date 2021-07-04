using VSec.DotNet.CmsCore.Wrapper.Natives;
using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    public class PinInfo
    {
        public uint Version { get; set; }
        public SECRET_TYPE PinType { get; set; }
        public SECRET_PURPOSE PinPurpose { get; set; }
        public uint ChangePermission { get; set; }
        public uint UnblockPermission { get; set; }
        public PinCachePolicy PinCachePolicy { get; set; }
        public uint Flags { get; set; }
    };
}
