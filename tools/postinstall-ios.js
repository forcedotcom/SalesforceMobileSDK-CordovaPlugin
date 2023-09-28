console.log('Running SalesforceMobileSDK plugin ios post-install script');

const fs = require('fs');
const path = require('path');

//--------------------------------------
// Doing actual post installation work
//--------------------------------------
const classesRoot = path.join('plugins', 'com.salesforce', 'src', 'ios', 'classes');
const appProjectRoot = path.join('platforms', 'ios');
const appName = path.parse(fs.readdirSync(appProjectRoot).filter(f=>f.endsWith('.xcworkspace'))[0]).name

console.log('Moving AppDelegate.m to the correct location');
fs.copyFile(path.join(classesRoot, 'AppDelegate.m'),
	    path.join(appProjectRoot, appName, 'AppDelegate.m'),
	    err => {
		if (err) {
		    console.error(`Error copying file: ${err}`)
		    process.exit(1);
		}
	    }
	   );

console.log('Done running SalesforceMobileSDK plugin ios post-install script');
