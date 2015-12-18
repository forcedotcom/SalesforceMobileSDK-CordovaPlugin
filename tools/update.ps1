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
$WINDOWS_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Windows.git"
$WINDOWS_SDK_FOLDER="SalesforceMobileSDK-Windows"

parse_opts "$@"

# Work from the root of the repo.
cd $ROOT_FOLDER

update_repo $WINDOWS_SDK_FOLDER $WINDOWS_SDK_REPO_PATH

cd $ROOT_FOLDER

echo "*** Creating directories ***"
echo "Starting clean"
rm src -r -force

echo "Creating Windows directories"
mkdir src/windows -Force

echo "*** Windows ***"
echo "Copying windows files from $WINDOWS_SDK_FOLDER/CordovaPluginJavascript/*.js to src/windows/" 
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/CordovaPluginJavascript/*.js src/windows/ -Force

echo "Copying windows source files to src/windows/src"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/ src/windows/src/ -Recurse -Force
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/packages src/windows/src/ -Recurse -Force
rm -r src/windows/src/TypeScriptLib

echo "Copying Core from $WINDOWS_SDK_FOLDER/DLLs to src/windows/src/Core/bin"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Core src/windows/src/Core/bin -Recurse -Force

echo "Copying Hybrid.winmd from $WINDOWS_SDK_FOLDER/DLLs to src/windows/src/Hybrid/bin"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Hybrid src/windows/src/Hybrid/bin -Recurse -Force

echo "Copying Universal from $WINDOWS_SDK_FOLDER/DLLs to src/windows/src/Core/bin"
cp $WINDOWS_SDK_FOLDER/SalesforceSDK/DLLs/Universal src/windows/src/Universal/bin -Recurse -Force

echo "--- Clean Up ---"
echo "Removing SalesforceSDK Library"
rm -Recurse -Force $WINDOWS_SDK_FOLDER


