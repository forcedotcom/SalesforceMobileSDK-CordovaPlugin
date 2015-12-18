# Runs every time a package is installed in a project

param($installPath, $toolsPath, $package, $project)

# $installPath is the path to the folder where the package is installed.
# $toolsPath is the path to the tools directory in the folder where the package is installed.
# $package is a reference to the package object.
# $project is a reference to the project the package was installed to.

$SpecialFolders = @{}
$names = [Environment+SpecialFolder]::GetNames( `

  [Environment+SpecialFolder])

foreach($name in $names)
{

  if($path = [Environment]::GetFolderPath($name)){

    $SpecialFolders[$name] = $path
  }

}

[System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
$publisher = New-Object System.EnterpriseServices.Internal.Publish

get-childitem | where {$_.Name -eq "TemplateWizard"} | Foreach-Object {$publisher.GacInstall($_)}



$templateOutput = $SpecialFolders["Personal"] + "\Visual Studio 2013\Templates\ProjectTemplates"
$templateSource = $toolsPath + "\SalesforceUniversalApplicationTemplate.zip";

write-output $templateSource
write-output $templateOutput

Copy-Item -Force $templateSource $templateOutput