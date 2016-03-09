#addin nuget:https://nuget.org/api/v2/?package=Cake.FileHelpers&version=1.0.3.2
#addin nuget:https://nuget.org/api/v2/?package=Cake.Xamarin&version=1.2.3

var target = Argument("target", "Default");
var version = EnvironmentVariable ("APPVEYOR_BUILD_VERSION") ?? Argument("version", "2.0.0.9999");

var libs = new Dictionary<string, string> {
 	{ "./ZXing.Net.Mobile.sln", "Any" },
 	{ "./ZXing.Net.Mobile.Forms.sln", "Any" }
};

var samples = new Dictionary<string, string> {
	{ "./Samples/Android/Sample.Android.sln", "Any" },
	{ "./Samples/iOS/Sample.iOS.sln", "Mac" },
	{ "./Samples/WindowsPhone/Sample.WindowsPhone.sln", "Win" },
	{ "./Samples/WindowsUniversal/Sample.WindowsUniversal.sln", "Win" },
	{ "./Samples/Forms/Sample.Forms.sln", "Win" },
};

// Used to build a dictionary of .sln files
var buildAction = new Action<Dictionary<string, string>> (solutions => {

	foreach (var sln in solutions) {

		// If the platform is Any build regardless
		//  If the platform is Win and we are running on windows build
		//  If the platform is Mac and we are running on Mac, build
		if ((sln.Value == "Any")
				|| (sln.Value == "Win" && IsRunningOnWindows ())
				|| (sln.Value == "Mac" && IsRunningOnUnix ())) {
			
			// Bit of a hack to use nuget3 to restore packages for project.json
			if (IsRunningOnWindows ()) {
				
				Information ("RunningOn: {0}", "Windows");

				NuGetRestore (sln.Key, new NuGetRestoreSettings {
					ToolPath = "./tools/nuget3.exe"
				});

				// Windows Phone / Universal projects require not using the amd64 msbuild
				MSBuild (sln.Key, c => { 
					c.Configuration = "Release";
					c.MSBuildPlatform = Cake.Common.Tools.MSBuild.MSBuildPlatform.x86;
				});
			} else {

				// Mac is easy ;)
				NuGetRestore (sln.Key);

				DotNetBuild (sln.Key, c => c.Configuration = "Release");
			}
		}
	}
});

Task ("libs").Does (() => 
{
	buildAction (libs);
});

Task ("samples")
	.IsDependentOn ("libs")
	.Does (() => 
{
	buildAction (samples);
});

Task ("nuget").IsDependentOn ("samples").Does (() => 
{
	// Make sure our output path is there
	if (!DirectoryExists ("./Build/nuget/"))
		CreateDirectory ("./Build/nuget");

	// Package our nuget
	NuGetPack ("./ZXing.Net.Mobile.nuspec", new NuGetPackSettings { OutputDirectory = "./Build/nuget/", Version = version });	
	NuGetPack ("./ZXing.Net.Mobile.Forms.nuspec", new NuGetPackSettings { OutputDirectory = "./Build/nuget/", Version = version });	
});

Task ("component")
	.IsDependentOn ("samples")
	.IsDependentOn ("nuget")
	.Does (() => 
{
	compVersion = version;
	if (compVersion.Contains ("-"))
		compVersion = compVersion.Substring (0, compVersion.IndexOf ("-"));

	// Clear out xml files from build (they interfere with the component packaging)
	DeleteFiles ("./Build/**/*.xml");

	// Generate component.yaml files from templates
	CopyFile ("./Component/component.template.yaml", "./Component/component.yaml");
	CopyFile ("./Component-Forms/component.template.yaml", "./Component-Forms/component.yaml");

	// Replace version in template files
	ReplaceTextInFiles ("./**/component.yaml", "{VERSION}", compVersion);

	var xamCompSettings = new XamarinComponentSettings { ToolPath = "./tools/xamarin-component.exe" };

	// Package both components
	PackageComponent ("./Component/", xamCompSettings);
	PackageComponent ("./Component-Forms/", xamCompSettings);
});

Task ("Default").IsDependentOn ("component");

Task ("clean").Does (() => 
{

	CleanDirectory ("./Component/tools/");

	CleanDirectories ("./Build/");

	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

RunTarget (target);
