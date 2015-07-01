#!/bin/bash

#set -x
param(
  [string]$b,
  [string]$n
)

$global:OPT_BUILD="no" # default 'no' since unlikely building iOS on a Windows machine.
$global:OPT_BRANCH=""



function usage 
{
    echo "usage: $0 -b <branch name> [-n]"
    echo "  Where <branch name> is the branch of each SDK to build."
}

function parse_opts
{
    if ($n)
    {
        $global:OPT_BUILD="yes"
    }

    if ($b)
    {
        $global:OPT_BRANCH = $b
    }
    if ( $global:OPT_BRANCH -eq "" )
    {
        echo "You must specify a value for the branch."
        usage
        exit 1
    }

    $valid_branch_regex='^[a-zA-Z0-9_][a-zA-Z0-9_]*(/[a-zA-Z0-9_][a-zA-Z0-9_]*)?$'
    if ( $global:OPT_BRANCH -match $valid_branch_regex )
    {}
    else
    {
        echo "$global:OPT_BRANCH is not a valid branch name.  Should be in the format <[remote/]branch name>"
        exit 2
    }
}

# Helper functions
function copy_and_fix([string] $src, [string] $dest)
{
    echo "* Fixing and copying $1 to $2 directory"
    find tmp -name $src | xargs sed 's/\#import\ \<Salesforce.*\/\(.*\)\>/#import "\1"/' > src/ios/$2/$1
}

function copy_lib
{
    echo "* Copying $1"G
    find tmp -name $1 -exec cp {} src/ios/frameworks/ \;
}

function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

function get_root_folder
{
    $current_folder=Get-ScriptDirectory
    $parent_folder=Split-Path -parent $current_folder
    echo "$parent_folder"
}

function update_branch
{
    $trimmed_branch_name=$global:OPT_BRANCH -replace "/", ""
    if ( $trimmed_branch_name -eq $global:OPT_BRANCH )
    {
        # Not a remote branch, so update the local one.#
        Write-Host "Updating $global:OPT_BRANCH from origin."
        git merge origin/$global:OPT_BRANCH
    }
    git submodule init
    git submodule sync
    git submodule update --init --recursive
}

function update_repo([string]$repo_dir, [string] $git_repo_url)
{
    if ( !(Test-Path $repo_dir) )
    {
        echo "Cloning $git_repo_url into $repo_dir"
        git clone $git_repo_url $repo_dir
        cd $repo_dir
     }
    else
    {
        echo "Found repo at $repo_dir.  Fetching the latest"
        cd $repo_dir
        git fetch origin
    }

    echo "Checking out the $global:OPT_BRANCH branch."
    git checkout $global:OPT_BRANCH
    update_branch
    cd $ROOT_FOLDER
}

$ROOT_FOLDER=$(get_root_folder)
$ANDROID_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Android.git"
$ANDROID_SDK_FOLDER="SalesforceMobileSDK-Android"
$SHARED_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Shared.git"
$SHARED_SDK_FOLDER="SalesforceMobileSDK-Shared"
$WINDOWS_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Windows.git"
$WINDOWS_SDK_FOLDER="SalesforceMobileSDK-Windows"

parse_opts "$@"

# Work from the root of the repo.
cd $ROOT_FOLDER

update_repo $ANDROID_SDK_FOLDER $ANDROID_SDK_REPO_PATH
update_repo $WINDOWS_SDK_FOLDER $WINDOWS_SDK_REPO_PATH
update_repo $SHARED_SDK_FOLDER $SHARED_SDK_REPO_PATH

cd $ROOT_FOLDER

echo "*** Creating directories ***"
echo "Starting clean"
rm src -r -force
echo "Creating android directories"
mkdir src/android/libs -Force
mkdir src/android/assets -Force
echo "Creating Windows directories"
mkdir src/windows -Force
mkdir src/windows/WinMD -Force
mkdir src/windows/WinMD/Store -Force
mkdir src/windows/WinMD/Store/x86 -Force
mkdir src/windows/WinMD/Store/x64 -Force
mkdir src/windows/WinMD/Store/ARM -Force
mkdir src/windows/WinMD/Phone -Force
mkdir src/windows/WinMD/Phone/ARM -Force
mkdir src/windows/WinMD/Phone/x86 -Force

echo "*** Android ***"
echo "Copying SalesforceSDK library"
cp $ANDROID_SDK_FOLDER/libs/SalesforceSDK src/android/libs/ -Recurse -Force
echo "Copying SmartStore library"
cp $ANDROID_SDK_FOLDER/libs/SmartStore src/android/libs/ -Recurse -Force
echo "Copying SmartSync library"
cp $ANDROID_SDK_FOLDER/libs/SmartSync src/android/libs/ -Recurse -Force
echo "Copying icu461.zip"
cp $ANDROID_SDK_FOLDER/external/sqlcipher/assets/icudt46l.zip src/android/assets/ -Recurse -Force
echo "Copying sqlcipher"
cp $ANDROID_SDK_FOLDER/external/sqlcipher/libs/* src/android/libs/SmartStore/libs/ -Recurse -Force  

echo "*** Windows ***"
echo "Copying windows files from $WINDOWS_SDK_FOLDER/CordovaPluginJavascript/*.js to src/windows/" 
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/CordovaPluginJavascript/*.js src/windows/ -Force
echo "Copying windows files for NewtonSoft.Json from $WINDOWS_SDK_FOLDER/DLLs to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/NewtonSoft.Json.dll src/windows/WinMD
echo "Copying windows files for Sqlite.Ext from $WINDOWS_SDK_FOLDER/DLLs to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/SQLitePCL.Ext.dll src/windows/WinMD
echo "Copying windows files for SDK.Core from $WINDOWS_SDK_FOLDER/DLLs/ to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Salesforce.SDK.Core.dll src/windows/WinMD
echo "Copying windows files for Hybrid WinMD from $WINDOWS_SDK_FOLDER/DLLs/ to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Salesforce.SDK.Hybrid.winmd src/windows/WinMD
echo "Copying windows files for Hybrid SmartStore from $WINDOWS_SDK_FOLDER/DLLs/ to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Salesforce.SDK.Hybrid.SmartStore.winmd src/windows/WinMD
echo "Copying windows files for Hybrid SmartSync from $WINDOWS_SDK_FOLDER/DLLs/ to src/windows/WinMD"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Salesforce.SDK.Hybrid.SmartSync.winmd src/windows/WinMD
echo "Copying windows files for sqlite from $WINDOWS_SDK_FOLDER/DLLs/Store to src/windows/WinMD/Store"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x86/sqlite3.dll src/windows/WinMD/Store/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x64/sqlite3.dll src/windows/WinMD/Store/x64
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/ARM/sqlite3.dll src/windows/WinMD/Store/ARM
echo "Copying windows files for sqlite from $WINDOWS_SDK_FOLDER/DLLs/Phone to src/windows/WinMD/Phone"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/x86/sqlite3.dll src/windows/WinMD/Phone/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/ARM/sqlite3.dll src/windows/WinMD/Phone/ARM
echo "Copying windows files for SmartStore from $WINDOWS_SDK_FOLDER/DLLs/Store to src/windows/WinMD/Store"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x86/Salesforce.SDK.SmartStore.dll src/windows/WinMD/Store/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x64/Salesforce.SDK.SmartStore.dll src/windows/WinMD/Store/x64
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/ARM/Salesforce.SDK.SmartStore.dll src/windows/WinMD/Store/ARM
echo "Copying windows files for SmartStore from $WINDOWS_SDK_FOLDER/DLLs/Phone to src/windows/WinMD/Phone"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/x86/Salesforce.SDK.SmartStore.dll src/windows/WinMD/Phone/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/ARM/Salesforce.SDK.SmartStore.dll src/windows/WinMD/Phone/ARM
echo "Copying windows files for SmartSync from $WINDOWS_SDK_FOLDER/DLLs/Store to src/windows/WinMD/Store"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x86/Salesforce.SDK.SmartSync.dll src/windows/WinMD/Store/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/x64/Salesforce.SDK.SmartSync.dll src/windows/WinMD/Store/x64
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Store/ARM/Salesforce.SDK.SmartSync.dll src/windows/WinMD/Store/ARM
echo "Copying windows files for SmartSync from $WINDOWS_SDK_FOLDER/DLLs/Phone to src/windows/WinMD/Phone"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/x86/Salesforce.SDK.SmartSync.dll src/windows/WinMD/Phone/x86
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Phone/ARM/Salesforce.SDK.SmartSync.dll src/windows/WinMD/Phone/ARM
echo "Copying windows source files to src/windows/src"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/ src/windows/src/ -Recurse -Force

echo "--- Shared ---"
echo "Copying split cordova.force.js out of bower_components"
cp $SHARED_SDK_FOLDER/gen/plugins/com.salesforce/*.js www/

echo "--- Clean Up ---"
echo "Removing SalesforceSDK Library"


