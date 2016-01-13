var target = Argument("target", "libs");
var version = Argument("nugetversion", Argument("version", "2.0.0.9999"));

var libs = new Dictionary<string, string> {
	{ "./ZXing.Net.Mobile.sln", "Any" },
	{ "./ZXing.Net.Mobile.Forms.sln", "Any" }
};

var samples = new Dictionary<string, string> {
	{ "./Samples/Android/Sample.Android.sln", "Any" },
	{ "./Samples/iOS/Sample.iOS.sln", "Any" },
	{ "./Samples/WindowsPhone/Sample.WindowsPhone.sln", "Win" },
	{ "./Samples/WindowsUniversal/Sample.WindowsUniversal.sln", "Win" },
	{ "./Samples/Forms/Sample.Forms.sln", "Win" },
};

var buildAction = new Action<Dictionary<string, string>> (solutions => {

	foreach (var sln in solutions) {

		if ((sln.Value == "Any")
				|| (sln.Value == "Win" && IsRunningOnWindows ())
				|| (sln.Value == "Mac" && IsRunningOnUnix ())) {
			
			if (IsRunningOnWindows ()) {
				NuGetRestore (sln.Key, new NuGetRestoreSettings {
					ToolPath = "./tools/nuget3.exe"
				});

				MSBuild (sln.Key, c => { 
					c.Configuration = "Release";
					c.MSBuildPlatform = MSBuildPlatform.x86;
				});
			} else {
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


Task ("samples").Does (() => 
{
	buildAction (samples);
});


Task ("component")
	.IsDependentOn ("nuget")
	.Does (() => 
{
	DeleteFiles ("./Build/**/*.xml");

	// Generate component.yaml files from templates
	CopyFile ("./Component/component.template.yaml", "./Component/component.yaml");
	CopyFile ("./Component-Forms/component.template.yaml", "./Component-Forms/component.yaml");

	// Replace version in template files
	FileWriteText ("./Component/component.yaml",
		TransformTextFile("./Component/component.yaml", "{", "}")
   			.WithToken("VERSION", version)
   			.ToString());
	FileWriteText ("./Component-Forms/component.yaml",
		TransformTextFile("./Component-Forms/component.yaml", "{", "}")
   			.WithToken("VERSION", version)
   			.ToString());

	StartProcess ("./tools/xamarin-component.exe", "package ./Component/");
	StartProcess ("./tools/xamarin-component.exe", "package ./Component-Forms/");	
});

Task ("nuget").IsDependentOn ("libs").Does (() => 
{
	if (!DirectoryExists ("./Build/nuget/"))
		CreateDirectory ("./Build/nuget");

	NuGetPack ("./ZXing.Net.Mobile.nuspec", new NuGetPackSettings { OutputDirectory = "./Build/nuget/", Version = version });	
	NuGetPack ("./ZXing.Net.Mobile.Forms.nuspec", new NuGetPackSettings { OutputDirectory = "./Build/nuget/", Version = version });	
});

Task ("release").IsDependentOn ("nuget").IsDependentOn ("component");
Task ("Default").IsDependentOn ("release");

Task ("publish").IsDependentOn ("nuget").IsDependentOn ("component")
	.Does (() => 
{
	if (string.IsNullOrEmpty (version)) {
		Information ("No version specified, not publishing anything.");		
		return;
	}

	var apiKey = TransformTextFile("./.nugetapikey").ToString ().Trim ();

	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile." + version + ".nupkg " + apiKey });
	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile.Forms." + version + ".nupkg " + apiKey });
});

Task ("stage").IsDependentOn ("nuget").Does (() => 
{
	if (string.IsNullOrEmpty (version)) {
		Information ("No version specified, not publishing anything.");		
		return;
	}

	var apiKey = TransformTextFile("./.mygetapikey").ToString ().Trim ();

	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile." + version + ".nupkg -Source https://www.myget.org/F/redth/api/v2 " + apiKey });
	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile.Forms." + version + ".nupkg -Source https://www.myget.org/F/redth/api/v2 " + apiKey });
});

Task ("clean").Does (() => 
{

	CleanDirectory ("./Component/tools/");

	CleanDirectories ("./Build/");

	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

RunTarget (target);
