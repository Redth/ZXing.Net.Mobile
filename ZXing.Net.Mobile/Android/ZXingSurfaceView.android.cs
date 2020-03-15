using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Mobile.CameraAccess;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile
{
	public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, IScannerView, IScannerSessionHost
	{
		public ZXingSurfaceView(Context context)
			: this(null, context)
		{
		}

		internal ZXingSurfaceView(ZXingScannerFragment parentFragment, Context context)
			: base(context)
		{
			ParentFragment = parentFragment;
			Init();
		}

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => ParentFragment?.ScanningOptions ?? options;
			set
			{
				if (ParentFragment != null)
					ParentFragment.ScanningOptions = value;
				else
					options = value;
			}
		}

		internal ZXingScannerFragment ParentFragment { get; private set; }

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer) => Init();

		bool addedHolderCallback = false;

		void Init()
		{
			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzer(this, this,
					r => OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(r)));

			cameraAnalyzer.ResumeAnalysis();

			if (!addedHolderCallback)
			{
				Holder.AddCallback(this);
				Holder.SetType(SurfaceType.PushBuffers);
				addedHolderCallback = true;
			}
		}

		public async void SurfaceCreated(ISurfaceHolder holder)
		{
			await PermissionsHandler.RequestPermissionsAsync();

			cameraAnalyzer.SetupCamera();

			surfaceCreated = true;
		}

		public async void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
			=> cameraAnalyzer.RefreshCamera();

		public async void SurfaceDestroyed(ISurfaceHolder holder)
		{
			try
			{
				if (addedHolderCallback)
				{
					Holder.RemoveCallback(this);
					addedHolderCallback = false;
				}
			}
			catch { }

			cameraAnalyzer.ShutdownCamera();
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			var r = base.OnTouchEvent(e);

			switch (e.Action)
			{
				case MotionEventActions.Down:
					return true;
				case MotionEventActions.Up:
					var touchX = e.GetX();
					var touchY = e.GetY();
					AutoFocus((int)touchX, (int)touchY);
					break;
			}

			return r;
		}

		public void AutoFocus()
			=> cameraAnalyzer.AutoFocus();

		public void AutoFocus(int x, int y)
			=> cameraAnalyzer.AutoFocus(x, y);

		public void StopScanning()
			=> cameraAnalyzer.ShutdownCamera();

		public void PauseAnalysis()
			=> cameraAnalyzer.PauseAnalysis();

		public void ResumeAnalysis()
			=> cameraAnalyzer.ResumeAnalysis();

		public void Torch(bool on)
		{
			if (on)
				cameraAnalyzer.Torch.TurnOn();
			else
				cameraAnalyzer.Torch.TurnOff();
		}

		public void ToggleTorch()
			=> cameraAnalyzer.Torch.Toggle();

		public bool IsTorchOn => cameraAnalyzer.Torch.IsEnabled;

		public bool IsAnalyzing => cameraAnalyzer.IsAnalyzing;

		CameraAnalyzer cameraAnalyzer;
		bool surfaceCreated;

		public bool HasTorch => cameraAnalyzer.Torch.IsSupported;

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Reinit things
			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);
			if (visibility == ViewStates.Visible)
				Init();
		}

		public override async void OnWindowFocusChanged(bool hasWindowFocus)
		{
			base.OnWindowFocusChanged(hasWindowFocus);

			if (!hasWindowFocus)
				return;

			//only refresh the camera if the surface has already been created. Fixed #569
			if (surfaceCreated)
				cameraAnalyzer.RefreshCamera();
		}
	}
}
