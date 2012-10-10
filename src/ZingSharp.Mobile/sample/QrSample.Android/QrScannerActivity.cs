using System;
using System.Collections;

using Android.App;
using Android.Views;
using Android.OS;
using Android.Hardware;

using com.google.zxing;
using com.google.zxing.common;
using com.google.zxing.client.android;

namespace QrSample.Android
{
	[Activity (Label = "QR Scanner", MainLauncher = true)]
	public class QrScannerActivity : Activity
	{
		QrScanner scanner;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			scanner = new QrScanner (this);
			SetContentView (scanner);
		}

		public virtual void OnQrScan (com.google.zxing.Result result)
		{
			var builder = new AlertDialog.Builder (this);
			builder.SetTitle ("QR Result");
			builder.SetMessage (result.Text);
			using (var dlg = builder.Create ())
				dlg.Show ();
		}

		// based on https://github.com/xamarin/monodroid-samples/blob/master/ApiDemo/Graphics/CameraPreview.cs
		class QrScanner : SurfaceView, ISurfaceHolderCallback, Camera.IPreviewCallback, Camera.IAutoFocusCallback
		{
			QrScannerActivity activity;
			ISurfaceHolder surface_holder;
			Camera camera;

			int width, height;
			MultiFormatReader reader;

			public QrScanner (QrScannerActivity activity)
				: base (activity)
			{
				this.activity = activity;
				this.reader = new MultiFormatReader {
					Hints = new Hashtable {
						{ DecodeHintType.POSSIBLE_FORMATS, new ArrayList { BarcodeFormat.QR_CODE } }
					}
				};

				this.surface_holder = Holder;
				this.surface_holder.AddCallback (this);
				this.surface_holder.SetType (SurfaceType.PushBuffers);
			}
	
			public void SurfaceCreated (ISurfaceHolder holder)
			{
				try {
					camera = Camera.Open ();
					camera.SetPreviewDisplay (holder);
					camera.SetPreviewCallback (this);

				} catch (Exception e) {
					ShutdownCamera ();

					// TODO: log or otherwise handle this exception

					throw;
				}
			}
	
			public void SurfaceChanged (ISurfaceHolder holder, global::Android.Graphics.Format format, int w, int h)
			{
				if (camera == null)
					return;
	
				Camera.Parameters parameters = camera.GetParameters ();

				width = parameters.PreviewSize.Width;
				height = parameters.PreviewSize.Height;

				camera.SetParameters (parameters);
				camera.SetDisplayOrientation (90);
				camera.StartPreview ();
				camera.AutoFocus (this);
			}
	
			public void SurfaceDestroyed (ISurfaceHolder holder)
			{
				ShutdownCamera ();
			}
	
			public void OnPreviewFrame (byte [] bytes, Camera camera)
			{
				try {
	
					var luminance = new YUVLuminanceSource ((sbyte[])(Array)bytes, width, height, 0, 0, width, height);
					var binarized = new BinaryBitmap (new HybridBinarizer (luminance));
					var result = reader.decodeWithState (binarized);
	
					// an exception would be thrown before this point if the QR code was not detected
	
					if (string.IsNullOrEmpty (result.Text))
						return;
	
					//ShutdownCamera ();

					activity.OnQrScan (result);

				} catch (ReaderException) {

					// ignore this exception; it happens every time there is a failed scan

				} catch (Exception e){

					// TODO: this one is unexpected.. log or otherwise handle it

					throw;
				}
			}
	
			public void OnAutoFocus (bool success, Camera camera)
			{
				// TODO: it might be a good idea to focus again after a delay
			}

			void ShutdownCamera ()
			{
				if (camera != null) {
					camera.SetPreviewCallback (null);
					camera.StopPreview ();
					camera.Release ();
					camera = null;
				}
			}
		}
	}
}