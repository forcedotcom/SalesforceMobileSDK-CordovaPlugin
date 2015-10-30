param($installPath, $toolsPath, $package, $project)

$sqliteReference = $project.Object.References.AddSDK("SQLite for Windows Runtime", "SQLite.WinRT, version=3.8.7.2")

Write-Host "Successfully added a reference to the extension SDK SQLite for Windows Runtime."
Write-Host "Please, verify that the extension SDK SQLite for Windows Runtime v3.8.7.2, from the SQLite.org site (http://www.sqlite.org/2014/sqlite-winrt-3080702.vsix), has been properly installed."
