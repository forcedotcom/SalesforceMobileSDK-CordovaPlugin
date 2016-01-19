console.log("Running SalesforceMobileSDK plugin android post-install script");
var targetAndroidApi = 23;

//--------------------------------------
// Useful functions
//--------------------------------------
var fs = require('fs');
var exec = require('child_process').exec;
var path = require('path');

try {
    var shelljs = require('shelljs');
} catch(e) {
    console.log('The node package shelljs is required to use this script. Run \'npm install shelljs\' before running this script.');
    process.exit(1);
}

var copyFile = function(srcPath, targetPath) {
    fs.createReadStream(srcPath).pipe(fs.createWriteStream(targetPath));
};

var fixFile = function(path, fix) {
    fs.readFile(path, 'utf8', function (err, data) {
        fs.writeFile(path, fix(data), function (err) {
            if (err) {
                console.log(err);
            }
        });
    });
};

// Function to fix AndroidManifest.xml
var fixAndroidManifest = function(data) {

    // Fix application tag
    var appName = "com.salesforce.androidsdk.phonegap.app.HybridApp";

    // In case the script was run twice
    if (data.indexOf(appName) == -1) {
        var applicationTag = '<application android:hardwareAccelerated="true" android:icon="@drawable/sf__icon" android:label="@string/app_name" android:manageSpaceActivity="com.salesforce.androidsdk.ui.ManageSpaceActivity" android:name="' + appName + '">'
        data = data.replace(/<application [^>]*>/, applicationTag);

        // Comment out first activity
        data = data.replace(/<activity/, "<!--<activity");
        data = data.replace(/<\/activity>/, "</activity>-->");

        // Change min sdk version
        data = data.replace(/android\:minSdkVersion\=\"10\"/, 'android:minSdkVersion="19"');

        // Change target api
        data = data.replace(/android\:targetSdkVersion\=\"22\"/, 'android:targetSdkVersion="' + targetAndroidApi + '"');
    }
    return data;
};

var getAndroidSDKToolPath = function() {
    var androidHomeDir = process.env.ANDROID_HOME;
    if (typeof androidHomeDir !== 'string') {
        console.log('You must set the ANDROID_HOME environment variable to the path of your installation of the Android SDK.');
        return null;
    }
    var androidExePath = path.join(androidHomeDir, 'tools', 'android');
    var isWindows = (/^win/i).test(process.platform);
    if (isWindows) {
        androidExePath = androidExePath + '.bat';
    }
    if (!fs.existsSync(androidExePath)) {
        console.log('The "android" utility does not exist at ' + androidExePath + '.  Make sure you\'ve properly installed the Android SDK.');
        return null;
    }
    return androidExePath;
};

//--------------------------------------
// Doing actual post installation work
//--------------------------------------
var androidExePath = getAndroidSDKToolPath();
if (androidExePath === null) {
    process.exit(2);
}

var pluginRoot = path.join('plugins', 'com.salesforce');
var libProjectRoot = path.join('plugins', 'com.salesforce', 'src', 'android', 'libs');
var appProjectRoot = path.join('platforms', 'android');

console.log('Fixing application AndroidManifest.xml');
fixFile(path.join('platforms', 'android', 'AndroidManifest.xml'), fixAndroidManifest);

console.log('Moving Salesforce libraries to the correct location');
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceSDK'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SmartStore'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SmartSync'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceHybrid'), appProjectRoot);

console.log('Fixing Gradle dependency paths in Salesforce libraries');
var oldCordovaDep = "compile project\(\':external:cordova:framework\'\)";
var oldSalesforceSdkDep = "compile project\(\':libs:SalesforceSDK\'\)";
var oldSmartStoreDep = "compile project\(\':libs:SmartStore\'\)";
var oldSmartSyncDep = "compile project\(\':libs:SmartSync\'\)";
shelljs.sed('-i', oldSalesforceSdkDep, 'compile project\(\':SalesforceSDK\'\)', path.join(appProjectRoot, 'SmartStore', 'build.gradle'));
shelljs.sed('-i', oldSmartStoreDep, 'compile project\(\':SmartStore\'\)', path.join(appProjectRoot, 'SmartSync', 'build.gradle'));
shelljs.sed('-i', oldCordovaDep, 'compile project\(\':CordovaLib\'\)', path.join(appProjectRoot, 'SalesforceHybrid', 'build.gradle'));
shelljs.sed('-i', oldSmartSyncDep, 'compile project\(\':SmartSync\'\)', path.join(appProjectRoot, 'SalesforceHybrid', 'build.gradle'));

console.log('Fixing root level Gradle file for the generated app');
shelljs.echo("include \":SalesforceSDK\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SmartStore\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SmartSync\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SalesforceHybrid\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));

console.log('Moving Gradle wrapper files to application directory');
shelljs.cp('-R', path.join(pluginRoot, 'gradle.properties'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradlew.bat'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradlew'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradle'), appProjectRoot);

fs.readFile(path.join(appProjectRoot, 'build.gradle'), 'utf8', function (err, data) {
    console.log('Fixing application build.gradle');
    
    // First verify that we didn't already modify the build.gradle file
    if(data.indexOf("org.apache.http.legacy") < 0 && data.indexOf("allprojects") < 0)
    {   
        var oldAndroidDepTree = "android {";
        var newAndroidDepTree = "android {\n\tpackagingOptions {\n\t\texclude 'META-INF/LICENSE'\n\t\texclude 'META-INF/LICENSE.txt'\n\t\texclude 'META-INF/DEPENDENCIES'\n\t\texclude 'META-INF/NOTICE'\n\t}";
        shelljs.sed('-i', oldAndroidDepTree, newAndroidDepTree, path.join(appProjectRoot, 'build.gradle'));
        shelljs.echo("allprojects {\n\trepositories {\n\t\tmavenCentral\(\)\n\t}\n}").toEnd(path.join(appProjectRoot, 'build.gradle'));
        var oldBuildScriptDepTree = "buildscript {";
        var newBuildScriptDepTree = "buildscript {\n\tdependencies {\n\t\tclasspath 'com.android.tools.build:gradle:1.3.1'\n\t}\n";
        shelljs.sed('-i', oldBuildScriptDepTree, newBuildScriptDepTree, path.join(appProjectRoot, 'build.gradle'));
        var newLibDep = "compile \"com.google.android.gms:play-services:7.5.0\"\ncompile project(':SalesforceHybrid')";
        var useLegacyStr = "android {\n\tuseLibrary 'org.apache.http.legacy'\n";
        shelljs.sed('-i', oldAndroidDepTree, useLegacyStr, path.join(appProjectRoot, 'CordovaLib', 'build.gradle'));
        shelljs.sed('-i', oldAndroidDepTree, useLegacyStr, path.join(appProjectRoot, 'build.gradle'));
        shelljs.sed('-i', 'debugCompile project(path: \"CordovaLib\", configuration: \"debug\")', newLibDep, path.join(appProjectRoot, 'build.gradle'));
        shelljs.sed('-i', 'releaseCompile project(path: \"CordovaLib\", configuration: \"release\")', '', path.join(appProjectRoot, 'build.gradle'));
    }
    console.log("Done running SalesforceMobileSDK plugin android post-install script");
});
