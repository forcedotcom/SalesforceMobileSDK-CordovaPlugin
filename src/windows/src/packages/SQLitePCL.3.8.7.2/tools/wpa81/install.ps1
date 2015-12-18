param($installPath, $toolsPath, $package, $project)

$sqliteReference = $project.Object.References.AddSDK("SQLite for Windows Phone 8.1", "SQLite.WP81, version=3.8.7.2")

Write-Host "Successfully added a reference to the extension SDK SQLite for Windows Phone 8.1."
Write-Host "Please, verify that the extension SDK SQLite for Windows Phone 8.1 v3.8.7.2, from the SQLite.org site (http://www.sqlite.org/2014/sqlite-wp81-winrt-3080702.vsix), has been properly installed."
