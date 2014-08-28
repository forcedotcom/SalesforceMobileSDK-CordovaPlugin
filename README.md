Steps to use that plugin
------------------------

<pre>
sudo npm install -g cordova

cordova create TestApp
cd TestApp
cordova plugin add https://github.com/wmathurin/SalesforceMobileSDK-CordovaPlugin
cordova platform add android
node ./plugins/com.salesforce/tools/postinstall-android.js
cordova platform add ios
cordova build
</pre>
