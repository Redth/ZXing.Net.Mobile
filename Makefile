MONOXBUILD=/Library/Frameworks/Mono.framework/Commands/xbuild
MDTOOL=/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool

all: compileAll

android: compileAndroid

ios: compileiOS

compileAndroid:
	$(MONOXBUILD) /p:Configuration=Release src/ZXing.Net.Mobile.MonoForAndroid.sln
	
compileiOS:
	$(MDTOOL) -v build -t:Build "-c:Release|iPhone" src/ZXing.Net.Mobile.MonoTouch.sln
	
compileAll: compileAndroid compileiOS

clean:
	-rm -rf Build/Release/
