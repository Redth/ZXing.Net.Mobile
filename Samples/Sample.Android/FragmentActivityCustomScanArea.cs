using System;
using System.Collections.Generic;
using ZXing.Mobile;
using Android.OS;

using Android.App;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using Xamarin.Essentials;
using Android.Content;
using AndroidX.ConstraintLayout.Widget;

namespace Sample.Android
{
    [Activity(Label = "ZXing.Net.Mobile", Theme = "@style/Theme.AppCompat.Light", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden)]
    public class FragmentActivityCustomScanArea : AndroidX.Fragment.App.FragmentActivity
    {
		ZXingScannerFragment scanFragment;
        Button buttonRectangle;
        Button buttonSquare;
        Button buttonRandom;
        ImageButton buttonIncrease;
        ImageButton buttonDecrease;
        ToggleButton scanViewPosition;
        View scanArea;
        bool scanAreaIsRectangle = true;
        bool isScanAreaCentered = false;

        protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

            SetContentView (Resource.Layout.FragmentActivity);

		}

        protected override void OnResume ()
        {
            base.OnResume ();


            if (scanFragment == null)
            {
                var zxingOverlay = LayoutInflater.FromContext(this).Inflate(Resource.Layout.ZxingOverlayCustomScanArea, null);
                scanArea = zxingOverlay.FindViewById<View>(Resource.Id.scanView);

                //Find all the buttons and wire up their events
                buttonRectangle = zxingOverlay.FindViewById<Button>(Resource.Id.buttonSquareScanRectangle);
                buttonSquare = zxingOverlay.FindViewById<Button>(Resource.Id.buttonSquareScanView);
                scanViewPosition = zxingOverlay.FindViewById<ToggleButton>(Resource.Id.toggleButtonScanViewLocation);
                buttonIncrease = zxingOverlay.FindViewById<ImageButton>(Resource.Id.buttonIncrease);
                buttonDecrease = zxingOverlay.FindViewById<ImageButton>(Resource.Id.buttonDecrease);
                buttonRandom = zxingOverlay.FindViewById<Button>(Resource.Id.buttonRandom);

                buttonRectangle.Click += Rectangle_Click;
                buttonSquare.Click += Square_Click;
                scanViewPosition.Click += ScanViewPosition_Click;
                buttonIncrease.Click += ButtonIncrease_Click;
                buttonDecrease.Click += ButtonDecrease_Click;
                buttonRandom.Click += ButtonRandom_Click;

                scanFragment = new ZXingScannerFragment(zxingOverlay, true, scanArea);                   

                SupportFragmentManager.BeginTransaction()
                    .Replace(Resource.Id.fragment_container, scanFragment)
                    .Commit();
            }

                Scan();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
            => Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        protected override void OnPause()
        {
            scanFragment?.StopScanning();

            base.OnPause();
        }

        void Scan ()
        {
            var opts = new MobileBarcodeScanningOptions {
                
                CameraResolutionSelector = availableResolutions => {

                    foreach (var ar in availableResolutions) {
                        Console.WriteLine ("Resolution: " + ar.Width + "x" + ar.Height);
                    }
                    return null;
                }
            };

            scanFragment.StartScanning(result => {

                var resultIntent = new Intent();
                string resultText;
                // Null result means scanning was cancelled
                if (result == null || string.IsNullOrEmpty (result.Text)) {
                    resultText = "";
                }
               else
                {
                    resultText = result.Text;
                }
                // Otherwise, proceed with result
                
                resultIntent.PutExtra(Activity1.RESULT_EXTRA_NAME, resultText);
                SetResult(Result.Ok, resultIntent);
                Finish();
            }, opts);
        }

        void ButtonRandom_Click(object sender, EventArgs e)
        {
            buttonRandom.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            var parentLayout = (ConstraintLayout)scanArea.Parent;
            var rand = new Random();

            var layoutParams = (ViewGroup.MarginLayoutParams)scanArea.LayoutParameters;

            if (scanAreaIsRectangle)
            {
                var moveUp = rand.Next(0, 100) % 2 == 0;

                if (isScanAreaCentered)
                {
                    if (moveUp)
                    {
                        layoutParams.BottomMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                        layoutParams.LeftMargin = rand.Next(0, parentLayout.Width - scanArea.Width);
                    }
                    else
                    {
                        layoutParams.TopMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                        layoutParams.RightMargin = rand.Next(0, parentLayout.Width - scanArea.Width);
                    }
                }
                else
                {
                    if (moveUp)
                    {
                        layoutParams.BottomMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                    }
                    else
                        layoutParams.TopMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                }
            }
            else
            {
                var moveUp = rand.Next(0, 100) % 2 == 0;

                if (moveUp)
                {
                    layoutParams.BottomMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                    layoutParams.LeftMargin = rand.Next(0, parentLayout.Width - scanArea.Width);
                }
                else
                {
                    layoutParams.TopMargin = rand.Next(0, parentLayout.Height - scanArea.Height);
                    layoutParams.RightMargin = rand.Next(0, parentLayout.Width - scanArea.Width);
                }
            }
            scanArea.LayoutParameters = layoutParams;
        }

        void ButtonDecrease_Click(object sender, EventArgs e)
        {
            buttonDecrease.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            var layout = scanArea.LayoutParameters;
            var parentLayout = (ConstraintLayout)scanArea.Parent;
            var height = layout.Height;
            var width = layout.Width;
            height = (int)(height * .9);
            width = (int)(width * .9);
            if (height > parentLayout.Height) height = parentLayout.Height;
            if (width > parentLayout.Width) width = parentLayout.Width;
            layout.Height = height;
            layout.Width = width;
            scanArea.LayoutParameters = layout;
        }

        void ButtonIncrease_Click(object sender, EventArgs e)
        {
            buttonIncrease.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            var layout = scanArea.LayoutParameters;
            var parentLayout = (ConstraintLayout)scanArea.Parent;
            var height = layout.Height;
            var width = layout.Width;
            height = (int)(height * 1.1);
            width = (int)(width * 1.1);
            if (height > parentLayout.Height) height = parentLayout.Height;
            if (width > parentLayout.Width) width = parentLayout.Width;
            layout.Height = height;
            layout.Width = width;
            scanArea.LayoutParameters = layout;
        }

        int ConvertDpToPx(int dp)
        {
            var density = ApplicationContext.Resources
                                   .DisplayMetrics
                                   .Density;
            return (int)Math.Round(dp * density + .5f);
        }

        void ScanViewPosition_Click(object sender, EventArgs e)
        {
            scanViewPosition.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            isScanAreaCentered = scanViewPosition.Checked;

            if (scanAreaIsRectangle)
            {
                if (isScanAreaCentered) SetRectangleCentered();
                else SetRectangleFullWidth();
            }
            else if (isScanAreaCentered) SetSquareCentered();
            else SetSquareFullWidth();
        }

        void Square_Click(object sender, EventArgs e)
        {
            buttonSquare.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            if (scanViewPosition.Checked)
            {
                SetSquareCentered();
            }
            else
            {
                SetSquareFullWidth();
            }
        }

        void Rectangle_Click(object sender, EventArgs e)
        {
            buttonRectangle.PerformHapticFeedback(FeedbackConstants.KeyboardTap);
            if (scanViewPosition.Checked)
            {
                SetRectangleCentered();
            }
            else
            {
                SetRectangleFullWidth();
            }
        }

        void SetRectangleCentered()
        {
            var layout = (ViewGroup.MarginLayoutParams)scanArea.LayoutParameters;
            layout.MarginEnd = 0;
            layout.LeftMargin = 0;
            layout.RightMargin = 0;
            layout.TopMargin = 0;

            layout.Height = ConvertDpToPx(75);
            layout.Width = ConvertDpToPx(150);
            scanArea.LayoutParameters = layout;
            isScanAreaCentered = true;
            scanAreaIsRectangle = true;
        }

        void SetRectangleFullWidth()
        {
            var layout = (ViewGroup.MarginLayoutParams)scanArea.LayoutParameters;
            layout.MarginEnd = 0;
            layout.LeftMargin = 0;
            layout.RightMargin = 0;
            layout.TopMargin = 0;
            var parentLayout = (ConstraintLayout)scanArea.Parent;
            layout.Height = ConvertDpToPx(120);
            layout.Width = parentLayout.Width;
            scanArea.LayoutParameters = layout;
            isScanAreaCentered = false;
            scanAreaIsRectangle = true;
        }

        void SetSquareCentered()
        {
            var layout = (ViewGroup.MarginLayoutParams)scanArea.LayoutParameters;
            layout.MarginEnd = 0;
            layout.LeftMargin = 0;
            layout.RightMargin = 0;
            layout.TopMargin = 0;
            layout.Height = ConvertDpToPx(120);
            layout.Width = ConvertDpToPx(120);
            scanArea.LayoutParameters = layout;
            isScanAreaCentered = true;
            scanAreaIsRectangle = false;
        }

        void SetSquareFullWidth()
        {
            var layout = (ViewGroup.MarginLayoutParams)scanArea.LayoutParameters;
            layout.MarginEnd = 0;
            layout.LeftMargin = 0;
            layout.RightMargin = 0;
            layout.TopMargin = 0;
            var parentLayout = (ConstraintLayout)scanArea.Parent;
            if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Landscape)
            {
                layout.Height = parentLayout.Height;
                layout.Width = parentLayout.Height;
            }
            else
            {
                layout.Height = parentLayout.Width;
                layout.Width = parentLayout.Width;
            }
            scanArea.LayoutParameters = layout;
            isScanAreaCentered = false;
            scanAreaIsRectangle = false;
        }
    }
}

