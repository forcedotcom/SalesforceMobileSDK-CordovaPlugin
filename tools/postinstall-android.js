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

function replaceTextInFile(fileName, textInFile, replacementText) {
    const contents = fs.readFileSync(fileName, 'utf8');
    const lines = contents.split(/\r*\n/);
    const result = lines.map(function (line) {
        return line.replace(textInFile, replacementText);
    }).join('\n');
    fs.writeFileSync(fileName, result, 'utf8');
}

function upsertStringResource(fileName, name, value) {
    const stringResource = `    <string name="${name}">${value}</string>`;
    const resourcePattern = new RegExp(`^[ \\t]*<string\\s+[^>]*\\bname\\s*=\\s*["']${name}["'][^>]*>[\\s\\S]*?</string>`, 'm');
    const contents = fs.existsSync(fileName)
        ? fs.readFileSync(fileName, 'utf8')
        : '<?xml version="1.0" encoding="utf-8"?>\n<resources>\n</resources>\n';
    const updatedContents = resourcePattern.test(contents)
        ? contents.replace(resourcePattern, stringResource)
        : contents.replace('</resources>', `${stringResource}\n</resources>`);
    fs.writeFileSync(fileName, updatedContents, 'utf8');
}

function setApplicationName(fileName, applicationName) {
    const contents = fs.readFileSync(fileName, 'utf8');
    const applicationPattern = /<application\b[^>]*>/;
    const applicationTag = contents.match(applicationPattern);
    if (!applicationTag) {
        throw new Error(`Application tag not found in ${fileName}`);
    }
    const updatedApplicationTag = /\bandroid:name="[^"]*"/.test(applicationTag[0])
        ? applicationTag[0].replace(/\bandroid:name="[^"]*"/, `android:name="${applicationName}"`)
        : applicationTag[0].replace(/>$/, ` android:name="${applicationName}">`);
    fs.writeFileSync(fileName, contents.replace(applicationPattern, updatedApplicationTag), 'utf8');
}

function getAndroidPackageName(appProjectRoot) {
    const config = JSON.parse(fs.readFileSync(path.join(appProjectRoot, 'cdv-gradle-config.json'), 'utf8'));
    const packageName = config.PACKAGE_NAMESPACE;
    if (!/^[A-Za-z_]\w*(\.[A-Za-z_]\w*)+$/.test(packageName)) {
        throw new Error(`Invalid Android package name: ${packageName}`);
    }
    return packageName;
}


//--------------------------------------
// Doing actual post installation work
//--------------------------------------
const pluginRoot = path.join('plugins', 'com.salesforce');
const libProjectRoot = path.join('plugins', 'com.salesforce', 'src', 'android', 'libs');
const appProjectRoot = path.join('platforms', 'android');

console.log('Fixing cordova.gradle');
// TODO: Remove fix once we switch to cordova android 15.0.1
//       Fix should be in cordova android 15.0.1 - see https://github.com/apache/cordova-android/pull/1896
replaceTextInFile(path.join(appProjectRoot, 'CordovaLib', 'cordova.gradle'), 'import java.util.regex.Pattern', 'import groovy.xml.XmlParser\nimport java.util.regex.Pattern');

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
    const newLibDep = "api 'com.salesforce.mobilesdk:SalesforceHybrid:14.0.0-rc.1-rc.0'";
    replaceTextInFile(path.join(appProjectRoot, 'app', 'build.gradle'), 'implementation(project(path: \":CordovaLib\"))', newLibDep);
}

console.log('Injecting MainApplication.kt into generated app');
// Cordova writes the effective package name here, including android-packageName overrides.
const packageName = getAndroidPackageName(appProjectRoot);
const packagePath = packageName.replace(/\./g, path.sep);
const mainAppSrcDir = path.join(appProjectRoot, 'app', 'src', 'main', 'java', packagePath);
shelljs.mkdir('-p', mainAppSrcDir);
const mainAppSrc = path.join(pluginRoot, 'src', 'android', 'MainApplication.kt');
const mainAppDest = path.join(mainAppSrcDir, 'MainApplication.kt');
shelljs.cp(mainAppSrc, mainAppDest);
replaceTextInFile(mainAppDest, 'package com.salesforce.androidsdk.phonegap.app', `package ${packageName}`);

// Set android:name in AndroidManifest.xml to point to the app's MainApplication.
// plugin.xml no longer sets android:name, so we inject it here into the <application> tag.
const manifestFile = path.join(appProjectRoot, 'app', 'src', 'main', 'AndroidManifest.xml');
setApplicationName(manifestFile, `${packageName}.MainApplication`);

// Cordova Android 15 no longer creates strings.xml, so config-file entries targeting it are ignored.
const stringsFile = path.join(appProjectRoot, 'app', 'src', 'main', 'res', 'values', 'strings.xml');
upsertStringResource(stringsFile, 'account_type', `${packageName}.login`);
upsertStringResource(stringsFile, 'app_package', packageName);
console.log(`MainApplication.kt injected at ${mainAppDest}`);

// Add the LoginActivity browser-redirect intent-filter with placeholder tokens. forcehybrid
// substitutes the real callback scheme/host/path via template.js; a direct `cordova plugin add`
// leaves the placeholders for the developer to fill in. Theme must be @style/SalesforceSDK to
// match the SalesforceSDK library's LoginActivity or the manifest merger fails. Idempotent.
console.log('Injecting LoginActivity redirect intent-filter (placeholders) into AndroidManifest.xml');
const redirectManifestFile = path.join(appProjectRoot, 'app', 'src', 'main', 'AndroidManifest.xml');
const redirectManifest = fs.readFileSync(redirectManifestFile, 'utf8');
if (redirectManifest.indexOf('com.salesforce.androidsdk.ui.LoginActivity') === -1) {
    const loginActivityBlock =
        '        <!-- Salesforce Mobile SDK OAuth redirect. Replace the __INSERT_..._HERE__ tokens\n' +
        '             with your callback URL\'s scheme/host/path (forcehybrid does this automatically).\n' +
        '             Keep android:theme="@style/SalesforceSDK" to match the SDK library. -->\n' +
        '        <activity\n' +
        '            android:name="com.salesforce.androidsdk.ui.LoginActivity"\n' +
        '            android:exported="true"\n' +
        '            android:launchMode="singleTask"\n' +
        '            android:theme="@style/SalesforceSDK">\n' +
        '            <intent-filter>\n' +
        '                <action android:name="android.intent.action.VIEW" />\n' +
        '                <category android:name="android.intent.category.DEFAULT" />\n' +
        '                <category android:name="android.intent.category.BROWSABLE" />\n' +
        '                <data\n' +
        '                    android:scheme="__INSERT_CALLBACK_URL_SCHEME_HERE__"\n' +
        '                    android:host="__INSERT_CALLBACK_URL_HOST_HERE__"\n' +
        '                    android:path="/__INSERT_CALLBACK_URL_PATH_HERE__" />\n' +
        '            </intent-filter>\n' +
        '        </activity>\n';
    const updatedRedirectManifest = redirectManifest.replace(/([ \t]*)<\/application>/, loginActivityBlock + '$1</application>');
    fs.writeFileSync(redirectManifestFile, updatedRedirectManifest, 'utf8');
    console.log('Injected LoginActivity redirect intent-filter with placeholder tokens');
} else {
    console.log('LoginActivity already present in manifest — skipping redirect intent-filter injection');
}

console.log("Done running SalesforceMobileSDK plugin android post-install script");
