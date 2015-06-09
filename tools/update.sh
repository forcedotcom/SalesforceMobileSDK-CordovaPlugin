#!/bin/bash

#set -x

OPT_BUILD="yes"
OPT_BRANCH=""

usage ()
{
    echo "usage: $0 -b <branch name> [-n]"
    echo "  Where <branch name> is the branch of each SDK to build."
    echo "  -n specifies that the iOS SDK should not be rebuilt."
}

parse_opts ()
{
    while getopts :b:n command_line_opt
    do
        case ${command_line_opt} in
            b)
                OPT_BRANCH=${OPTARG};;
            n)
                OPT_BUILD="no";;
            ?)
                echo "Unknown option '-${OPTARG}'."
                usage
                exit 1;;
        esac
    done

    if [ "${OPT_BRANCH}" == "" ]
    then
        echo "You must specify a value for the branch."
        usage
        exit 1
    fi

    valid_branch_regex='^[a-zA-Z0-9_][a-zA-Z0-9_]*(/[a-zA-Z0-9_][a-zA-Z0-9_]*)?$'
    if [[ "${OPT_BRANCH}" =~ $valid_branch_regex ]]
   	 then
   	     # No action
    	    :
   	 else
    	    echo "${OPT_BRANCH} is not a valid branch name.  Should be in the format <[remote/]branch name>"
      	  exit 2
    	fi
}

# Helper functions
copy_and_fix ()
{
    echo "* Fixing and copying $1 to $2 directory"
    find tmp -name $1 | xargs sed 's/\#import\ \<Salesforce.*\/\(.*\)\>/#import "\1"/' > src/ios/$2/$1
}

copy_lib ()
{
    echo "* Copying $1"
    find tmp -name $1 -exec cp {} src/ios/frameworks/ \;
}

get_root_folder ()
{
    local current_folder=`dirname "${BASH_SOURCE[0]}"`
    local parent_folder=`cd $current_folder && cd .. && pwd`
    echo "${parent_folder}"
}

update_branch ()
{
    trimmed_branch_name=`echo "${OPT_BRANCH}" | sed 's/\///g'`
    if [ "${trimmed_branch_name}" == "${OPT_BRANCH}" ]
    then
        # Not a remote branch, so update the local one.
        echo "Updating ${OPT_BRANCH} from origin."
        git merge origin/${OPT_BRANCH}
    fi
    git submodule init
    git submodule sync
    git submodule update --init --recursive
}

update_repo ()
{
    local repo_dir=$1
    local git_repo_url=$2

    if [ ! -d "$repo_dir" ]
    then
        echo "Cloning $git_repo_url into $repo_dir"
        git clone ${git_repo_url} ${repo_dir}
        cd ${repo_dir}
    else
        echo "Found repo at $repo_dir.  Fetching the latest"
        cd ${repo_dir}
        git fetch origin
    fi

    echo "Checking out the ${OPT_BRANCH} branch."
    git checkout ${OPT_BRANCH}
    update_branch
    cd ${ROOT_FOLDER}
}

ROOT_FOLDER=$(get_root_folder)
ANDROID_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Android.git"
ANDROID_SDK_FOLDER="SalesforceMobileSDK-Android"
IOS_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-iOS.git"
IOS_SDK_FOLDER="SalesforceMobileSDK-iOS"
SHARED_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Shared.git"
SHARED_SDK_FOLDER="SalesforceMobileSDK-Shared"
WINDOWS_SDK_REPO_PATH="https://github.com/forcedotcom/SalesforceMobileSDK-Windows.git"
WINDOWS_SDK_FOLDER="SalesforceMobileSDK-Windows"

parse_opts "$@"

# Work from the root of the repo.
cd ${ROOT_FOLDER}

update_repo "${IOS_SDK_FOLDER}" "${IOS_SDK_REPO_PATH}"
update_repo "${ANDROID_SDK_FOLDER}" "${ANDROID_SDK_REPO_PATH}"
update_repo "${WINDOWS_SDK_FOLDER}" "${WINDOWS_SDK_REPO_PATH}"
update_repo "${SHARED_SDK_FOLDER}" "${SHARED_SDK_REPO_PATH}"

if [ "$OPT_BUILD" == "yes" ]
then
    echo "Building the iOS SDK"
    cd ${IOS_SDK_FOLDER}
    ./install.sh
    cd build
    rm -rf artifacts
    ant
fi

cd ${ROOT_FOLDER}

echo "*** Creating directories ***"
echo "Starting clean"
rm -rf tmp src
echo "Creating tmp directory"
mkdir -p tmp
echo "Creating android directories"
mkdir -p src/android/libs
mkdir -p src/android/assets
echo "Creating ios directories"
mkdir -p src/ios/headers
mkdir -p src/ios/frameworks
mkdir -p src/ios/classes
mkdir -p src/ios/resources
mkdir -p src/windows/$WINDOWS_SDK_FOLDER

echo "*** Android ***"
echo "Copying SalesforceSDK library"
cp -RL $ANDROID_SDK_FOLDER/libs/SalesforceSDK src/android/libs/
echo "Copying SmartStore library"
cp -RL $ANDROID_SDK_FOLDER/libs/SmartStore src/android/libs/
echo "Copying SmartSync library"
cp -RL $ANDROID_SDK_FOLDER/libs/SmartSync src/android/libs/
echo "Copying icu461.zip"
cp $ANDROID_SDK_FOLDER/external/sqlcipher/assets/icudt46l.zip src/android/assets/
echo "Copying sqlcipher"
cp -RL $ANDROID_SDK_FOLDER/external/sqlcipher/libs/* src/android/libs/SmartStore/libs/    

echo "*** iOS ***"
echo "Copying SalesforceHybridSDK library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceHybridSDK-Debug.zip -d tmp
echo "Copying SalesforceOAuth library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceOAuth-Debug.zip -d tmp
echo "Copying SalesforceSDKCore library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceSDKCore-Debug.zip -d tmp
echo "Copying SalesforceSecurity library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceSecurity-Debug.zip -d tmp
echo "Copying SalesforceNetwork library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceNetwork-Debug.zip -d tmp
echo "Copying SalesforceRestAPI library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceRestAPI-Debug.zip -d tmp
echo "Copying SmartSync library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SmartSync-Debug.zip -d tmp
echo "Copying SalesforceSDKCommon library"    
unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceSDKCommon-Debug.zip -d tmp
echo "Copying SalesforceCommonUtils library"    
cp -RL $IOS_SDK_FOLDER/external/ThirdPartyDependencies/SalesforceCommonUtils  tmp
echo "Copying sqlcipher library"    
cp -RL $IOS_SDK_FOLDER/external/ThirdPartyDependencies/sqlcipher  tmp
echo "Copying AppDelegate+SalesforceHybridSDK"    
cp $IOS_SDK_FOLDER/shared/hybrid/AppDelegate+SalesforceHybridSDK.*  tmp
cp $IOS_SDK_FOLDER/shared/hybrid/UIApplication+SalesforceHybridSDK.*  tmp
cp $IOS_SDK_FOLDER/shared/hybrid/InitialViewController.*  tmp
echo "Copying and fixing needed headers to src/ios/headers"
copy_and_fix AppDelegate+SalesforceHybridSDK.h headers
copy_and_fix UIApplication+SalesforceHybridSDK.h headers
copy_and_fix InitialViewController.h headers
copy_and_fix SFSDKAppConfig.h headers
copy_and_fix SFAuthenticationManager.h headers
copy_and_fix SFCommunityData.h headers
copy_and_fix SFDefaultUserManagementViewController.h headers
copy_and_fix SFHybridViewConfig.h headers
copy_and_fix SFHybridViewController.h headers
copy_and_fix SFIdentityCoordinator.h headers
copy_and_fix SFIdentityData.h headers
copy_and_fix SFLocalhostSubstitutionCache.h headers
copy_and_fix SFLogger.h headers
copy_and_fix SFOAuthCoordinator.h headers
copy_and_fix SFOAuthCredentials.h headers
copy_and_fix SFOAuthInfo.h headers
copy_and_fix SFPushNotificationManager.h headers
copy_and_fix SFUserAccount.h headers
copy_and_fix SFUserAccountConstants.h headers
copy_and_fix SFUserAccountManager.h headers
copy_and_fix SFUserAccountIdentity.h headers
copy_and_fix SalesforceSDKManager.h headers
copy_and_fix SFAuthErrorHandler.h headers
copy_and_fix SFAuthErrorHandlerList.h headers
copy_and_fix AppDelegate+SalesforceHybridSDK.m classes
copy_and_fix UIApplication+SalesforceHybridSDK.m classes
copy_and_fix InitialViewController.m classes
copy_and_fix SalesforceSDKCoreDefines.h headers
echo "Copying needed libraries to src/ios/frameworks"
copy_lib libSalesforceCommonUtils.a
copy_lib libSalesforceHybridSDK.a
copy_lib libSalesforceOAuth.a
copy_lib libSalesforceSDKCore.a
copy_lib libSalesforceSecurity.a
copy_lib libSalesforceNetwork.a
copy_lib libSalesforceRestAPI.a
copy_lib libSmartSync.a
copy_lib libSalesforceSDKCommon.a
copy_lib libsqlcipher.a
echo "Copying Images.xcassets"
cp -RL $IOS_SDK_FOLDER/shared/resources/Images.xcassets src/ios/resources/Images.xcassets
echo "Copying Settings.bundle"
cp -RL $IOS_SDK_FOLDER/shared/resources/Settings.bundle src/ios/resources/
echo "Copying SalesforceSDKResources.bundle"
cp -RL $IOS_SDK_FOLDER/shared/resources/SalesforceSDKResources.bundle src/ios/resources/

echo "*** Shared ***"
echo "Copying split cordova.force.js out of bower_components"
cp $SHARED_SDK_FOLDER/gen/plugins/com.salesforce/*.js www/

echo "*** Windows ***"
echo "Copying windows files from $WINDOWS_SDK_FOLDER to src/windows/$WINDOWS_SDK_FOLDER"
cp -RL $WINDOWS_SDK_FOLDER src/windows/
cp src/windows/$WINDOWS_SDK_FOLDER/SalesforceSDK/CordovaPluginJavascript/*.js src/windows/
echo "*** Cleanup ***"
rm -rf tmp


