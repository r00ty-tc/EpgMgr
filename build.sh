#!/bin/bash
if [ -z "$1" ]; then
        echo "Parameter version number required. Provide version (e.g. 1.0.1.1) as parameter.";
        exit 1;
fi
if [ -z "$2" ]; then
	echo "Parameter target. Provide target (e.g. win-x64) as parameter.";
	exit 1;
fi
rm -rf EpgMgr.Console/bin/Release/net8.0/$2
rm -rf DemoPlugin/bin/Release/net8.0/$2
dotnet build -c release /p:AssemblyVersion=$1 /p:Version=$1 /p:FileVersion=$1
if [ $? -ne 0 ] 
then
	exit 1
fi
dotnet publish -c release -r $2 --self-contained /p:AssemblyVersion=$1 /p:Version=$1 /p:FileVersion=$1
if [ $? -ne 0 ] 
then
	exit 1
fi
mkdir EpgMgr.Console/bin/Release/net8.0/$2/publish/Plugins
cp EpgMgr.Console/bin/Release/net8.0/Plugins/*.dll EpgMgr.Console/bin/Release/net8.0/$2/publish/Plugins/
rm -f EpgMgr-*-$2.7z
cd EpgMgr.Console/bin/Release/net8.0/$2/publish
rm -f EpgMgr.*.7z
7zz -mmt=8 -mx=9 a EpgMgr-$1-$2.7z .
cd -
mv EpgMgr.Console/bin/Release/net8.0/$2/publish/EpgMgr-$1-$2.7z .
