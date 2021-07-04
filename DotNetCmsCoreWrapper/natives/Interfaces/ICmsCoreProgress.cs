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
        public abstract class ICmsCoreProgress {

 public abstract void Show(NativeInteger iWhat);
 public abstract void SetRange(NativeInteger iStart, NativeInteger iEnd);
 public abstract void SetPos(NativeInteger i);
 public abstract NativeInteger  SetStep(NativeInteger i);
 public abstract void StepIt();
 public abstract void OnStart();
 public abstract void OnEnd();
 public abstract void SetMsg(IntPtr pMsg, NativeInteger idx=0);
 public abstract void Progress(IntPtr? pszMsg=null, NativeInteger idx=0);
    public abstract void SetRemainingTime(IntPtr pMsg) ;
 public abstract void WaitCursor(bool bOn) ;
 public virtual NativeUnsignedInteger StatusTakeSnapshot() { return 0; }
 public abstract void  StatusRevertToSnapshot(NativeUnsignedInteger dwID) ;
}

}


