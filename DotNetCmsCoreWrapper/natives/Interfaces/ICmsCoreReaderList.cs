﻿/*
*  auto generate dll import file 
interfaces from c++ header file
*
*/
namespace VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces
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
        using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;
        using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
        using System.Text;
        [StructLayout(LayoutKind.Sequential)]            
        public abstract class ICmsCoreReaderList {

 public abstract NativeUnsignedInteger getCnt();
 public abstract IntPtr get(NativeUnsignedInteger idx);
 public abstract void add(IntPtr pReaderName);
 public abstract void del(NativeUnsignedInteger idx);
 public abstract NativeInteger find(IntPtr pReaderName);
 public abstract void ResetContent();
 public abstract NativeInteger GetCurSel();
 public abstract void SetCurSel(NativeInteger idx);

 bool bLastReaderAvail;
 bool bReaderSelected;
 NativeInteger lastReaderCnt;
 NativeUnsignedInteger dwLastUpdate;
 NativeUnsignedInteger dwLastReaderWrite;
 bool bRdUpdateRequired;
 string szForceSelectedReader;
}

}


