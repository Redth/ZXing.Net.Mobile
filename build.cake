#tool nuget:?package=XamarinComponent

#addin nuget:?package=Cake.XCode
#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.Xamarin.Build
#addin nuget:?package=Cake.SemVer
#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.MonoApiTools

var PREVIEW = "";
var VERSION = EnvironmentVariable ("APPVEYOR_BUILD_VERSION") ?? Argument("version", "2.1.9999");
var NUGET_VERSION = VERSION;

var TARGET = Argument ("t", Argument ("target", "Default"));

// Build a semver string out of the preview if it's specified
if (!string.IsNullOrEmpty (PREVIEW)) {
	var sv = ParseSemVer (VERSION);
	NUGET_VERSION = CreateSemVer (sv.Major, sv.Minor, sv.Patch, PREVIEW).ToString ();
}

var buildSpec = new BuildSpec {
	Samples = new [] {
		new DefaultSolutionBuilder { SolutionPath = "./Samples/Android/Sample.Android.sln", BuildsOn = BuildPlatforms.Windows | BuildPlatforms.Mac },
		new IOSSolutionBuilder { SolutionPath = "./Samples/iOS/Sample.iOS.sln", BuildsOn = BuildPlatforms.Mac },
		new WpSolutionBuilder { SolutionPath = "./Samples/WindowsPhone/Sample.WindowsPhone.sln", BuildsOn = BuildPlatforms.Windows },
		new DefaultSolutionBuilder { SolutionPath = "./Samples/WindowsUniversal/Sample.WindowsUniversal.sln", BuildsOn = BuildPlatforms.Windows },
		new WpSolutionBuilder { SolutionPath = "./Samples/Forms/Sample.Forms.sln", BuildsOn = BuildPlatforms.Windows },
	},

	// These should only get populated on windows where all the binaries will exist
	NuGets = new NuGetInfo [] {},
	Components = new Component [] {},
};

if (IsRunningOnWindows ()) {
	buildSpec.Libs = new [] {
		new WpSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.sln",
			BuildsOn = BuildPlatforms.Windows,
		},
		new WpSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.Forms.sln",
			BuildsOn = BuildPlatforms.Windows,
		}
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
			SolutionPath = "./ZXing.Net.Mobile.Mac.sln",
			BuildsOn = BuildPlatforms.Mac,
		},
		new DefaultSolutionBuilder {
			SolutionPath = "./ZXing.Net.Mobile.Forms.Mac.sln",
			BuildsOn = BuildPlatforms.Mac,
		}
	};
}

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

