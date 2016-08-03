#tool nuget:?package=NUnit.Runners&version=2.6.3
#addin nuget:?package=Cake.Xamarin

var TARGET = Argument ("target", Argument ("t", "Default"));

var ANDROID_DEVICES = (EnvironmentVariable ("ANDROID_DEVICES") ?? "").Split (';');
var IOS_DEVICES = (EnvironmentVariable ("IOS_DEVICES") ?? "").Split (';');
var NUNIT_PATH = GetFiles ("../packages/**/nunit.framework.dll").FirstOrDefault ();

Task ("Samples").Does (() =>
{
	EnsureDirectoryExists ("../output");

	var sampleSlns = new [] {
		"../Samples/Android/Sample.Android.sln",
		"../Samples/iOS/Sample.iOS.sln",
		"../Samples/Forms/Sample.Forms.Mac.sln",
		"../ZXing.Net.Mobile.UITests.sln"
	};
	
	foreach (var sln in sampleSlns) {
		NuGetRestore (sln);
		DotNetBuild (sln, c => c.Configuration = "Debug");
	}
});

Task ("Android.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./Sample.Android.UITests/bin/Debug/Sample.Android.UITests.dll";

	var apk = AndroidPackage ("../Samples/Android/Sample.Android/Sample.Android.csproj", false, c => c.Configuration = "Release");
	Information ("APK: {0}", apk);

	foreach (var device in ANDROID_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);
		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-Android-" + device + ".xml" });
	}
});

Task ("Forms.Android.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./FormsSample.UITests/bin/Debug/FormsSample.UITests.dll";

	var apk = AndroidPackage ("../Samples/Forms/Droid/FormsSample.Droid.csproj", false, c => c.Configuration = "Release");
	Information ("APK: {0}", apk);

	foreach (var device in ANDROID_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);
		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-Forms-" + device + ".xml" });
	}
});

RunTarget (TARGET);