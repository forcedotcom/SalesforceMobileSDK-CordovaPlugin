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
    let androidExePath = path.join(androidHomeDir, 'tools', 'android');
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

console.log('Fixing root level Gradle file for the generated app');
replaceTextInFile(path.join(appProjectRoot, 'settings.gradle'), "include \":CordovaLib\"", "");

shelljs.echo("def salesforceMobileSdkRoot = new File('mobile_sdk/SalesforceMobileSDK-Android');").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("if (salesforceMobileSdkRoot.exists()) {").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("  includeBuild(salesforceMobileSdkRoot)").toEnd(path.join(appProjectRoot, 'settings.gradle'));
shelljs.echo("}").toEnd(path.join(appProjectRoot, 'settings.gradle'));

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
    const newLibDep = "api 'com.salesforce.mobilesdk:SalesforceHybrid:12.1.0'";
    replaceTextInFile(path.join(appProjectRoot, 'app', 'build.gradle'), 'implementation(project(path: \":CordovaLib\"))', newLibDep);
}

console.log("Done running SalesforceMobileSDK plugin android post-install script");
