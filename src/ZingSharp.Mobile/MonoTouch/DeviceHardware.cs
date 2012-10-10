using System;
using System.Runtime.InteropServices;
using MonoTouch.Foundation;

using Constants = MonoTouch.Constants;

namespace MBZxing
{
    /// <summary>
    /// This code source is:
    /// http://snippets.dzone.com/user/zachgris
    /// 
    /// Detail descriptions how to determine what iPhone hardware is:
    /// http://www.drobnik.com/touch/2009/07/determining-the-hardware-model/    
    /// </summary>
    public class DeviceHardware
    {
        // make sure to add a 'using System.Runtime.InteropServices;' line to your file
        public const string HardwareProperty = "hw.machine";

        public enum HardwareVersion
        {
            iPhone1G,
            iPhone2G,
            iPhone3G,
			iPhone4,
			iPhone4S,
            iPod1G,
            iPod2G,
            iPod3G,
			iPod4G,
			iPad,
			iPad2,
		    Simulator,
            Unknown
        }

        [DllImport(Constants.SystemLibrary)]
        internal static extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string property, // name of the property
                                               IntPtr output, // output
                                               IntPtr oldLen, // IntPtr.Zero
                                               IntPtr newp, // IntPtr.Zero
                                               uint newlen // 0
                                              );
 
		
		
		public static HardwareVersion Version
        {
            get
            {
               
				
				// get the length of the string that will be returned
                var pLen = Marshal.AllocHGlobal(sizeof(int));
                sysctlbyname(DeviceHardware.HardwareProperty, IntPtr.Zero, pLen, IntPtr.Zero, 0);

                var length = Marshal.ReadInt32(pLen);

                // check to see if we got a length
                if (length == 0)
                {
                    Marshal.FreeHGlobal(pLen);
                    return HardwareVersion.Unknown;
                }

                // get the hardware string
                var pStr = Marshal.AllocHGlobal(length);
                sysctlbyname(DeviceHardware.HardwareProperty, pStr, pLen, IntPtr.Zero, 0);

                // convert the native string into a C# string
                var hardwareStr = Marshal.PtrToStringAnsi(pStr);
                var ret = HardwareVersion.Unknown;


                // determine which hardware we are running
                if (hardwareStr == "iPhone1,1")
                    ret = HardwareVersion.iPhone1G;
                else if (hardwareStr == "iPhone1,2")
                    ret = HardwareVersion.iPhone2G;
                else if (hardwareStr == "iPhone2,1")
                    ret = HardwareVersion.iPhone3G;
				else if (hardwareStr == "iPhone3,1")
                    ret = HardwareVersion.iPhone4;
				else if (hardwareStr == "iPhone4,1")
					ret = HardwareVersion.iPhone4S;
                else if (hardwareStr == "iPod1,1")
                    ret = HardwareVersion.iPod1G;
                else if (hardwareStr == "iPod2,1")
                    ret = HardwareVersion.iPod2G;
                else if (hardwareStr == "iPod3,1")
                    ret = HardwareVersion.iPod3G;
				else if (hardwareStr == "iPod4,1")
                    ret = HardwareVersion.iPod4G;
				else if (hardwareStr == "iPad1,1")
                    ret = HardwareVersion.iPad;
				else if (hardwareStr == "iPad2,1")
                    ret = HardwareVersion.iPad2;
				else if (hardwareStr == "iPad2,2")
                    ret = HardwareVersion.iPad2;
				else if (hardwareStr == "iPad2,3")
                    ret = HardwareVersion.iPad2;
			    else if (hardwareStr == "i386")
                    ret = HardwareVersion.Simulator;

                // cleanup
                Marshal.FreeHGlobal(pLen);
                Marshal.FreeHGlobal(pStr);

                return ret;
            }
        }
    }
}

