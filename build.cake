#tool nuget:?package=XamarinComponent

#addin nuget:?package=Cake.Android.SdkManager
#addin nuget:?package=Cake.XCode
#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.Xamarin.Build
#addin nuget:?package=Cake.SemVer
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.MonoApiTools

var PREVIEW = "";
var VERSION = EnvironmentVariable ("APPVEYOR_BUILD_VERSION") ?? Argument("version", "0.0.0");
var NUGET_VERSION = VERSION;

var ANDROID_HOME = EnvironmentVariable ("ANDROID_HOME") ?? Argument ("android_home", "");

var TARGET = Argument ("t", Argument ("target", "Default"));

var buildSpec = new BuildSpec {

	Samples = new [] {
		new DefaultSolutionBuilder { SolutionPath = "./Samples/Android/Sample.Android.sln", BuildsOn = BuildPlatforms.Windows | BuildPlatforms.Mac },
		new IOSSolutionBuilder { SolutionPath = "./Samples/iOS/Sample.iOS.sln", BuildsOn = BuildPlatforms.Mac },
		new WpSolutionBuilder { SolutionPath = "./Samples/WindowsPhone/Sample.WindowsPhone.sln", BuildsOn = BuildPlatforms.Windows },
		new DefaultSolutionBuilder { SolutionPath = "./Samples/WindowsUniversal/Sample.WindowsUniversal.sln", BuildsOn = BuildPlatforms.Windows },
		new WpSolutionBuilder { SolutionPath = "./Samples/Forms/Sample.Forms.sln", BuildsOn = BuildPlatforms.Windows },
		new WpSolutionBuilder { SolutionPath = "./Samples/Forms/Sample.Forms.WP8.sln", BuildsOn = BuildPlatforms.Windows },
	},

	// These should only get populated on windows where all the binaries will exist
	NuGets = new NuGetInfo [] {},
	Components = new Component [] {},
};

if (IsRunningOnWindows ()) {

	buildSpec.Libs = new [] {
		new WpSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.sln",
			Configuration = "ReleaseWin",
			BuildsOn = BuildPlatforms.Windows,
		},
		new WpSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.Forms.sln",
			Configuration = "ReleaseWin",
			BuildsOn = BuildPlatforms.Windows,
		},
		new WpSolutionBuilder { 
			SolutionPath = "./ZXing.Net.Mobile.WP8.sln",
			BuildsOn = BuildPlatforms.Windows 
		},
	};

	buildSpec.NuGets = new [] {
		new NuGetInfo { NuSpec = "./ZXing.Net.Mobile.nuspec", Version = NUGET_VERSION },
		new NuGetInfo { NuSpec = "./ZXing.Net.Mobile.Forms.nuspec", Version = NUGET_VERSION },
	};

	buildSpec.Components = new [] {
		new Component { ManifestDirectory = "./Component" },
		new Component { ManifestDirectory = "./Component-Forms" },
	};
} else {
	buildSpec.Libs = new [] {
		new DefaultSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.sln",
			Configuration = "ReleaseMac",
			BuildsOn = BuildPlatforms.Mac,
		},
		new DefaultSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.Forms.sln",
			Configuration = "ReleaseMac",
			BuildsOn = BuildPlatforms.Mac,
		}
	};
}

Task ("externals")
	.IsDependentOn ("externals-base")
	.Does (() =>
{
	Information ("ANDROID_HOME: {0}", ANDROID_HOME);

	var androidSdkSettings = new AndroidSdkManagerToolSettings { 
		SdkRoot = ANDROID_HOME,
		SkipVersionCheck = true
	};

	try { AcceptLicenses (androidSdkSettings); } catch { }

	AndroidSdkManagerInstall (new [] { 
			"platforms;android-15",
			"platforms;android-23",
			"platforms;android-25",
			"platforms;android-26"
		}, androidSdkSettings);
});


Task ("component-setup")
	.IsDependentOn ("samples")
	.IsDependentOn ("nuget")
	.Does (() =>
{
	var compVersion = VERSION;
	if (compVersion.Contains ("-"))
		compVersion = compVersion.Substring (0, compVersion.IndexOf ("-"));

	// Clear out xml files from build (they interfere with the component packaging)
	DeleteFiles ("./Build/**/*.xml");

	// Generate component.yaml files from templates
	CopyFile ("./Component/component.template.yaml", "./Component/component.yaml");
	CopyFile ("./Component-Forms/component.template.yaml", "./Component-Forms/component.yaml");

	// Replace version in template files
	ReplaceTextInFiles ("./**/component.yaml", "{VERSION}", compVersion);
});

Task ("component").IsDependentOn ("component-setup").IsDependentOn ("component-base");

Task ("Default").IsDependentOn ("component");

Task ("clean").IsDependentOn ("clean-base").Does (() =>
{
	CleanDirectories ("./Build/");

	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

SetupXamarinBuildTasks (buildSpec, Tasks, Task);

RunTarget (TARGET);

