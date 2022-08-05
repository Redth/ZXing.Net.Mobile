using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Views;

namespace ZXing.Net.Mobile.Android
{
    public class DeviceOrientationEventListener : OrientationEventListener
    {
        public int Orientation { get; private set; }

        public bool IsEnabled { get; private set; } = false;

        public DeviceOrientationEventListener(Context context)
            : base(context)
        {
        }

        public DeviceOrientationEventListener(Context context, [GeneratedEnum] SensorDelay rate)
            : base(context, rate)
        {
        }

        public override void OnOrientationChanged(int orientation)
        {
            if (orientation != OrientationUnknown)
                Orientation = orientation;
        }

        public override void Enable()
        {
            base.Enable();
            IsEnabled = true;
        }

        public override void Disable()
        {
            base.Disable();
            IsEnabled = false;
        }
    }
}
