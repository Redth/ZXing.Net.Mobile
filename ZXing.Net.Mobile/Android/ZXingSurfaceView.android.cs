using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Net.Mobile.Android;
using System.Threading.Tasks;

namespace ZXing.UI
{
	public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, IScannerView
	{
		public ZXingSurfaceView(Context context, BarcodeScanningOptions options = null, BarcodeScannerOverlay<View> overlay = null)
			: base(context)
		{
			Options = options ?? new BarcodeScanningOptions();
			Overlay = overlay;

			Init();
		}

		public BarcodeScanningOptions Options { get; }
		public new BarcodeScannerOverlay<View> Overlay { get; }

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer) => Init();

		bool addedHolderCallback = false;

		void Init()
		{
			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzer(this, Options,
					r => OnBarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(r)));

			cameraAnalyzer.IsAnalyzing = true;

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
					AutoFocusAsync((int)touchX, (int)touchY);
					break;
			}

			return r;
		}

		public Task AutoFocusAsync()
			=> cameraAnalyzer.AutoFocusAsync();

		public Task AutoFocusAsync(int x, int y)
			=> cameraAnalyzer.AutoFocusAsync(x, y);

		public new void Dispose()
		{
			cameraAnalyzer.ShutdownCamera();
			base.Dispose();
		}

		public bool IsAnalyzing
		{
			get => cameraAnalyzer?.IsAnalyzing ?? false;
			set { if (cameraAnalyzer != null) cameraAnalyzer.IsAnalyzing = value; }
		}

		public Task TorchAsync(bool on)
		{
			if (on)
				cameraAnalyzer?.Torch?.TurnOn();
			else
				cameraAnalyzer?.Torch?.TurnOff();
			return Task.CompletedTask;
		}

		public Task ToggleTorchAsync()
		{
			cameraAnalyzer?.Torch?.Toggle();
			return Task.CompletedTask;
		}

		public bool IsTorchOn => cameraAnalyzer.Torch.IsEnabled;

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
