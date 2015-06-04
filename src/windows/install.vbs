Option Explicit
On Error Resume Next

' ***                                                                      ***
' *                                                                          *
' * ATTENTION: Windows Users                                                 *
' *                                                                          *
' * Run this script to initialize the submodules of your repository
' *    From the command line: cscript install.vbs                            *
' ***                                                                      ***

Dim strWorkingDirectory, objShell, intReturnVal

' Set the working folder to the script location (i.e. the repo root)
strWorkingDirectory = GetDirectoryName(WScript.ScriptFullName)
Set objShell = WScript.CreateObject("WScript.Shell")
objShell.CurrentDirectory = strWorkingDirectory

' Initialze and update the submodules.
intReturnVal = objShell.Run("git submodule init", 1, True)
If intReturnVal <> 0 Then
    WScript.Echo "Error initializing the submodules!"
    WScript.Quit 2
End If
intReturnVal = objShell.Run("git submodule sync", 1, True)
If intReturnVal <> 0 Then
    WScript.Echo "Error syncing the submodules!"
    WScript.Quit 6
End If
intReturnVal = objShell.Run("git submodule update", 1, True)
If intReturnVal <> 0 Then
    WScript.Echo "Error updating the submodules!"
    WScript.Quit 3
End If

WScript.Echo vbCrLf & "Successfully initialized submodules."
WScript.Quit 0


' -------------------------------------------------------------------
' - Gets the directory name, from a file path.
' -------------------------------------------------------------------
Function GetDirectoryName(ByVal strFilePath)
    Dim strFinalSlash
    strFinalSlash = InStrRev(strFilePath, "\")
    If strFinalSlash = 0 Then
        GetDirectoryName = strFilePath
    Else
        GetDirectoryName = Left(strFilePath, strFinalSlash)
    End If
End Function
