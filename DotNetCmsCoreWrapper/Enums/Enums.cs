using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace VSec.DotNet.CmsCore.Wrapper.Enums
{
    /// <summary>
    /// define  possbile user roles
    /// </summary>
    public enum Roles
    {
        /// <summary>
        /// for no use of any role
        /// </summary>
        [Description("None")]
        None = 0,
        /// <summary>
        /// normal user 
        /// </summary>
        [Description("User")]
        User = 1,
        /// <summary>
        /// admin user
        /// </summary>
        [Description("Admin")]
        Admin,
        /// <summary>
        /// test - for future use
        /// </summary>
        [Description("Test1")]
        Test1,
        /// <summary>
        /// test - for future use
        /// </summary>
        [Description("Test2")]
        Test2,
        /// <summary>
        /// test - for future use
        /// </summary>
        [Description("Test3")]
        Test3,
        /// <summary>
        /// test - for future use
        /// </summary>
        [Description("Test4")]
        Test4
    }
}
