#!/bin/bash

#set -x

OPT_VERSION=""
OPT_IS_DEV="no"
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

usage ()
{
    echo "Use this script to set Mobile SDK version number in source files"
    echo "Usage: $0 -v <version> [-d <isDev>]"
    echo "  where: version is the version e.g. 7.1.0"
    echo "         isDev is yes or no (default) to indicate whether it is a dev build"
}

parse_opts ()
{
    while getopts v:d: command_line_opt
    do
        case ${command_line_opt} in
            v)  OPT_VERSION=${OPTARG};;
            d)  OPT_IS_DEV=${OPTARG};;
        esac
    done

    if [ "${OPT_VERSION}" == "" ]
    then
        echo -e "${RED}You must specify a value for the version.${NC}"
        usage
        exit 1
    fi
}

# Helper functions
update_package_json ()
{
    local file=$1
    local version=$2
    gsed -i "s/\"version\":.*\"[^\"]*\"/\"version\": \"${version}\"/g" ${file}
}

update_tools_postinstall_android_sh ()
{
    local file=$1
    local version=$2
    gsed -i "s/\(com.salesforce.mobilesdk:SalesforceHybrid:\)[0-9]\+.[0-9]\+.[0-9]\+/\1$version/g" ${file}
}

update_plugin_xml ()
{
    local file=$1
    local version=$2
    local isDev=$3
    local newPodSpecVersion="tag=\"v${version}\""
    gsed -i "s/\($[ ]*\)version.*=.*\"[^\"]*\">/\1version=\"${version}\">/g" ${file}

    if [ $isDev == "yes" ]
    then
        newPodSpecVersion="branch=\"dev\""
    fi
    gsed -i "s/\(.*<pod.*git=\"https:\/\/github.com\/.*\/SalesforceMobileSDK-[^\"]*\"\).*$/\1 ${newPodSpecVersion} \/>/g" ${file}
}


parse_opts "$@"

echo -e "${YELLOW}*** SETTING VERSION TO ${OPT_VERSION}, IS DEV = ${OPT_IS_DEV} ***${NC}"

echo "*** Updating package.json ***"
update_package_json "./package.json" "${OPT_VERSION}"

echo "*** Updating postinstall-android.js ***"
update_tools_postinstall_android_sh "./tools/postinstall-android.js" "${OPT_VERSION}"

echo "*** Updating plugin.xml ***"
update_plugin_xml "./plugin.xml" "${OPT_VERSION}" "${OPT_IS_DEV}"

