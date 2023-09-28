console.log("Running SalesforceMobileSDK plugin ios post-install script");

//--------------------------------------
// Useful functions
//--------------------------------------
var path = require('path');
var shelljs;


try {
    shelljs = require('shelljs');
    var version = require('shelljs/package.json').version
    if (version !== '0.8.5') {
        console.log('The version 0.8.5 of the node package shelljs is required to use this script. Run \'npm install shelljs@0.8.5\' before running this script.');
        process.exit(1);
    }

} catch(e) {
    console.log('The node package shelljs is required to use this script. Run \'npm install shelljs@0.8.5\' before running this script.');
    process.exit(1);
}

//--------------------------------------
// Doing actual post installation work
//--------------------------------------
var pluginRoot = path.join('plugins', 'com.salesforce');
var classesRoot = path.join('plugins', 'com.salesforce', 'src', 'ios', 'classes');
var appProjectRoot = path.join('platforms', 'ios');

console.log('Moving AppDelegate.m to the correct location');
shelljs.cp('-f', path.join(classesRoot, 'AppDelegate.m'), appProjectRoot);

console.log("Done running SalesforceMobileSDK plugin ios post-install script");
