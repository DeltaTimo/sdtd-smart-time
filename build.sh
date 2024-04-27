#!/bin/zsh

MOD_NAME="SmartTime"
export WINEDEBUG="-all"
CSC="/usr/lib/mono/msbuild/Current/bin/Roslyn/csc.exe"

$CSC /out:bin/${MOD_NAME}.dll \
  /target:library \
  -r:lib/Assembly-CSharp.dll \
  -r:lib/Assembly-CSharp-firstpass.dll \
  -r:lib/LogLibrary.dll \
  -r:lib/mscorlib.dll \
  -r:lib/System.dll \
  -r:lib/System.Xml.dll \
  -r:lib/UnityEngine.dll \
  -r:lib/UnityEngine.CoreModule.dll \
  API.cs

if [ "$?" -eq 0 ] ; then
  cd bin
    [ -d "${MOD_NAME}" ] && rm -rf ${MOD_NAME}
    mkdir ${MOD_NAME}
    cp ${MOD_NAME}.dll ${MOD_NAME}/.
    cp ../ModInfo.xml ${MOD_NAME}/.
    zip ${MOD_NAME}.zip ${MOD_NAME}/*
  cd ..
fi
