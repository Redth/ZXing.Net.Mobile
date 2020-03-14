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
	public class ZXingTextureView : View, TextureView.ISurfaceTextureListener, IScannerSessionHost, IScannerView
	{
		LayoutInflater layoutInflater;
		Camera camera;
		View cameraView;
		TextureView textureView;
		float transparentLevel;
		CameraAnalyzer cameraAnalyzer;

		public MobileBarcodeScanningOptions ScanningOptions { get; }
		public bool IsTorchOn { get; }
		public bool IsAnalyzing { get; }
		public bool HasTorch { get; }

		public ZXingTextureView(Context context)
			: base(context)
		{
			textureView = new TextureView(context);
			Init();
		}

		public ZXingTextureView(Context context, IAttributeSet attr)
			: base(context, attr)
		{
			textureView = new TextureView(context, attr);
			Init();
		}

		public ZXingTextureView(Context context, IAttributeSet attr, int defStyleAttr)
			: base(context, attr, defStyleAttr)
		{
			textureView = new TextureView(context, attr, defStyleAttr);
			Init();
		}

		public ZXingTextureView(Context context, IAttributeSet attr, int defStyleAttr, int defStyleRes)
			: base(context, attr, defStyleAttr, defStyleRes)
		{
			textureView = new TextureView(context, attr, defStyleAttr, defStyleRes);
			Init();
		}

		void Init()
		{
			if (cameraAnalyzer == null)
				cameraAnalyzer = new CameraAnalyzer(textureView, this);

			cameraAnalyzer.ResumeAnalysis();

		}

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			cameraAnalyzer.SetupCamera(surface);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			cameraAnalyzer.ShutdownCamera();
			return false;
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
			cameraAnalyzer.RefreshCamera(surface);
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{
			cameraAnalyzer.RefreshCamera(surface);
		}

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
