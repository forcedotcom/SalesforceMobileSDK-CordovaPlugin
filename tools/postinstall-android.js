console.log("Running SalesforceMobileSDK plugin android post-install script");

//--------------------------------------
// Useful functions
//--------------------------------------
const fs = require('fs');
const exec = require('child_process').exec;
const path = require('path');
const shelljs = loadShellJs();


function loadShellJs() {
    try {
	const shelljs = require('shelljs');
	const version = require('shelljs/package.json').version
	if (version !== '0.8.5') {
            console.log('The version 0.8.5 of the node package shelljs is required to use this script. Run \'npm install shelljs@0.8.5\' before running this script.');
            process.exit(1);
	}
	return shelljs;

    } catch(e) {
	console.log('The node package shelljs is required to use this script. Run \'npm install shelljs@0.8.5\' before running this script.');
	process.exit(1);
    }
}

const getAndroidSDKToolPath = function() {
    const androidHomeDir = process.env.ANDROID_HOME;
    if (typeof androidHomeDir !== 'string') {
        console.log('You must set the ANDROID_HOME environment variable to the path of your installation of the Android SDK.');
        return null;
    }
    const androidExePath = path.join(androidHomeDir, 'tools', 'android');
    const isWindows = (/^win/i).test(process.platform);
    if (isWindows) {
        androidExePath = androidExePath + '.bat';
    }
    if (!fs.existsSync(androidExePath)) {
        console.log('The "android" utility does not exist at ' + androidExePath + '.  Make sure you\'ve properly installed the Android SDK.');
        return null;
    }
    return androidExePath;
};

function replaceTextInFile(fileName, textInFile, replacementText) {
    const contents = fs.readFileSync(fileName, 'utf8');
    const lines = contents.split(/\r*\n/);
    const result = lines.map(function (line) {
        return line.replace(textInFile, replacementText);
    }).join('\n');
    fs.writeFileSync(fileName, result, 'utf8'); 
}


//--------------------------------------
// Doing actual post installation work
//--------------------------------------
const androidExePath = getAndroidSDKToolPath();
if (androidExePath === null) {
    process.exit(2);
}

const pluginRoot = path.join('plugins', 'com.salesforce');
const libProjectRoot = path.join('plugins', 'com.salesforce', 'src', 'android', 'libs');
const appProjectRoot = path.join('platforms', 'android');

console.log('Moving Salesforce libraries to the correct location');
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceAnalytics'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceSDK'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SmartStore'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'MobileSync'), appProjectRoot);
shelljs.cp('-R', path.join(libProjectRoot, 'SalesforceHybrid'), appProjectRoot);

console.log('Fixing Gradle dependency paths in Salesforce libraries');
const oldSalesforceAnalyticsDep = "api(project\(\":libs:SalesforceAnalytics\"\))";
const oldSalesforceSdkDep = "api(project\(\":libs:SalesforceSDK\"\))";
const oldSmartStoreDep = "api(project\(\":libs:SmartStore\"\))";
const oldMobileSyncDep = "api(project\(\":libs:MobileSync\"\))";
replaceTextInFile(path.join(appProjectRoot, 'SalesforceSDK', 'build.gradle.kts'), oldSalesforceAnalyticsDep, 'api(project\(\":SalesforceAnalytics\"\))');
replaceTextInFile(path.join(appProjectRoot, 'SmartStore', 'build.gradle.kts'), oldSalesforceSdkDep, 'api(project\(\":SalesforceSDK\"\))');
replaceTextInFile(path.join(appProjectRoot, 'MobileSync', 'build.gradle.kts'), oldSmartStoreDep, 'api(project\(\":SmartStore\"\))');
replaceTextInFile(path.join(appProjectRoot, 'SalesforceHybrid', 'build.gradle.kts'), oldMobileSyncDep, 'api(project\(\":MobileSync\"\))');

console.log('Fixing root level Gradle file for the generated app');
replaceTextInFile(path.join(appProjectRoot, 'settings.gradle'), "include \":CordovaLib\"", "");
shelljs.echo("include \":SalesforceAnalytics\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SalesforceSDK\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SmartStore\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":MobileSync\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("include \":SalesforceHybrid\"\n").toEnd(path.join(appProjectRoot, 'settings.gradle'));

console.log('Moving Gradle wrapper files to application directory');
shelljs.cp('-R', path.join(pluginRoot, 'gradle.properties'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradlew.bat'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradlew'), appProjectRoot);
shelljs.cp('-R', path.join(pluginRoot, 'gradle'), appProjectRoot);

const data = fs.readFileSync(path.join(appProjectRoot, 'app', 'build.gradle'), 'utf8');

// First verify that we didn't already modify the build.gradle file.
if (data.indexOf("SalesforceHybrid") < 0)
{
    console.log('Fixing application build.gradle');
    const oldAndroidDepTree = "android {";
    const newAndroidDepTree = "android {\n\tpackagingOptions {\n\t\texclude 'META-INF/LICENSE'\n\t\texclude 'META-INF/LICENSE.txt'\n\t\texclude 'META-INF/DEPENDENCIES'\n\t\texclude 'META-INF/NOTICE'\n\t}";
    replaceTextInFile(path.join(appProjectRoot, 'app', 'build.gradle'), oldAndroidDepTree, newAndroidDepTree);
    const newLibDep = "api project(':SalesforceHybrid')";
    replaceTextInFile(path.join(appProjectRoot, 'app', 'build.gradle'), 'implementation(project(path: \":CordovaLib\"))', newLibDep);
}

console.log("Done running SalesforceMobileSDK plugin android post-install script");

