@echo off
setlocal
set XC="C:\Users\gary\.nuget\packages\microsoft.windowsappsdk\1.6.250205002\tools\net472\XamlCompiler.exe"
set IN=obj\x64\Debug\net8.0-windows10.0.22621.0\win-x64\input.json
set OUT=obj\x64\Debug\net8.0-windows10.0.22621.0\win-x64\output.json
%XC% "%IN%" "%OUT%" > xaml.out 2> xaml.err
echo exit=%ERRORLEVEL%
type xaml.out
echo ---
type xaml.err
