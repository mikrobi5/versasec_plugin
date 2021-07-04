/*
*  auto generate dll import file 
* enum  from c++ header file
*
*/
namespace VSec.DotNet.CmsCore.Wrapper.Natives.Enums
{
        using System.Runtime.InteropServices;
        using VSec.DotNet.CmsCore.Wrapper.Natives.Structs;
        using VSec.DotNet.CmsCore.Wrapper.Natives.Interfaces;
        //[StructLayout(LayoutKind.Sequential)]
         public enum SECRET_TYPE
 {
 AlphaNumericPinType = 0,            // Regular PIN
 ExternalPinType,                    // Biometric PIN
 ChallengeResponsePinType,           // Challenge/Response PIN
 EmptyPinType                        // No PIN
 }

}


