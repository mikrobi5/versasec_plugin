using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    public class CCmsCoreReaderList : ICmsCoreReaderList
    {
        //   PointerToManagedFunctionToInvoke GetCurSel;
        private List<string> _readerCollection = new List<string>();
        private NativeInteger _currentSelectedReader = -1;

        public CCmsCoreReaderList()
        {

        }
        public override void add(IntPtr pReaderName)
        {
            Trace.WriteLine("reader added");
            Log.Logger.Debug($"reader name handle {pReaderName}");
            _readerCollection.Add(Marshal.PtrToStringUni(pReaderName));
        }

        public override void del([MarshalAs(UnmanagedType.SysUInt)] NativeUnsignedInteger idx)
        {
            _readerCollection.RemoveAt((int)idx);
        }

        public void DeleteReader(NativeUnsignedInteger index)
        {
            _readerCollection.RemoveAt((int)index);
        }

        public override NativeInteger find(IntPtr pReaderName)
        {
            return _readerCollection.IndexOf(Marshal.PtrToStringUni(pReaderName));
        }

        public override IntPtr get([MarshalAs(UnmanagedType.SysUInt)] NativeUnsignedInteger idx)
        {
            if (_readerCollection.Count > (int)idx)
            {
                return Marshal.StringToHGlobalUni(_readerCollection[(int)idx]);
            }
            return Marshal.StringToHGlobalUni("no reader found");
        }

        public override NativeUnsignedInteger getCnt()
        {
            return (NativeUnsignedInteger)_readerCollection.Count;

        }
        
        public override NativeInteger GetCurSel()
        {
            return _currentSelectedReader;
        }

        public string GetReader(NativeUnsignedInteger index)
        {
            if (_readerCollection.Count > (int)index)
            {
                return _readerCollection[(int)index];
            }
            return "no reader found";
        }

        public NativeUnsignedInteger GetReaderCount()
        {
            return (NativeUnsignedInteger)_readerCollection.Count;
        }

        public NativeUnsignedInteger GetReaderIndex(string readerName)
        {
            return (NativeUnsignedInteger)_readerCollection.IndexOf(readerName);
        }

        public override void ResetContent()
        {
            _currentSelectedReader = -1;
            _readerCollection = new List<string>();
        }

        public override void SetCurSel([MarshalAs(UnmanagedType.SysInt)] NativeInteger idx)
        {
            _currentSelectedReader = idx;
        }

        internal bool FindReader(string readersName)
        {
            return _readerCollection.IndexOf(readersName) != -1;
        }
    }
}
