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
         public enum CARD_FILE_ACCESS_CONDITION
 {
 // Invalid value, chosed to cooincide with common initialization
 // of memory
 InvalidAc = 0,

 // Everyone     Read
 // User         Read, Write
 //
 // Example:  A user certificate file.
 EveryoneReadUserWriteAc,

 // Everyone     None
 // User         Write, Execute
 //
 // Example:  A private key file.
 UserWriteExecuteAc,

 // Everyone     Read
 // Admin        Read, Write
 //
 // Example:  The Card Identifier file.
 EveryoneReadAdminWriteAc,

 // Explicit value to set when it is desired to say that
 // it is unknown
 UnknownAc,

 // Everyone No Access 
 // User Read Write 
 // 
 // Example:  A password wallet file. 

 UserReadWriteAc,
 // Everyone/User No Access 
 // Admin Read Write 
 // 
 // Example:  Administration data. 

 AdminReadWriteAc
 }

}


