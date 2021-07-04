using System;
using System.Collections.Generic;
using System.Text;
using VSec.DotNet.CmsCore.Wrapper.Edge;
using VSec.DotNet.CmsCore.Wrapper.Enums;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    public class SmartCard : ISmartCard
    {
        public string Identifier { get; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string ReaderName { get; set; }
        public string Csn { get; set; }
        public string CardId { get; set; }
        public ulong RoleTries { get; set; }
        public ulong AdminTries { get; set; }
        public ulong PinPolicyOne { get; set; }
        public ulong PinPolicyTow { get; set; }
        public string PinName { get; set; }
        public bool IsAvailable { get; set; }
        public bool Identified { get; set; }
        public ulong AvailableBytes { get; set; }
        public IntPtr Handle { get; private set; }
        public IntPtr PcScHandle { get; private set; }
        public CardSpaces FreeSpace { get; set; }
        public CardKeySizes KeySizes { get; set; }
        public PinInfo PinInfo { get; set; }
        public byte[] PolicyBytesOne { get; set; }
        public byte[] PolicyBytesTwo { get; set; }
        public string Pin { get; set; }
        public string NewPin { get; set; }
        public Roles UserRole { get; set; }

        public SmartCard(IntPtr handle, IntPtr pcscHandle)
        {
            Handle = handle;
            PcScHandle = pcscHandle;
        }

        public bool ChangeUserPin()
        {
            var result = false;
            result = CmsCoreCaller._Instance.ChangeRolePin(Handle, (uint)UserRole, Pin, NewPin);
            return result;
        }

        public bool UnblockUserPin()
        {
            var result = false;
            result = CmsCoreCaller._Instance.UnblockUserPin(Handle, Pin, 5);
            return result;
        }

        public bool LoginUser()
        {
            var result = false;
            result = CmsCoreCaller._Instance.LoginRole(Handle, (uint)UserRole, Pin);
            if(!result)
            {
                CmsCoreCaller._Instance.GetCardRoleTries(Handle, (uint)UserRole, out var rolesTries);
                RoleTries = rolesTries;
            }
            return result;
        }
    }
}
