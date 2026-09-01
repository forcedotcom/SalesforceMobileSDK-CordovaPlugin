console.log('Running SalesforceMobileSDK plugin ios post-install script');

const fs = require('fs');
const path = require('path');

function replaceTextInFile(fileName, textInFile, replacementText) {
    const contents = fs.readFileSync(fileName, 'utf8');
    const lines = contents.split(/\r*\n/);
    const result = lines.map(function (line) {
        return line.replace(textInFile, replacementText);
    }).join('\n');
    fs.writeFileSync(fileName, result, 'utf8'); 
}


const appProjectRoot = path.join('platforms', 'ios');
const appName = path.parse(fs.readdirSync(appProjectRoot).filter(f=>f.endsWith('.xcworkspace'))[0]).name;
const projectFile = path.join(appProjectRoot, `${appName}.xcodeproj`, 'project.pbxproj');

// In cordova-ios 8.x, <source-file> entries go into the CordovaPlugins SPM target, which cannot
// link against CocoaPods frameworks like SalesforceHybridSDK. So AppDelegate.swift is NOT declared
// as a <source-file> in plugin.xml — instead we copy it here and redirect the app target reference.
console.log('Copying AppDelegate.swift from plugin to App/Plugins/com.salesforce/');
const pluginRoot = path.join('plugins', 'com.salesforce');
const appDelegateSource = path.join(pluginRoot, 'src', 'ios', 'classes', 'AppDelegate.swift');
const pluginsDir = path.join(appProjectRoot, 'App', 'Plugins', 'com.salesforce');
if (!fs.existsSync(pluginsDir)) {
    fs.mkdirSync(pluginsDir, { recursive: true });
}
fs.copyFileSync(appDelegateSource, path.join(pluginsDir, 'AppDelegate.swift'));

console.log('Pointing app target AppDelegate.swift to the SDK version in Plugins/com.salesforce/');
replaceTextInFile(projectFile, 'path = AppDelegate.swift;', 'name = AppDelegate.swift; path = Plugins/com.salesforce/AppDelegate.swift;');

console.log('Adding InitialViewController to Bridging-Header for Swift interop');
const bridgingHeaderFile = path.join(appProjectRoot, 'App', 'Bridging-Header.h');
if (fs.existsSync(bridgingHeaderFile)) {
    const bridgingContents = fs.readFileSync(bridgingHeaderFile, 'utf8');
    const importLine = '#import "InitialViewController.h"';
    if (!bridgingContents.includes(importLine)) {
        fs.writeFileSync(bridgingHeaderFile, bridgingContents + '\n' + importLine + '\n', 'utf8');
    }
} else {
    console.warn('WARNING: Bridging-Header.h not found at ' + bridgingHeaderFile + ' — InitialViewController will not be visible from Swift. The build will likely fail.');
}

// Fix Xcode 27 compatibility: raise any pod deployment target below iOS 15.0
console.log('Patching Podfile to raise minimum pod deployment target to 15.0 for Xcode 27 compatibility');
const podfilePath = path.join(appProjectRoot, 'Podfile');
if (fs.existsSync(podfilePath)) {
    const podfileContents = fs.readFileSync(podfilePath, 'utf8');
    if (!podfileContents.includes('post_install')) {
        const postInstallBlock = `
post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      dt = config.build_settings['IPHONEOS_DEPLOYMENT_TARGET']
      if dt && dt.to_f < 15.0
        config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '15.0'
      end
    end
  end
end
`;
        fs.appendFileSync(podfilePath, postInstallBlock, 'utf8');
    }
}

console.log('Done running SalesforceMobileSDK plugin ios post-install script');




