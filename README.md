Steps to use the Cordova plugin for the Salesforce Mobile SDK
------------------------

<pre>
npm install cordova -g

cordova create TestApp
cd TestApp
cordova plugin add https://github.com/forcedotcom/SalesforceMobileSDK-CordovaPlugin
cordova platform add android
node ./plugins/com.salesforce/tools/postinstall-android.js
cordova platform add ios
cordova build
</pre>
