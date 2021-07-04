using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace VSec.DotNet.CmsCore.Wrapper.SystemMethods
{
    public class CurrentSystem
    {
        private static readonly Lazy<CurrentSystem> lazy = new Lazy<CurrentSystem>(() => new CurrentSystem());

        public static CurrentSystem _Instance { get { return lazy.Value; } }


        public string OsPlatform { get; set; }

        public string DotNetVersion { get; set; }

        private CurrentSystem()
        {
            var framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            OsPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            DotNetVersion = framework;
        }
    }
}
