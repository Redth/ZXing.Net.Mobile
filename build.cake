var target = Argument("target", "libs");
var version = Argument("nugetversion", "");

List<string> GetSamples ()
{
	var l = new List<string> ();
	l.Add ("Samples/Android/Sample.Android.sln");
	l.Add ("Samples/iOS/Sample.iOS.sln");
	l.Add ("Samples/iOS/Sample.iOS-Classic.sln");
	
	if (IsRunningOnWindows ()) {
		l.Add ("Samples/WindowsPhone8/Sample.WindowsPhone8.sln");
	}
	
	return l;
}

var slns = GetSamples ();

Task ("libs").Does (() => 
{
	// Build the PCL
	NuGetRestore ("ZXing.Net.Mobile.sln");
	DotNetBuild ("src/ZXing.Net.Mobile.Portable/ZXing.Net.Mobile.Portable.csproj", c => c.Configuration = "Release");

	foreach (var s in slns) {

		// Build each project
		NuGetRestore (s);

		if (s.Contains ("WindowsPhone8")) {
			MSBuild (s, c => {
					c.Configuration = "Release";
					c.PlatformTarget = Cake.Common.Tools.MSBuild.PlatformTarget.x86;
			});
		} else {
			DotNetBuild (s, c => c.Configuration = "Release");
		}
	}
});


Task ("tools").WithCriteria (!FileExists ("./Component/tools/xamarin-component.exe")).Does (() => 
{
	if (!DirectoryExists ("./Component/tools/"))
		CreateDirectory ("./Component/tools/");

	DownloadFile ("https://components.xamarin.com/submit/xpkg", "./Component/tools/tools.zip");

	Unzip ("./Component/tools/tools.zip", "./Component/tools/");

	DeleteFile ("./Component/tools/tools.zip");
});

Task ("component").IsDependentOn ("libs").IsDependentOn ("tools").Does (() => 
{
	DeleteFiles ("./Build/**/*.xml");
	
	if (IsRunningOnWindows ())
		StartProcess ("./Component/tools/xamarin-component.exe", new ProcessSettings { Arguments = "package ./" });
	else
		StartProcess ("mono", new ProcessSettings { Arguments = "./Component/tools/xamarin-component.exe package ./" });
});

Task ("nuget").IsDependentOn ("libs").Does (() => 
{
	if (!DirectoryExists ("./NuGet/"))
		CreateDirectory ("./NuGet");

	NuGetPack ("./ZXing.Net.Mobile.nuspec", new NuGetPackSettings { OutputDirectory = "./NuGet/" });	
});

Task ("release").IsDependentOn ("nuget").IsDependentOn ("component");

Task ("publish").IsDependentOn ("nuget").IsDependentOn ("component")
	.Does (() => 
{
	if (string.IsNullOrEmpty (version)) {
		Information ("No version specified, not publishing anything.");		
		return;
	}

	var apiKey = TransformTextFile("./nuget_api_key.txt").ToString ().Trim ();

	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile." + version + ".nupkg " + apiKey });
});

Task ("stage").IsDependentOn ("nuget").Does (() => 
{
	if (string.IsNullOrEmpty (version)) {
		Information ("No version specified, not publishing anything.");		
		return;
	}

	var apiKey = TransformTextFile("./myget_api_key.txt").ToString ().Trim ();

	StartProcess ("nuget", new ProcessSettings { Arguments = "push ./NuGet/ZXing.Net.Mobile." + version + ".nupkg -Source https://www.myget.org/F/redth/api/v2 " + apiKey });
});

Task ("clean").Does (() => 
{

	CleanDirectory ("./Component/tools/");

	CleanDirectories ("./Build/");

	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

RunTarget (target);
