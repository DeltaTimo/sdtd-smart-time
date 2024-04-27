#!/bin/zsh

export WINEDEBUG="-all"
CSC="/usr/lib/mono/msbuild/Current/bin/Roslyn/csc.exe"

$CSC /out:bin/SmartTime.dll \
  /target:library \
  -r:lib/Assembly-CSharp.dll \
  -r:lib/Assembly-CSharp-firstpass.dll \
  -r:lib/LogLibrary.dll \
  -r:lib/mscorlib.dll \
  -r:lib/System.dll \
  -r:lib/System.Xml.dll \
  -r:lib/UnityEngine.dll \
  -r:lib/UnityEngine.CoreModule.dll \
  -r:lib/UnityEngine.SharedInternalsModule.dll \
  API.cs

if [ "$?" -eq 0 ] ; then
  cd bin
    [ -d "SmartTime" ] && rm -rf SmartTime
    mkdir SmartTime
    cp SmartTime.dll SmartTime/.
    cp ../ModInfo.xml SmartTime/.
    zip SmartTime.zip SmartTime/*
  cd ..
fi
