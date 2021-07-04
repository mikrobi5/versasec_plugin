/*
 */
#if !_WIN32
using NativeInteger = System.Int64;
using NativeUnsignedInteger = System.UInt64;
#else
#endif


namespace VSec.DotNet.CmsCore.Wrapper.Natives.Delegates
{
    using System.Runtime.InteropServices;
    using static VSec.DotNet.CmsCore.Wrapper.Natives.Delegates.CmsCoreProgressDelegates;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class CmsCoreProgressFunctionDelegates
    {
        public PtrToMngInvokeDestructorCmsCoreProgress DestructorCmsCoreProgress;
        public PtrToMngInvokeShow Show;
        public PtrToMngInvokeSetRange SetRange;
        public PtrToMngInvokeSetPos SetPos;
        public PtrToMngInvokeSetStep SetStep;
        public PtrToMngInvokeStepIt StepIt;
        public PtrToMngInvokeOnStart OnStart;
        public PtrToMngInvokeOnEnd OnEnd;
        public PtrToMngInvokeSetMsg SetMsg;
        public PtrToMngInvokeProgress Progress;
        public PtrToMngInvokeSetRemainingTime SetRemainingTime;
        public PtrToMngInvokeWaitCursor WaitCursor;
        public PtrToMngInvokeStatusTakeSnapshot StatusTakeSnapshot;
        public PtrToMngInvokeStatusRevertToSnapshot StatusRevertToSnapshot;
    };

}

