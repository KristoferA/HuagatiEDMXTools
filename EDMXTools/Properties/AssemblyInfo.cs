using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Huagati EDMX Tools")]
[assembly: AssemblyDescription("EDMX file wrapper library")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("Huagati Systems Co., Ltd.")]
[assembly: AssemblyProduct("Huagati EDMX Tools")]
[assembly: AssemblyCopyright("Copyright © Huagati Systems Co., Ltd. 2010-2015")]
[assembly: AssemblyTrademark("Huagati DBML/EDMX Tools")]
[assembly: AssemblyCulture("")]

//make internal members visible and callable by the Huagati DBML/EDMX Tools add-in
[assembly: InternalsVisibleTo("HuagatiDBMLTools2010, PublicKey=002400000480000094000000060200000024000052534131000400000100010007b8016b016ac8df06823b899e50ddac5bedce1a3d0e5fa395f74b0232a2632e91d18adb167a551f920782f181c32fa729073f3371e5e55c2f7f253418b67d10ef23d8c0994653ec2a8688171eaa0f9689efb13bfdc35ddd16afeb1042d7dc66e074caae1d8762e981ac047a919569f6d9772403e6362d9d14f6d8d12f1c8f9c")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f47a240a-c41c-4a62-bdc4-fb3aef4e5ccf")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("2.36.*")]

