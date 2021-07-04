using System.Runtime.InteropServices;
using VSec.DotNet.CmsCore.Wrapper.Natives;
using VSec.DotNet.CmsCore.Wrapper.Natives.Enums;
using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;

namespace VSec.DotNet.CmsCore.Wrapper.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CardKeySizes
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CardKeySizes"/> class.
        /// </summary>
        /// <param name="cmsCardKeySize">Size of the CMS card key.</param>
        public CardKeySizes(CMSCORE_CARD_KEY_SIZES cmsCardKeySize)
        {
            Version = (uint)(cmsCardKeySize.dwVersion);
            DefaultBitLength = (uint)(cmsCardKeySize.dwDefaultBitlen);
            IncrementalBitLength = (uint)(cmsCardKeySize.dwIncrementalBitlen);
            MaximumBitLength = (uint)(cmsCardKeySize.dwMaximumBitlen);
            MinimumBitLength = (uint)(cmsCardKeySize.dwMinimumBitlen);
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public uint Version { get; set; }
        /// <summary>
        /// Gets the default length of the bit.
        /// </summary>
        /// <value>
        /// The default length of the bit.
        /// </value>
        public uint DefaultBitLength { get; }
        /// <summary>
        /// Gets the length of the incremental bit.
        /// </summary>
        /// <value>
        /// The length of the incremental bit.
        /// </value>
        public uint IncrementalBitLength { get; }
        /// <summary>
        /// Gets the maximum length of the bit.
        /// </summary>
        /// <value>
        /// The maximum length of the bit.
        /// </value>
        public uint MaximumBitLength { get; }
        /// <summary>
        /// Gets the minimum length of the bit.
        /// </summary>
        /// <value>
        /// The minimum length of the bit.
        /// </value>
        public uint MinimumBitLength { get; }
    }
}
