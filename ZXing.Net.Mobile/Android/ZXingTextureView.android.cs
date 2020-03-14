using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Util;
using Android.Views;
using ZXing.Mobile;
using ZXing.Mobile.CameraAccess;
using SurfaceTexture = Android.Graphics.SurfaceTexture;

namespace ZXing.Mobile
{
	public class ZXingTextureView : TextureView, TextureView.ISurfaceTextureListener, IScannerSessionHost, IScannerView
	{
		LayoutInflater layoutInflater;
		Camera camera;
		View cameraView;
		float transparentLevel;
		CameraAnalyzer cameraAnalyzer;
		internal ZXingScannerFragment parentFragment;

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

		// We want to check if the parent was set and use IT'S options
		// Otherwise use a local set since someone used the fragment directly
		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => parentFragment?.ScanningOptions ?? options;
			set
			{
				if (parentFragment != null)
					parentFragment.ScanningOptions = value;
				else
					options = value;
			}
		}

		public bool IsTorchOn { get; }
		public bool IsAnalyzing { get; }
		public bool HasTorch { get; }

		public ZXingTextureView(Context context)
			: base(context)
			=> Init();

		public ZXingTextureView(Context context, IAttributeSet attr)
			: base(context, attr)
			=> Init();

		public ZXingTextureView(Context context, IAttributeSet attr, int defStyleAttr)
			: base(context, attr, defStyleAttr)
			=> Init();

		public ZXingTextureView(Context context, IAttributeSet attr, int defStyleAttr, int defStyleRes)
			: base(context, attr, defStyleAttr, defStyleRes)
			=> Init();

		void Init()
		{
			SurfaceTextureListener = this;

			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzer(this, this);

			cameraAnalyzer.ResumeAnalysis();
		}

		public async void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			await Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync();

			cameraAnalyzer.SetupCamera(surface);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			cameraAnalyzer.ShutdownCamera();
			return false;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
			=> cameraAnalyzer.RefreshCamera(surface);

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
			=> cameraAnalyzer.RefreshCamera(surface);

		public void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{ }

		public void StopScanning()
		{
			// throw new NotImplementedException();
		}

		public void PauseAnalysis()
			=> cameraAnalyzer.PauseAnalysis();

		public void ResumeAnalysis()
			=> cameraAnalyzer.ResumeAnalysis();

		public void Torch(bool on)
		{ }

		public void AutoFocus()
			=> cameraAnalyzer.AutoFocus();

		public void AutoFocus(int x, int y)
			=> cameraAnalyzer.AutoFocus();

		public void ToggleTorch()
		{

		}
	}
}
