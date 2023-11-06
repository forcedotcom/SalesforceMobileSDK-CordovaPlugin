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


console.log('Pointing to AppDelegate.m defined in plugin');
const appProjectRoot = path.join('platforms', 'ios');
const appName = path.parse(fs.readdirSync(appProjectRoot).filter(f=>f.endsWith('.xcworkspace'))[0]).name;
const projectFile = path.join(appProjectRoot, `${appName}.xcodeproj`, 'project.pbxproj');
replaceTextInFile(projectFile, 'path = AppDelegate.m;', 'name = AppDelegate.m; path = Plugins/com.salesforce/AppDelegate.m;');

console.log('Done running SalesforceMobileSDK plugin ios post-install script');




