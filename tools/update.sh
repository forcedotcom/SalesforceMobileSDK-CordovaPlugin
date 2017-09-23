#!/bin/bash

#set -x  # turn trace on 
set -e   # stop at first error


OPT_BUILD="yes"
OPT_BRANCH=""
OPT_OS=""

usage ()
{
    echo "usage: $0 -b <branch name> -o <os name> [-n]"
    echo "  Where <branch name> is the branch of each SDK to build."
    echo "  Where <os name> is the name of the platform to build"
    echo "  -n specifies that the iOS SDK should not be rebuilt."
}

parse_opts ()
{
    while getopts :b:o:n command_line_opt
    do
        case ${command_line_opt} in
            b)
                OPT_BRANCH=${OPTARG};;
            o)
                OPT_OS=${OPTARG};;
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

    if [ "${OPT_OS}" == "" ]
    then
        OPT_OS="all"
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

    valid_ios_regex='^i[oO][sS]$'
    valid_android_regex='^[aA]ndroid$'
    valid_all_regex='^all$'

    if [[ "${OPT_OS}" =~ ${valid_ios_regex} ]] || [[ "${OPT_OS}" =~ ${valid_android_regex} ]] || [[ "${OPT_OS}" =~ ${valid_all_regex} ]]
    then
        #No action
        :
    else
        echo "${OPT_OS} is not a valid os name.  Should be either iOS or android or all"
        exit 2
    fi
}

# Helper functions
copy_and_fix ()
{
    echo "* Fixing and copying $1 to $2 directory"
    find tmp -name $1 | xargs sed -E 's/#import <(SalesforceAnalytics|SalesforceSDKCore|SmartStore|SmartSync|SalesforceHybrid|CocoaLumberjack).*\/(.*)>/#import "\2"/' > src/ios/$2/$1
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

update_ios_repo ()
{   
    update_repo "${IOS_SDK_FOLDER}" "${IOS_SDK_REPO_PATH}"
    
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
}

update_android_repo ()
{
    update_repo "${ANDROID_SDK_FOLDER}" "${ANDROID_SDK_REPO_PATH}"
    cd ${ROOT_FOLDER}
}

create_ios_dirs()
{
    echo "Creating ios directories"
    mkdir -p src/ios/headers
    mkdir -p src/ios/frameworks
    mkdir -p src/ios/classes
    mkdir -p src/ios/resources
}

create_android_dirs()
{
    echo "Creating android directories"
    mkdir -p src/android/libs
    mkdir -p src/android/assets
}

copy_ios_sdk()
{
    echo "*** iOS ***"
    echo "Copying SalesforceAnalytics library"
    unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceAnalytics-Debug.zip -d tmp
    echo "Copying SalesforceSDKCore library"
    unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceSDKCore-Debug.zip -d tmp
    echo "Copying SmartStore library"
    unzip $IOS_SDK_FOLDER/build/artifacts/SmartStore-Debug.zip -d tmp
    echo "Copying SmartSync library"
    unzip $IOS_SDK_FOLDER/build/artifacts/SmartSync-Debug.zip -d tmp
    echo "Copying SalesforceHybridSDK library"
    unzip $IOS_SDK_FOLDER/build/artifacts/SalesforceHybridSDK-Debug.zip -d tmp
    echo "Copying sqlcipher library"    
    cp -RL $IOS_SDK_FOLDER/external/ThirdPartyDependencies/sqlcipher tmp
    echo "Copying CocoaLumberjack library"
    unzip $IOS_SDK_FOLDER/build/artifacts/Lumberjack-Debug.zip -d tmp
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
    copy_and_fix SalesforceSDKConstants.h headers
    copy_and_fix SFCommunityData.h headers
    copy_and_fix SFDefaultUserManagementViewController.h headers
    copy_and_fix SFHybridViewConfig.h headers
    copy_and_fix SFHybridViewController.h headers
    copy_and_fix SFIdentityCoordinator.h headers
    copy_and_fix SFIdentityData.h headers
    copy_and_fix SFLocalhostSubstitutionCache.h headers
    copy_and_fix SFSDKLogger.h headers
    copy_and_fix SFSDKFileLogger.h headers
    copy_and_fix NSNotificationCenter+SFAdditions.h headers
    copy_and_fix SFOAuthCoordinator.h headers
    copy_and_fix SFOAuthCredentials.h headers
    copy_and_fix SFOAuthInfo.h headers
    copy_and_fix SFPushNotificationManager.h headers
    copy_and_fix SFUserAccount.h headers
    copy_and_fix SFUserAccountConstants.h headers
    copy_and_fix SFUserAccountManager.h headers
    copy_and_fix SFUserAccountIdentity.h headers
    copy_and_fix SalesforceSDKManager.h headers
    copy_and_fix SalesforceSDKManagerWithSmartStore.h headers
    copy_and_fix SFAuthErrorHandler.h headers
    copy_and_fix SFAuthErrorHandlerList.h headers
    copy_and_fix AppDelegate+SalesforceHybridSDK.m classes
    copy_and_fix UIApplication+SalesforceHybridSDK.m classes
    copy_and_fix InitialViewController.m classes
    copy_and_fix SalesforceSDKCoreDefines.h headers
    copy_and_fix SFLoginViewController.h headers
    copy_and_fix SFSDKLoginHostDelegate.h headers
    copy_and_fix SFSDKLoginHost.h headers
    copy_and_fix SFSDKLoginHostListViewController.h headers
    copy_and_fix SFSDKLoginHostStorage.h headers
    copy_and_fix DDLog.h headers
    copy_and_fix DDFileLogger.h headers
    copy_and_fix DDLegacyMacros.h headers

    echo "Copying needed libraries to src/ios/frameworks"
    copy_lib libSalesforceAnalytics.a
    copy_lib libSalesforceSDKCore.a
    copy_lib libSmartStore.a
    copy_lib libSmartSync.a
    copy_lib libSalesforceHybridSDK.a
    copy_lib libsqlcipher.a
    copy_lib libCocoaLumberjack.a
    echo "Copying Images.xcassets"
    cp -RL $IOS_SDK_FOLDER/shared/resources/Images.xcassets src/ios/resources/Images.xcassets
    echo "Copying SalesforceSDKAssets.xcassets"
    cp -RL $IOS_SDK_FOLDER/shared/resources/SalesforceSDKAssets.xcassets src/ios/resources/SalesforceSDKAssets.xcassets
    echo "Copying SalesforceSDKResources.bundle"
    cp -RL $IOS_SDK_FOLDER/shared/resources/SalesforceSDKResources.bundle src/ios/resources/
}

copy_android_sdk()
{
    echo "*** Android ***"
    echo "Copying SalesforceAnalytics library"
    cp -RL $ANDROID_SDK_FOLDER/libs/SalesforceAnalytics src/android/libs/
    echo "Copying SalesforceSDK library"
    cp -RL $ANDROID_SDK_FOLDER/libs/SalesforceSDK src/android/libs/
    echo "Copying SmartStore library"
    cp -RL $ANDROID_SDK_FOLDER/libs/SmartStore src/android/libs/
    echo "Copying SmartSync library"
    cp -RL $ANDROID_SDK_FOLDER/libs/SmartSync src/android/libs/
    echo "Copying SalesforceHybrid library"
    cp -RL $ANDROID_SDK_FOLDER/libs/SalesforceHybrid src/android/libs/
    echo "Copying Gradle wrapper files"
    cp $ANDROID_SDK_FOLDER/gradle.properties ./
    cp $ANDROID_SDK_FOLDER/gradlew.bat ./
    cp $ANDROID_SDK_FOLDER/gradlew ./
    cp -RL $ANDROID_SDK_FOLDER/gradle ./
}

parse_opts "$@"

# Work from the root of the repo.
cd ${ROOT_FOLDER}

#if os is ios call update ios repo
if [ "$OPT_OS" == "ios" ]
then
    update_ios_repo
fi

#if os is android call update android
if [ "$OPT_OS" == "android" ]
then
    update_android_repo
fi
#if os is all call update ios and android repo
if [ "$OPT_OS" == "all" ]
then
    update_ios_repo
    update_android_repo
fi
update_repo "${SHARED_SDK_FOLDER}" "${SHARED_SDK_REPO_PATH}"

cd ${ROOT_FOLDER}
echo "*** Creating directories ***"
echo "Starting clean"
rm -rf tmp src/ios src/android
echo "Creating tmp directory"
mkdir -p tmp
#create ios directories
if [ "$OPT_OS" == "ios" ]
then
    create_ios_dirs
fi

#create android directories
if [ "$OPT_OS" == "android" ]
then
    create_android_dirs
fi
#create all directories
if [ "$OPT_OS" == "all" ]
then
    create_ios_dirs
    create_android_dirs
fi

#copy ios sdk
if [ "$OPT_OS" == "ios" ]
then
    copy_ios_sdk
fi
#copy android sdk
if [ "$OPT_OS" == "android" ]
then
    copy_android_sdk
fi
#copy all sdks
if [ "$OPT_OS" == "all" ]
then
    copy_ios_sdk
    copy_android_sdk
fi
echo "*** Shared ***"
echo "Copying split cordova.force.js out of bower_components"
cp $SHARED_SDK_FOLDER/gen/plugins/com.salesforce/*.js www/

echo "*** Cleanup ***"
rm -rf tmp
cd ${ROOT_FOLDER}
rm -rf $ANDROID_SDK_FOLDER
rm -rf $IOS_SDK_FOLDER
rm -rf $SHARED_SDK_FOLDER
