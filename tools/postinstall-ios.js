console.log('Running SalesforceMobileSDK plugin ios post-install script');

const fs = require('fs');
const path = require('path');

const appProjectRoot = path.join('platforms', 'ios');
const appName = path.parse(fs.readdirSync(appProjectRoot).filter(f=>f.endsWith('.xcworkspace'))[0]).name
const pluginAppDelegate = path.join('plugins', 'com.salesforce', 'src', 'ios', 'classes', 'AppDelegate.m');
const appAppDelegate = path.join(appProjectRoot, appName, 'AppDelegate.m')

console.log('Moving AppDelegate.m to the correct location');
fs.copyFile(pluginAppDelegate, appAppDelegate,
	    err => {
		if (err) {
		    console.error(`Error copying file: ${err}`)
		    process.exit(1);
		}
	    }
	   );

fs.rmSync(pluginAppDelegate)

console.log('Done running SalesforceMobileSDK plugin ios post-install script');
