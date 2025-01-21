#!/bin/bash
export VER=0
export OLDVER=`gh release list -R r00ty-tc/EpgMgr|awk '/[0-9\.]/ {print $1}'`
if [ -z "$1" ]; then
	export VER=$(echo ${OLDVER} | awk -F. -v OFS=. '{$NF += 1 ; print}')
else
	export VER=$1
fi
echo "Building all targets for version $VER"
rm -f build.*.log
echo "Building Windows x64"
./build.sh $VER win-x64
if [ $? -ne 0 ]
then
	echo "win-x64 build failed"
	exit 1
fi
echo "Building Linux x64"
./build.sh $VER linux-x64
if [ $? -ne 0 ]
then
	echo "linux-x64 build failed"
	exit 1
fi
echo "Building Linux ARM"
./build.sh $VER linux-arm
if [ $? -ne 0 ]
then
	echo "linux-arm build failed"
	exit 1
fi
echo "Building Linux ARM 64"
./build.sh $VER linux-arm64
if [ $? -ne 0 ]
then
	echo "linux-arm64 build failed"
	exit 1
fi
echo "Builds complete"
echo "Creating new release"
gh release delete -R r00ty-tc/EpgMgr $OLDVER -y
gh release create -R r00ty-tc/EpgMgr $VER -p -n "$VER" EpgMgr-$VER-win-x64.7z EpgMgr-$VER-linux-x64.7z EpgMgr-$VER-linux-arm.7z EpgMgr-$VER-linux-arm64.7z
sleep 5
