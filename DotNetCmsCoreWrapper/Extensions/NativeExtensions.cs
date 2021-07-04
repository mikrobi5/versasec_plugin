using VSec.DotNet.CmsCore.Wrapper.Natives.Delegates;
using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;

namespace VSec.DotNet.CmsCore.Wrapper.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class NativeExtensions
    {
        /// <summary>
        /// Gets the function delegates.
        /// </summary>
        /// <param name="cmsCoreReaderList">The CMS core reader list.</param>
        /// <returns></returns>
        public static ReaderListFunctionDelegates GetFunctionDelegates(this ICmsCoreReaderList cmsCoreReaderList)
        {
            ReaderListFunctionDelegates functionDelegates = new ReaderListFunctionDelegates
            {
                Add = cmsCoreReaderList.add,
                Del = cmsCoreReaderList.del,
                Find = cmsCoreReaderList.find,
                Get = cmsCoreReaderList.get,
                GetCount = cmsCoreReaderList.getCnt,
                GetCurrentSelected = cmsCoreReaderList.GetCurSel,
                ResetContent = cmsCoreReaderList.ResetContent,
                SetCurrentSelected = cmsCoreReaderList.SetCurSel
            };
            return functionDelegates;
        }

        /// <summary>
        /// Gets the function delegates.
        /// </summary>
        /// <param name="cmsCoreProgress">The CMS core progress.</param>
        /// <returns></returns>
        public static CmsCoreProgressFunctionDelegates GetFunctionDelegates(this ICmsCoreProgress cmsCoreProgress)
        {
            CmsCoreProgressFunctionDelegates functionDelegates = new CmsCoreProgressFunctionDelegates
            {
                //DestructorCmsCoreProgress = cmsCoreProgress.
                OnEnd = cmsCoreProgress.OnEnd,
                OnStart = cmsCoreProgress.OnStart,
                Progress = cmsCoreProgress.Progress,
                SetMsg = cmsCoreProgress.SetMsg,
                SetPos = cmsCoreProgress.SetPos,
                SetRange = cmsCoreProgress.SetRange,
                SetRemainingTime = cmsCoreProgress.SetRemainingTime,
                SetStep = cmsCoreProgress.SetStep,
                Show = cmsCoreProgress.Show,
                StatusRevertToSnapshot = cmsCoreProgress.StatusRevertToSnapshot,
                StatusTakeSnapshot= cmsCoreProgress.StatusTakeSnapshot,
                StepIt = cmsCoreProgress.StepIt,
                WaitCursor = cmsCoreProgress.WaitCursor

            };
            return functionDelegates;
        }

        /// <summary>
        /// Gets the function delegates.
        /// </summary>
        /// <param name="cmsCardStatusNotify">The CMS card status notify.</param>
        /// <returns></returns>
        public static CardStatusFunctionDelegates GetFunctionDelegates(this ICmsCoreCardStatusChangeNotify cmsCardStatusNotify)
        {
            CardStatusFunctionDelegates functionDelegates = new CardStatusFunctionDelegates
            {
                OnCardInsert = cmsCardStatusNotify.OnCardInsert,
                OnCardRemove = cmsCardStatusNotify.OnCardRemove,
            };
            return functionDelegates;
        }
    }
}
