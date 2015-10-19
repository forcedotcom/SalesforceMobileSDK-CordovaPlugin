console.log("Running SalesforceMobileSDK plugin android post-install script");
var targetAndroidApi = 21; 

//--------------------------------------
// Useful functions
//--------------------------------------
var fs = require('fs');
var exec = require('child_process').exec;
var path = require('path');
var shelljs = require('shelljs');

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
    var appName = "com.salesforce.androidsdk.smartsync.app.HybridAppWithSmartSync";

    // In case the script was run twice
    if (data.indexOf(appName) == -1) {
        var applicationTag = '<application android:hardwareAccelerated="true" android:icon="@drawable/sf__icon" android:label="@string/app_name" android:manageSpaceActivity="com.salesforce.androidsdk.ui.ManageSpaceActivity" android:name="' + appName + '">'
        data = data.replace(/<application [^>]*>/, applicationTag);

        // Comment out first activity
        data = data.replace(/<activity/, "<!--<activity");
        data = data.replace(/<\/activity>/, "</activity>-->");

        // Change min sdk version
        data = data.replace(/android\:minSdkVersion\=\"10\"/, 'android:minSdkVersion="17"');

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

var libProjectRoot = path.join('plugins', 'com.salesforce', 'src', 'android', 'libs');
var appProjectRoot = path.join('platforms', 'android');

console.log('Fixing application AndroidManifest.xml');
fixFile(path.join('platforms', 'android', 'AndroidManifest.xml'), fixAndroidManifest);

console.log('Moving Salesforce libraries to the correct location');
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceSDK'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SmartStore'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SmartSync'), appProjectRoot);

console.log('Fixing Gradle dependency paths in Salesforce libraries');
var oldCordovaDep = "compile project\(\':external:cordova:framework\'\)";
var oldSalesforceSdkDep = "compile project\(\':libs:SalesforceSDK\'\)";
var oldSmartStoreDep = "compile project\(\':libs:SmartStore\'\)";
shelljs.sed('-i', oldCordovaDep, 'compile project\(\':CordovaLib\'\)', path.join(appProjectRoot, 'SalesforceSDK', 'build.gradle'));
shelljs.sed('-i', oldSalesforceSdkDep, 'compile project\(\':SalesforceSDK\'\)', path.join(appProjectRoot, 'SmartStore', 'build.gradle'));
shelljs.sed('-i', oldSmartStoreDep, 'compile project\(\':SmartStore\'\)', path.join(appProjectRoot, 'SmartSync', 'build.gradle'));

console.log('Fixing root level Gradle file for the generated app');
shelljs.echo("include \":SalesforceSDK\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SmartStore\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SmartSync\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));

console.log("Done running SalesforceMobileSDK plugin android post-install script");
