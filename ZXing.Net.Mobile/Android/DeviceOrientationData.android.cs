using Android.Content.Res;

namespace ZXing.Net.Mobile.Android
{
    public class DeviceOrientationData
    {
        public Orientation OrientationMode { get; }

        public int DeviceOrientation { get; }

        public int SensorRotation { get; }

        public DeviceOrientationData(Orientation orientationMode, int orientation, int sensorRotation)
        {
            OrientationMode = orientationMode;
            DeviceOrientation = orientation;
            SensorRotation = sensorRotation;
        }
    }
}
