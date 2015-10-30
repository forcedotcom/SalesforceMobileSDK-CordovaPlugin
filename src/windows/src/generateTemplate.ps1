$a = $args.length
if ($a -eq 3) {
	$vsTempName = $args[2];
	$strProject = $args[1];
	$workingDir = $args[0];
} else { write-warning "You must supply a solution working path, project name and vstemplate name."; break }
cls
$FOF_SILENT_FLAG = 4
$FOF_NOCONFIRMATION_FLAG = 16
write-output "Using $workingDir as working path"
write-output "Using $strProject as the target project"
write-output "Using $vsTempName.vstemplate as the name for vstemplate";
Set-Location $workingDir
$SpecialFolders = @{}

$names = [Environment+SpecialFolder]::GetNames( `

  [Environment+SpecialFolder])

foreach($name in $names)

{

  if($path = [Environment]::GetFolderPath($name)){

    $SpecialFolders[$name] = $path
  }

}
$shell = new-object -com shell.application
$exportedBasePath = $SpecialFolders["Personal"] + "\My Exported Templates\" + $strProject + ".zip"
$destinationPath = $shell.NameSpace($workingDir + $strProject)
.\vspte -s SalesforceSDK.sln -p $strProject | out-null
write-output "Extracting MyTemplate.vstemplate from $exportedBasePath"
$zip = $shell.NameSpace($exportedBasePath)
foreach($item in $zip.items())
{
if ($item.Name.equals("MyTemplate.vstemplate"))
	{ 
		$shell.Namespace($destinationPath).copyhere($item, $FOF_SILENT_FLAG + $FOF_NOCONFIRMATION_FLAG);
		$outputPath =$workingDir + $strProject;
		write-output $outputPath;
		$templateName = $outputPath + "\MyTemplate.vstemplate";
		$finalName = $outputPath + "\" + $vsTempName + ".vstemplate";
		write-output "Generating vstemplate file $finalName"
		remove-item $finalName
		rename-item -path $templateName -newname $finalName -Force
	 	break
	}
}