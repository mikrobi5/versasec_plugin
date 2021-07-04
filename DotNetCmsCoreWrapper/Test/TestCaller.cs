using System;
using System.Collections.Generic;
using System.Text;
using VSec.DotNet.CmsCore.Wrapper.SystemMethods;

namespace VSec.DotNet.CmsCore.Wrapper.Test
{
    public class TestCaller
    {
        /// <summary>
        /// to call from another assembly and test if the into call operates
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public string InstanceTestCall(string input = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return $"I came from instanciated TestCall Used System: {CurrentSystem._Instance.OsPlatform} DotNet Version: {CurrentSystem._Instance.DotNetVersion}";
            }
            return input;
        }

        /// <summary>
        /// to call from another assembly and test if the into call operates
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string StaticTestCall(string input = "")
        {
            if (string.IsNullOrEmpty(input))
            {
                return $"I came from static TestCall Used System: {CurrentSystem._Instance.OsPlatform} DotNet Version: {CurrentSystem._Instance.DotNetVersion}";
            }
            return input;
        }
    }
}
