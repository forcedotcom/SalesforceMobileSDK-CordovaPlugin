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
        ./build-all.sh
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
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/SalesforceAnalytics.framework src/ios/frameworks/
    echo "Copying SalesforceSDKCore library"
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/SalesforceSDKCore.framework src/ios/frameworks/
    echo "Copying SmartStore library"
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/SmartStore.framework src/ios/frameworks/
    echo "Copying SmartSync library"
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/SmartSync.framework src/ios/frameworks/
    echo "Copying SalesforceHybridSDK library"
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/SalesforceHybridSDK.framework src/ios/frameworks/
    echo "Copying sqlcipher library"    
    cp -RL $IOS_SDK_FOLDER/external/ThirdPartyDependencies/sqlcipher/libsqlcipher.a src/ios/frameworks/
    echo "Copying CocoaLumberjack library"
    cp -RL $IOS_SDK_FOLDER/build/artifacts/Debug/CocoaLumberjack.framework src/ios/frameworks/
    echo "Copying AppDelegate+SalesforceHybridSDK"    
    cp -RL $IOS_SDK_FOLDER/shared/hybrid/AppDelegate+SalesforceHybridSDK.*  src/ios/classes/
    cp -RL $IOS_SDK_FOLDER/shared/hybrid/UIApplication+SalesforceHybridSDK.*  src/ios/classes/
    cp -RL $IOS_SDK_FOLDER/shared/hybrid/InitialViewController.*  src/ios/classes/
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

