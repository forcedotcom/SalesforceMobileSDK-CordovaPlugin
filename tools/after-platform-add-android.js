//--------------------------------------
// Useful functions
//--------------------------------------
var fs = require('fs');
var path = require('path');
var targetAndroidApi = 23;

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
        
        console.log('Fixed AndroidManifest.xml');
    } else {
        console.log('Already fixed. Skipping.');
    }
    return data;
};

var fixFile = function(path, fix) {
    var data = fix(fs.readFileSync(path, 'utf8'));
    fs.writeFileSync(path, data);
};

module.exports = function(ctx) {
    run(ctx);
};

/**
 * Run the hook logic.
 *
 * @param {Object} ctx - cordova context object
 */
function run(ctx) {
    // make sure android platform is part of build 
    console.log('Fixing AndroidManifest.xml file for Salesforce SDK Plugin.');
    if (ctx.opts.cordova.platforms.indexOf('android') < 0) {
        console.warn('Not android.\n');
        return;
    }
    
    try {
        fixFile(path.join('platforms', 'android', 'AndroidManifest.xml'), fixAndroidManifest);
    } catch(e) {
        console.warn('error: ' + e);
    }   
}