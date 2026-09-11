console.log('Running SalesforceMobileSDK plugin ios post-prepare script');

const fs = require('fs');
const path = require('path');

const appProjectRoot = path.join('platforms', 'ios');

// Fix Xcode 27 compatibility: raise any pod deployment target below iOS 15.0.
// cordova prepare regenerates the Podfile and runs pod install, wiping any
// post_install block injected earlier. Patch the Pods project directly here
// instead, after pod install has run.
const podsProjectFile = path.join(appProjectRoot, 'Pods', 'Pods.xcodeproj', 'project.pbxproj');
if (fs.existsSync(podsProjectFile)) {
    console.log('Patching Pods project to raise minimum deployment target to 15.0 for Xcode 27 compatibility');
    let content = fs.readFileSync(podsProjectFile, 'utf8');
    const updated = content.replace(/IPHONEOS_DEPLOYMENT_TARGET = (\d+(?:\.\d+)?);/g, (match, version) => {
        return parseFloat(version) < 15.0
            ? 'IPHONEOS_DEPLOYMENT_TARGET = 15.0;'
            : match;
    });
    if (updated !== content) {
        fs.writeFileSync(podsProjectFile, updated, 'utf8');
        console.log('Done patching Pods project deployment targets');
    }
}

console.log('Done running SalesforceMobileSDK plugin ios post-prepare script');
