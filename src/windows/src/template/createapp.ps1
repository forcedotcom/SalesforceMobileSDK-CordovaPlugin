#!/bin/bash

#set -x

# Command line option vars
$global:OPT_APP_TYPE=""
$global:OPT_APP_NAME=""
$global:OPT_COMPANY_ID=""
$global:OPT_ORG_NAME=""
$global:OPT_APP_ID="3MVG9Iu66FKeHhINkB1l7xt7kR8czFcCTUhgoA8Ol2Ltf1eYHOU4SqQRSEitYFDUpqRWcoQ2.dBv_a1Dyu5xa"
$global:OPT_REDIRECT_URI="testsfdc:///mobilesdk/detect/oauth/done"
$global:OPT_OUTPUT_FOLDER=Split-Path -Parent $PSCommandPath

# Template substitution keys
$global:SUB_NATIVE_APP_NAME="__NativeTemplateAppName__"
$global:SUB_COMPANY_ID="__CompanyIdentifier__"
$global:SUB_ORG_NAME="__OrganizationName__"
$global:SUB_APP_ID="__ConnectedAppIdentifier__"
$global:SUB_REDIRECT_URI="__ConnectedAppRedirectUri__"

# Terminal color codes
$global:TERM_COLOR_RED="Red"
$global:TERM_COLOR_GREEN="\x1b[32;1m"
$global:TERM_COLOR_YELLOW="\x1b[33;1m"
$global:TERM_COLOR_MAGENTA="Magenta"
$global:TERM_COLOR_CYAN="Cyan"
$global:TERM_COLOR_RESET="\x1b[0m"

$version = "5.0.0"
$command = $args[0]

Function main 
{
    switch ($command)
    {
        "version" { "createapp version: " + $version }
        "create" { createApp }
        default { usage }
    }
}

Function createApp()
{
	GetInputs
    ParseOpts
    ReplaceTokens
}

Function EchoColor
{
	Param($1,$2)
	Write-Host $2 -foreground $1
}

Function Usage()
{ 
	echoColor $TERM_COLOR_CYAN "Usage:"
	echoColor $TERM_COLOR_MAGENTA "   createapp.ps1 create"
	echoColor $TERM_COLOR_CYAN "Or"
	echoColor $TERM_COLOR_MAGENTA "   createapp.ps1 version"
}

Function GetInputs()
{
	$global:t = Read-Host -Prompt 'Enter your application type (native) '
	while ($global:t -ne "native") 
	{
		echoColor $global:TERM_COLOR_RED "Invalid application type entered"
		$global:t = Read-Host -Prompt 'Enter your application type (native) '
	}

	$global:n = Read-Host -Prompt 'Enter your application name '
	while ($global:n -eq "") 
	{
		echoColor $global:TERM_COLOR_RED "Not configure a name for application yet."
		$global:t = Read-Host -Prompt 'Enter your application type (native) '
	}
	$global:g = Read-Host -Prompt 'Enter your organization name (Acme, Inc.) '

	$global:o = Read-Host -Prompt 'Enter the output directory for your app (defaults to the current directory) '
	while (!(Test-Path $global:o))
	{
		echoColor $global:TERM_COLOR_RED "The output target is not a directory, or not exist"
		$global:o = Read-Host -Prompt 'Enter the output directory for your app (defaults to the current directory) '
	}

	$global:a = Read-Host -Prompt 'Enter your Connected App ID (defaults to the sample app''s ID) '
	$global:u = Read-Host -Prompt 'Enter your Connected App Callback URI (defaults to the sample app''s URI) ' 
}

Function ParseOpts()
{
	if($global:t)
	{
		$global:OPT_APP_TYPE = $global:t
		if($global:OPT_APP_TYPE -ne "native")
		{
			echoColor $TERM_COLOR_RED "$global:OPT_APP_TYPE is not a valid application type.  Should be 'native'."
			Usage
            GetInputs
		}
	}
    else
    {
        echoColor $TERM_COLOR_RED "Enter a valid application type.  Should be 'native'."
			Usage
            GetInputs
    }
	if($global:n)
	{
		$global:OPT_APP_NAME = $global:n;
		if($global:OPT_APP_NAME -eq "")
		{
			echoColor $TERM_COLOR_RED "Application name must have a value."
			Usage
			GetInputs
		}
		#check if it has valid characters
	}
	else 
	{
		echoColor $TERM_COLOR_RED "Application name must have a value."
		Usage
		GetInputs
	}
	if($global:g)
	{
		$global:OPT_ORG_NAME = $global:g
		if($global:OPT_ORG_NAME -eq "")
		{
			echoColor $TERM_COLOR_RED "Organization name must have a value."
			Usage
			GetInputs
		}
		#check if it has valid characters
	}
	else
	{
		echoColor $TERM_COLOR_RED "Organization name must have a value."
		Usage
		GetInputs
	}
	if($global:o)
	{
		$global:OPT_OUTPUT_FOLDER = $global:o + "\" + $global:n
		#check if it has valid characters
	}
	if($global:a)
	{
		$global:OPT_APP_ID = $global:a
		#check if it has valid characters
	}
	if($global:u)
	{
		$global:OPT_REDIRECT_URI = $global:u
		#check if it has valid characters
	}
}

Function SubstitueTokensInFile()
{
	#$1--->filename
	#$2--->token to be replaced
	#$3--->replacement value
	Param($1,$2,$3)
	(Get-Content $1 | ForEach-Object { $_ -replace "$2", "$3" } ) | Set-Content $1

}

Function ReplaceTokens()
{
	#Create the output folder
	if(Test-Path $global:OPT_OUTPUT_FOLDER)
	{
		echoColor $TERM_COLOR_RED "'${OPT_OUTPUT_FOLDER}' folder already exists, it will be removed."
		Remove-Item -Path "$global:OPT_OUTPUT_FOLDER" -force -recurse:$true
	}
	
	#Create output driectory if it does not exist
    New-Item -ItemType Directory -Path $global:OPT_OUTPUT_FOLDER
	$templateAppFilesDir = Split-Path -Parent $PSCommandPath

    #Copy template files to otuput directory
	Copy-Item $templateAppFilesDir\app_template_files\* $global:OPT_OUTPUT_FOLDER -Recurse
        
    #Rename files in output directory to name of the app provided by user
	Rename-Item $global:OPT_OUTPUT_FOLDER\$global:SUB_NATIVE_APP_NAME.csproj $global:OPT_OUTPUT_FOLDER\$global:OPT_APP_NAME.csproj
    Rename-Item $global:OPT_OUTPUT_FOLDER\$global:SUB_NATIVE_APP_NAME.pfx $global:OPT_OUTPUT_FOLDER\$global:OPT_APP_NAME.pfx
		
    #Replace all tokens in files
    $fileName = "$global:OPT_OUTPUT_FOLDER\App.xaml"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME

	$fileName = "$global:OPT_OUTPUT_FOLDER\App.xaml.cs"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME

	$fileName = "$global:OPT_OUTPUT_FOLDER\Pages\MainPage.xaml"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME

	$fileName = "$global:OPT_OUTPUT_FOLDER\Pages\MainPage.xaml.cs"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME

	$fileName = "$global:OPT_OUTPUT_FOLDER\settings\Config.cs"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME
	SubstitueTokensInFile $fileName $global:SUB_APP_ID $global:OPT_APP_ID
	SubstitueTokensInFile $fileName $global:SUB_REDIRECT_URI $global:OPT_REDIRECT_URI
        
    $fileName = "$global:OPT_OUTPUT_FOLDER\Package.appxmanifest"
    SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME
    SubstitueTokensInFile $fileName $global:SUB_ORG_NAME $global:OPT_ORG_NAME
        
    $fileName = "$global:OPT_OUTPUT_FOLDER\$global:OPT_APP_NAME.csproj"
    SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME

	$fileName = "$global:OPT_OUTPUT_FOLDER\Logging\Logger.cs"
	SubstitueTokensInFile $fileName $global:SUB_NATIVE_APP_NAME $global:OPT_APP_NAME
}

main
