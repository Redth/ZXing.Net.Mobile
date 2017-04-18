using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace BarcodeDisplayServer
{
    [BroadcastReceiver]
    [IntentFilter (new [] { Intent.ActionBootCompleted })]
    public class HttpListenerBootReceiver : BroadcastReceiver 
    {
        // Broadcast receiver to start listener on boot
        public override void OnReceive (Context context, Intent intent)
        {
            context.StartService (new Intent (context, typeof (HttpListenerService)));
        }
    }

    [Service]
    public class HttpListenerService : IntentService
    {
        static bool running = false;

        protected override void OnHandleIntent (Intent intent)
        {
            // Make sure server is running
            StartHttpServer ();
        }

        public override bool StopService (Intent name)
        {
            // Trigger cancellation token for http listener when service stops
            if (ctsHttp != null)
                ctsHttp.Cancel ();
            running = false;
            return base.StopService (name);
        }

        HttpListener httpListener;
        CancellationTokenSource ctsHttp;

        void ProcessHttpRequest (HttpListenerContext context)
        {
            try {
                string barcodeFormatStr = context.Request?.QueryString? ["format"] ?? "QR_CODE";
                string barcodeValue = context?.Request?.QueryString? ["value"] ?? "";
                string barcodeUrl = context?.Request?.QueryString? ["url"] ?? "";

                // Pass along the querystring values
                var intent = new Android.Content.Intent (this, typeof (MainActivity));
                intent.PutExtra ("FORMAT", barcodeFormatStr);
                intent.PutExtra ("VALUE", barcodeValue);
                intent.PutExtra ("URL", barcodeUrl);
                intent.AddFlags (ActivityFlags.NewTask);

                // Start the activity to show the values
                StartActivity (intent);

                // Return a success 
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.StatusDescription = "OK";
                context.Response.Close ();

            } catch (Exception e) {
                Console.WriteLine ("Error " + e.Message);

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close ();
            }
        }

        void StartHttpServer ()
        {
            if (running || httpListener != null)
                return;

            running = true;
            ctsHttp = new CancellationTokenSource ();

            // Setup our listener
            httpListener = new HttpListener ();
            httpListener.Prefixes.Add ("http://*:8158/");
            httpListener.Start ();

            var httpTask = Task.Factory.StartNew (() => {
                while (!ctsHttp.IsCancellationRequested) {
                    try {
                        var httpContext = httpListener.GetContext ();
                        Task.Run (() => {
                            ProcessHttpRequest (httpContext);
                        });

                    } catch (Exception e) {
                        Android.Util.Log.Error ("BARCODE", "HttpListener Error: {0}", e.Message);
                    }
                }
            }, TaskCreationOptions.LongRunning);

            // Stop the listener after cancel token was issued
            httpTask.ContinueWith (t => {
                if (httpListener != null)
                    httpListener.Stop ();
                httpListener = null;
            });
        }

    }
}

