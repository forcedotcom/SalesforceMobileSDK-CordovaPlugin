echo off
cls
set "basepath=%2"
set "wpa=%basepath%Core.NuGet\build\wpa"
set "win=%basepath%Core.NuGet\build\win"
set "libwin=%basepath%Core.NuGet\lib\win"
set "libwpa=%basepath%Core.NuGet\lib\wpa"

if "%1" == "Release" (goto :release) else (goto :debug)

:release
set "mode=Release"
goto :copy

:debug
set "mode=Debug"
goto :copy

:copy
echo %mode%
echo %1
xcopy /s /y %basepath%Phone\bin\ARM\%mode%\Salesforce.SDK.* %wpa%\ARM
xcopy /s /y %basepath%Phone\bin\x86\%mode%\Salesforce.SDK.* %wpa%\x86
xcopy /s /y %basepath%Store\bin\ARM\%mode%\Salesforce.SDK.* %win%\ARM
xcopy /s /y %basepath%Store\bin\x86\%mode%\Salesforce.SDK.* %win%\x86
xcopy /s /y %basepath%Store\bin\x64\%mode%\Salesforce.SDK.* %win%\x64
xcopy /s /y %basepath%App\bin\ARM\%mode%\Salesforce.SDK.* %wpa%\ARM
xcopy /s /y %basepath%App\bin\x86\%mode%\Salesforce.SDK.* %wpa%\x86
xcopy /s /y %basepath%App\bin\ARM\%mode%\Salesforce.SDK.* %win%\ARM
xcopy /s /y %basepath%App\bin\x86\%mode%\Salesforce.SDK.* %win%\x86
xcopy /s /y %basepath%App\bin\x64\%mode%\Salesforce.SDK.* %win%\x64

xcopy /s /y %basepath%Phone\bin\ARM\%mode%\Salesforce.SDK.* %libwpa%\
xcopy /s /y %basepath%Store\bin\ARM\%mode%\Salesforce.SDK.* %libwin%\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %libwpa%\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %libwin%\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %libwpa%\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %libwin%\App\

xcopy /s /y %basepath%Phone\bin\ARM\%mode%\*.xbf %wpa%\ARM\Phone\
xcopy /s /y %basepath%Phone\bin\x86\%mode%\*.xbf %wpa%\x86\Phone\
xcopy /s /y %basepath%Store\bin\ARM\%mode%\*.xbf %win%\ARM\Store\
xcopy /s /y %basepath%Store\bin\x86\%mode%\*.xbf %win%\x86\Store\
xcopy /s /y %basepath%Store\bin\x86\%mode%\*.xbf %win%\x64\Store\

xcopy /s /y %basepath%Phone\bin\ARM\%mode%\*.xr.xml %wpa%\ARM\Phone\
xcopy /s /y %basepath%Phone\bin\x86\%mode%\*.xr.xml %wpa%\x86\Phone\
xcopy /s /y %basepath%Store\bin\ARM\%mode%\*.xr.xml %win%\ARM\Store\
xcopy /s /y %basepath%Store\bin\x86\%mode%\*.xr.xml %win%\x86\Store\
xcopy /s /y %basepath%Store\bin\x64\%mode%\*.xr.xml %win%\x64\Store\

xcopy /s /y %basepath%App\obj\%mode%\*.xbf %wpa%\ARM\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %wpa%\x86\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %win%\ARM\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %win%\x86\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xbf %win%\x64\App\

xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %wpa%\ARM\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %wpa%\x86\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %win%\ARM\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %win%\x86\App\
xcopy /s /y %basepath%App\obj\%mode%\*.xr.xml %win%\x64\App\