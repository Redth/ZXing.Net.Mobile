using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#if NET20 && !UNITY
[assembly: AssemblyTitle("zxing.net for .net 2.0")]
#endif
#if NET35 && !UNITY
[assembly: AssemblyTitle("zxing.net for .net 3.5")]
#endif
#if NET35 && UNITY
[assembly: AssemblyTitle("zxing.net for .net 3.5 and unity (w/o System.Drawing)")]
#endif
#if NET40
[assembly: AssemblyTitle("zxing.net for .net 4.0")]
#endif
#if SILVERLIGHT4
[assembly: AssemblyTitle("zxing.net for silverlight 4")]
#endif
#if SILVERLIGHT5
[assembly: AssemblyTitle("zxing.net for silverlight 5")]
#endif
#if WINDOWS_PHONE70
[assembly: AssemblyTitle("zxing.net for windows phone 7.0")]
#endif
#if WINDOWS_PHONE71
[assembly: AssemblyTitle("zxing.net for windows phone 7.1")]
#endif
#if WINDOWS_PHONE80
[assembly: AssemblyTitle("zxing.net for windows phone 8.0")]
#endif
#if WINDOWS_PHONE81
[assembly: AssemblyTitle("zxing.net for windows phone 8.1")]
#endif
#if MONOANDROID
[assembly: AssemblyTitle("zxing.net for mono android")]
#endif
#if MONOTOUCH
[assembly: AssemblyTitle("zxing.net for mono touch")]
#endif
[assembly: AssemblyDescription("port of the java based barcode scanning library for .net (java zxing 08.08.2016 10:09:53)")]
[assembly: AssemblyCompany("ZXing.Net Development")]
[assembly: AssemblyProduct("ZXing.Net")]
[assembly: AssemblyCopyright("Copyright � 2012-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#if !PORTABLE
[assembly: Guid("ECE3AB74-9DD1-4CFB-9D48-FCBFB30E06D6")]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Revision
//      Build Number
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:

[assembly: AssemblyVersion("0.14.1.0")]
#if !WindowsCE
[assembly: AssemblyFileVersion("0.14.1.0")]
#endif

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("zxing.test, PublicKey=0024000004800000140100000602000000240000525341310008000001000100014c9a01956f13a339130616473f69f975e086d9a3a56278936b12c48ca45a4ddfee05c21cdc22aedd84e9468283127a20bba4761c4e0d9836623fc991d562a508845fe314a435bd6c6ff4b0b1d7a141ef93dc1c62252438723f0f93668288673ea6042e583b0eed040e3673aca584f96d4dca19937fbed30e6cd3c0409db82d5c5d2067710d8d86e008447201d99238b94d91171bb0edf3e854985693051ba5167ca6ae650aca5dd65471d68835db00ce1728c58c7bbf9a5d152f491123caf9c0f686dc4e48e1ef63eaf738a12b3771c24d595cc5a5b5daf2cc7611756e9ba3cc89f08fb9adf39685bd5356858c010eb9aa8a767e5ef020408e0c9746cbb5a8")]
[assembly: InternalsVisibleTo("zxing.sl4.test, PublicKey=0024000004800000140100000602000000240000525341310008000001000100014c9a01956f13a339130616473f69f975e086d9a3a56278936b12c48ca45a4ddfee05c21cdc22aedd84e9468283127a20bba4761c4e0d9836623fc991d562a508845fe314a435bd6c6ff4b0b1d7a141ef93dc1c62252438723f0f93668288673ea6042e583b0eed040e3673aca584f96d4dca19937fbed30e6cd3c0409db82d5c5d2067710d8d86e008447201d99238b94d91171bb0edf3e854985693051ba5167ca6ae650aca5dd65471d68835db00ce1728c58c7bbf9a5d152f491123caf9c0f686dc4e48e1ef63eaf738a12b3771c24d595cc5a5b5daf2cc7611756e9ba3cc89f08fb9adf39685bd5356858c010eb9aa8a767e5ef020408e0c9746cbb5a8")]