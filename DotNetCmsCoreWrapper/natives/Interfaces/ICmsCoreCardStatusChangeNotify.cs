/*
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
        public abstract class ICmsCoreCardStatusChangeNotify {

 public abstract void OnCardInsert(IntPtr  pPcsc);
 public abstract void OnCardRemove(IntPtr  pPcsc);
}

}


