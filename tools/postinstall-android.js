console.log("Running SalesforceMobileSDK plugin android post-install script");
var useSmartStoreOrSmartSync = true;
var targetAndroidApi = 21; 


//--------------------------------------
// Useful functions
//--------------------------------------
var fs = require('fs');
var exec = require('child_process').exec;
var path = require('path');

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
    var appName = "com.salesforce.androidsdk." + (useSmartStoreOrSmartSync  ? "smartsync.app.HybridAppWithSmartSync"  : "app.HybridApp");

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
        data = data.replace(/android\:targetSdkVersion\=\"19\"/, 'android:targetSdkVersion="' + targetAndroidApi + '"');
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

var libProject = useSmartStoreOrSmartSync ? path.join('..', '..', 'plugins', 'com.salesforce', 'src', 'android', 'libs', 'SmartSync') : path.join('..', '..', 'plugins', 'com.salesforce', 'src', 'android', 'libs', 'SalesforceSDK');
var cordovaLibProject = path.join('..', '..', '..', '..', '..', '..', 'platforms', 'android', 'CordovaLib');

console.log('Fixing application AndroidManifest.xml');
fixFile(path.join('platforms', 'android', 'AndroidManifest.xml'), fixAndroidManifest);

console.log("Done running SalesforceMobileSDK plugin android post-install script");
