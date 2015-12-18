$a = $args.length
if ($a -eq 1) {
	$source = $args[0];
} else { write-warning "You must supply the working path"; break }
cls
Set-Location $source

$SpecialFolders = @{}

$names = [Environment+SpecialFolder]::GetNames( `

  [Environment+SpecialFolder])

foreach($name in $names)
{

  if($path = [Environment]::GetFolderPath($name)){

    $SpecialFolders[$name] = $path
  }

}


write-output "Using $source for source path"
$shell = new-object -com shell.application
$paths = @("Universal.Template\")
$files = @("BlankApp.png", "SharedBlankSalesforceApplication.vstemplate")


$templateBuildPath = $source + "TemplateTemp"
$templateBuild = $shell.Namespace($templateBuildPath)
if ($templateBuild -eq $null)
{
	New-Item -ItemType directory -Path $templateBuildPath
        $templateBuild = $shell.Namespace($templateBuildPath)
} else
{
	remove-item $templateBuildPath -Force -Recurse
	New-Item -ItemType directory -Path $templateBuildPath
	$templateBuild = $shell.Namespace($templateBuildPath)
}
$templateOutput = $SpecialFolders["Personal"] + "\Visual Studio 2013\Templates\ProjectTemplates\SalesforceUniversalApplicationTemplate.zip"
$FOF_SILENT_FLAG = 4
$FOF_NOCONFIRMATION_FLAG = 16
$pLen = $paths.length
$fLen = $files.length

write-output "$pLen paths to copy"
foreach ($item in $paths)
{
	write-output "copying $source$item to $templateBuild.Title"
	$shell.Namespace($templateBuild).copyhere($source + $item, $FOF_SILENT_FLAG + $FOF_NOCONFIRMATION_FLAG)
}

write-output "$fLen files to copy"
foreach ($item in $files)
{
	write-output "copying $source$item to $templateBuild"
	$shell.Namespace($templateBuild).copyhere($source + $item, $FOF_SILENT_FLAG + $FOF_NOCONFIRMATION_FLAG)
}


remove-item $templateOutput -Force -Recurse

[Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
[System.IO.Compression.ZipFile]::CreateFromDirectory($templateBuildPath,$templateOutput,$compressionLevel, $false)

remove-item $templateBuildPath -Force -Recurse