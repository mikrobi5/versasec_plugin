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
         public enum SECRET_PURPOSE
 {
 AuthenticationPin,                  // Authentication PIN
 DigitalSignaturePin,                // Digital Signature PIN
 EncryptionPin,                      // Encryption PIN
 NonRepudiationPin,                  // Non Repudiation PIN
 AdministratorPin,                   // Administrator PIN
 PrimaryCardPin,                     // Primary Card PIN
 UnblockOnlyPin
 }

}


