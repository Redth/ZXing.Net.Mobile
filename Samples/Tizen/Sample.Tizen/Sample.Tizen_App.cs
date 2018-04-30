using Tizen.Applications;
using ElmSharp;
using ZXing.Mobile;
using ZXing;
using Tizen.Security;

namespace Sample.Tizen
{
    class App : CoreUIApplication
    {

        protected override void OnCreate()
        {
            base.OnCreate();
            Initialize();
        }
        private Window MainWindow;
        private MobileBarcodeScanner scanner;

        void Initialize()
        {
            MainWindow = new Window("ZXingTizenSample")
            {
                AvailableRotations = DisplayRotation.Degree_0 | DisplayRotation.Degree_180 | DisplayRotation.Degree_270 | DisplayRotation.Degree_90
            };
            MainWindow.BackButtonPressed += (s, e) =>
            {
                Exit();
            };
            MainWindow.Show();

            var box = new Box(MainWindow)
            {
                AlignmentX = -1,
                AlignmentY = -1,
                WeightX = 1,
                WeightY = 1,
            };
            box.Show();

            var bg = new Background(MainWindow)
            {
                Color = Color.White
            };
            bg.SetContent(box);

            var conformant = new Conformant(MainWindow);
            conformant.Show();
            conformant.SetContent(bg);

            var buttonScanDefaultView = new Button(MainWindow)
            {
                Text = "Scan with Default Overlay",
                AlignmentX = -1,
                WeightX = 1,
            };
            buttonScanDefaultView.Show();
            box.PackEnd(buttonScanDefaultView);
            buttonScanDefaultView.Clicked += async (s, e) =>
            {
                MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
                scanner = new MobileBarcodeScanner();
                scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
                scanner.BottomText = "Wait for the barcode to automatically scan!";
                Result r = await scanner.Scan(options);
                ToastMessage(r.Text);
            };

            var buttonScanCustomView = new Button(MainWindow)
            {
                Text = "Scan with Custom Overlay",
                AlignmentX = -1,
                WeightX = 1,
            };
            buttonScanCustomView.Show();
            box.PackEnd(buttonScanCustomView);
            buttonScanCustomView.Clicked += async (s, e) =>
            {
                scanner = new MobileBarcodeScanner();
                scanner.UseCustomOverlay = true;
                scanner.CustomOverlay = GetCustomOverlay(scanner.MainWindow);
                MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
                Result r = await scanner.Scan(options);
                ToastMessage(r.Text);

            };

            var buttonContinuousScan = new Button(MainWindow)
            {
                Text = "Scan Countinuously",
                AlignmentX = -1,
                WeightX = 1,
            };
            buttonContinuousScan.Show();
            box.PackEnd(buttonContinuousScan);
            buttonContinuousScan.Clicked += (s, e) =>
            {
                MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
                scanner = new MobileBarcodeScanner();
                scanner.TopText = "Hold the camera up to the barcode\nAbout 6 inches away";
                scanner.BottomText = "Wait for the barcode to automatically scan!";
                scanner.ScanContinuously(options, (Result r) =>
                {
                    ToastMessage(r.Text);
                });
            };

            var buttonGenerate = new Button(MainWindow)
            {
                Text = "Barcode Generaotr",
                AlignmentX = -1,
                WeightX = 1,
            };
            buttonGenerate.Show();
            box.PackEnd(buttonGenerate);
            buttonGenerate.Clicked += (s, e) =>
            {
                BarcodeImageViewer barcodeImageViewer = new BarcodeImageViewer();
                barcodeImageViewer.Show();

            };
        }
        private void ToastMessage(string msg)
        {
            ToastMessage toast = new ToastMessage();
            toast.Message = msg;
            toast.Post();
        }
        private Container GetCustomOverlay(Window window)
        {
            var customOverlay = new Box(window)
            {
                AlignmentX = -1,
                AlignmentY = -1,
                WeightX = 1,
                WeightY = 1,
                BackgroundColor = Color.Transparent,
            };
            customOverlay.Show();

            var flashButton = new Button(window)
            {
                AlignmentX = -1,
                AlignmentY = -1,
                WeightX = 1,
                Text = "Toggle Torch",
            };
            flashButton.Show();
            customOverlay.PackEnd(flashButton);
            flashButton.Clicked += (s, e) =>
            {
                scanner.Torch(true);
            };
            return customOverlay;
        }
        static void Main(string[] args)
        {
            Elementary.Initialize();
            Elementary.ThemeOverlay();
            App app = new App();
            CheckResult result = PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/camera");
            switch (result)
            {
                case CheckResult.Allow:
                    app.Run(args);
                    break;
                default:
                    break;
            }
        }
    }
}
