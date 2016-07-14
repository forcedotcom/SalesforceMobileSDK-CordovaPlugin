var fs = require('fs');
var path = require('path');

// Function to remove duplicate entry in AndroidManifest.xml.
var removeDuplicateManifestEntry = function(data) {
    var singleTop = 'android:launchMode="singleTop" ';
    data = data.replace(singleTop, '');
    console.log('Removed duplicate entry in AndroidManifest.xml.');
    return data;
};

var fixManifestFile = function(path, fix) {
    var data = fix(fs.readFileSync(path, 'utf8'));
    fs.writeFileSync(path, data);
};

module.exports = function(ctx) {
    run(ctx);
};

/**
 * Run the hook logic.
 *
 * @param {Object} ctx - Cordova context object.
 */
function run(ctx) {
    console.log('Removing duplicate entry from AndroidManifest.xml.');
    if (ctx.opts.cordova.platforms.indexOf('android') < 0) {
        console.warn('Not Android.\n');
        return;
    }
    try {
        fixManifestFile(path.join('platforms', 'android', 'AndroidManifest.xml'), removeDuplicateManifestEntry);
    } catch(e) {
        console.warn('error: ' + e);
    }   
}
