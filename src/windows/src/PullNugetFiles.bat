echo off
cls
set "basepath=%2"

set "win=%basepath%Core.NuGet\build\win"
set "libwin=%basepath%Core.NuGet\lib\win"

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
xcopy /s /y %basepath%Core\bin\%mode%\Salesforce.SDK.* %win%\
xcopy /s /y %basepath%Core\bin\x86\%mode%\Salesforce.SDK.* %win%\x86\

xcopy /s /y %basepath%Core\bin\%mode%\Salesforce.SDK.* %libwin%\
xcopy /s /y %basepath%Core\bin\x86\%mode%\Salesforce.SDK.* %libwin%\x86\

xcopy /s /y %basepath%Universal\bin\x64\%mode%\Salesforce.SDK.* %win%\x64\
xcopy /s /y %basepath%Universal\bin\x86\%mode%\Salesforce.SDK.* %win%\x86\

xcopy /s /y %basepath%Universal\bin\x64\%mode%\Salesforce.SDK.* %libwin%\x64\
xcopy /s /y %basepath%Universal\bin\x86\%mode%\Salesforce.SDK.* %libwin%\x86\

xcopy /s /y %basepath%Universal\obj\%mode%\*.xr.xml %libwin%\Universal\
xcopy /s /y %basepath%Universal\obj\%mode%\*.xbf %libwin%\Universal\