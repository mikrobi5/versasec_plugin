using System;
using System.Collections.Generic;
using System.Diagnostics;
using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    #if !_WIN32
        using NativeInteger = System.Int64;
        using NativeUnsignedInteger = System.UInt64;
        #else
        using NativeInteger = System.Int32;
        using NativeUnsignedInteger = System.UInt32;
        #endif
    public class CCmsCoreProgress : ICmsCoreProgress
    {
        //   PointerToManagedFunctionToInvoke GetCurSel;
        private List<string> _readerCollection = new List<string>();
        private int _currentSelectedReader = -1;

        public CCmsCoreProgress()
        {

        }

        public override void OnEnd()
        {
            Trace.WriteLine("OnEnd");
        }

        public override void OnStart()
        {
            Trace.WriteLine("OnStart");
        }

        public override void Progress(IntPtr? pszMsg = null, NativeInteger idx = 0)
        {
            Trace.WriteLine("Progress");
        }

        public override void SetMsg(IntPtr pMsg, NativeInteger idx = 0)
        {
            Trace.WriteLine("SetMsg");
        }

        public override void SetPos(NativeInteger i)
        {
            Trace.WriteLine("SetPos");
        }

        public override void SetRange(NativeInteger iStart, NativeInteger iEnd)
        {
            Trace.WriteLine("SetRange");
        }

        public override void SetRemainingTime(IntPtr pMsg)
        {
            Trace.WriteLine("SetRemainingTime");
        }

        public override NativeInteger SetStep(NativeInteger i)
        {
            Trace.WriteLine("SetStep");
            return i;
        }

        public override void Show(NativeInteger iWhat)
        {
            Trace.WriteLine("Show");
        }

        public override void StatusRevertToSnapshot(NativeUnsignedInteger dwID)
        {
            Trace.WriteLine("StatusRevertToSnapshot");
        }

        public override void StepIt()
        {
            Trace.WriteLine("StepIt");
        }

        public override void WaitCursor(bool bOn)
        {
            Trace.WriteLine("WaitCursor");
        }
    }
}
