Steps to update ios/android/shared used by plugin
-------------------------------------------------
Git clone SalesforceMobileSDK-Shared, SalesforceMobileSDK-Android and SalesforceMobileSDK-iOS in the same directory as SalesforceMobileSDK-CordovaPlugin
Make sure to run ./install.sh for SalesforceMobileSDK-Android and SalesforceMobileSDK-iOS
Do:

<pre>
./tools/update.sh
</pre>

If you don't want to regenerate the iOS libraries, do:

<pre>
./tools/update.sh nobuild
</pre>
