# Cordova Plugin for Salesforce Mobile SDK

This repository is an **aggregation-only distribution package**. Source code is NOT developed here. The plugin assembles JavaScript from `SalesforceMobileSDK-Shared`, iOS bridge code from `SalesforceMobileSDK-iOS-Hybrid`, iOS resources from `SalesforceMobileSDK-iOS`, and the entire Android SDK from `SalesforceMobileSDK-Android` into a single Cordova plugin distributed via npm.

## Repository Structure

```
SalesforceMobileSDK-CordovaPlugin/
├── www/                              # JavaScript (copied from Shared repo)
│   ├── com.salesforce.plugin.oauth.js
│   ├── com.salesforce.plugin.network.js
│   ├── com.salesforce.plugin.sdkinfo.js
│   ├── com.salesforce.plugin.smartstore.js
│   ├── com.salesforce.plugin.smartstore.client.js
│   ├── com.salesforce.plugin.sfaccountmanager.js
│   ├── com.salesforce.plugin.mobilesync.js
│   ├── com.salesforce.util.bootstrap.js
│   ├── com.salesforce.util.event.js
│   ├── com.salesforce.util.exec.js
│   ├── com.salesforce.util.logger.js
│   ├── com.salesforce.util.promiser.js
│   └── com.salesforce.util.push.js
│
├── src/ios/
│   ├── classes/                      # Objective-C bridge (copied from iOS-Hybrid)
│   │   ├── AppDelegate.m
│   │   ├── InitialViewController.h
│   │   ├── InitialViewController.m
│   │   ├── UIApplication+SalesforceHybridSDK.h
│   │   └── UIApplication+SalesforceHybridSDK.m
│   └── resources/                    # Assets (copied from iOS SDK)
│       ├── Images.xcassets
│       ├── SalesforceSDKAssets.xcassets
│       └── SalesforceSDKResources.bundle
│
├── src/android/libs/mobile_sdk/      # Entire Android repo (pruned)
│   ├── libs/
│   │   ├── SalesforceSDK/
│   │   ├── SmartStore/
│   │   ├── MobileSync/
│   │   └── SalesforceHybrid/
│   ├── settings.gradle.kts
│   └── ...
│
├── plugin.xml                        # Cordova plugin manifest
├── package.json                      # npm package definition
│
├── tools/
│   ├── update.sh                     # Copies source files from upstream repos
│   ├── postinstall-ios.js            # Runs after `cordova plugin add` on iOS
│   └── postinstall-android.js        # Runs after `cordova plugin add` on Android
│
├── gradle/                           # Gradle wrapper (copied from Android)
├── gradlew
├── gradlew.bat
└── gradle.properties
```

## `tools/update.sh` -- The Update Pipeline

This script clones source repositories and copies their files into this repo. It must be run manually after changes in source repos, and is also called automatically by `release/release.js` in the Package repo at release time.

### Usage

```bash
./tools/update.sh -b <branch> -o <ios|android|all>
```

Examples:
```bash
./tools/update.sh -b dev -o all          # Update both platforms from dev
./tools/update.sh -b dev -o ios          # Update only iOS
./tools/update.sh -b v14.0.0 -o all      # Update from a release tag
```

### What it does, step by step

1. **Clones source repos** at the specified branch (shallow, single-branch):
   - `SalesforceMobileSDK-Shared`
   - `SalesforceMobileSDK-iOS-Hybrid`
   - `SalesforceMobileSDK-iOS`
   - `SalesforceMobileSDK-Android`

2. **Wipes both `src/ios/` and `src/android/`** regardless of the `-o` flag, then recreates only the directories matching the requested platform.

3. **Copies iOS bridge code** from iOS-Hybrid:
   - `iOS-Hybrid/shared/hybrid/AppDelegate.m` -> `src/ios/classes/`
   - `iOS-Hybrid/shared/hybrid/UIApplication+SalesforceHybridSDK.{h,m}` -> `src/ios/classes/`
   - `iOS-Hybrid/shared/hybrid/InitialViewController.{h,m}` -> `src/ios/classes/`

4. **Copies iOS resources** from iOS SDK:
   - `iOS/shared/resources/Images.xcassets` -> `src/ios/resources/`
   - `iOS/shared/resources/SalesforceSDKAssets.xcassets` -> `src/ios/resources/`
   - `iOS/shared/resources/SalesforceSDKResources.bundle` -> `src/ios/resources/`

5. **Copies Android SDK**:
   - Copies entire Android repo -> `src/android/libs/mobile_sdk/`
   - Prunes `native/`, `hybrid/`, and `libs/SalesforceReact` directories
   - Removes those pruned modules from `settings.gradle.kts`
   - Removes symlinks (npm cannot handle them)
   - Copies Gradle wrapper files (`gradle.properties`, `gradlew`, `gradlew.bat`, `gradle/`) to the repo root

6. **Copies JavaScript** from Shared:
   - `Shared/gen/plugins/com.salesforce/*.js` -> `www/`

7. **Deletes the cloned temp directories**.

## `plugin.xml` -- The Cordova Manifest

The manifest declares the plugin structure with id `com.salesforce`.

### JavaScript modules

Six plugin modules plus `smartstore.client`, plus six utility modules -- all served from `www/`. The `<clobbers>` element sets global JS names:

```xml
<js-module src="www/com.salesforce.plugin.smartstore.js" name="plugin.smartstore">
  <clobbers target="navigator.smartstore" />
</js-module>
<js-module src="www/com.salesforce.plugin.smartstore.client.js" name="plugin.smartstore.client">
  <clobbers target="navigator.smartstoreClient" />
</js-module>
```

### Engine requirements

```xml
<engines>
  <engine name="cordova-ios" version="7.1.1" />
  <engine name="cordova-android" version="15.0.0" />
</engines>
```

### iOS platform section

- **`<feature>` entries** map service names to Objective-C classes:
  ```xml
  <feature name="com.salesforce.oauth"><param name="ios-package" value="SalesforceOAuthPlugin"/></feature>
  <feature name="com.salesforce.smartstore"><param name="ios-package" value="SFSmartStorePlugin"/></feature>
  ```

- **`<podspec>` block** pulls `SalesforceHybridSDK` from iOS-Hybrid and the full iOS SDK pod chain (`MobileSync`, `SmartStore`, `SalesforceSDKCore`, `SalesforceAnalytics`, `SalesforceSDKCommon`) from GitHub:
  ```xml
  <podspec>
    <pods use-frameworks="true">
      <pod name="SalesforceHybridSDK" git="https://github.com/forcedotcom/SalesforceMobileSDK-iOS-Hybrid" branch="dev" />
      <pod name="MobileSync" git="https://github.com/forcedotcom/SalesforceMobileSDK-iOS" branch="dev" />
      ...
    </pods>
  </podspec>
  ```

- **Source and resource files** from `src/ios/`.

- **Post-install hook**: `tools/postinstall-ios.js`.

### Android platform section

- **`<feature>` entries** map service names to Kotlin/Java classes:
  ```xml
  <feature name="com.salesforce.oauth"><param name="android-package" value="com.salesforce.androidsdk.phonegap.plugin.SalesforceOAuthPlugin"/></feature>
  <feature name="com.salesforce.smartstore"><param name="android-package" value="com.salesforce.androidsdk.phonegap.plugin.SmartStorePlugin"/></feature>
  ```

- **Manifest edits** set `SalesforceDroidGapActivity` as the main activity and `HybridApp` as the application class.

- **Gradle/Kotlin version preferences**:
  ```xml
  <preference name="GradleVersion" value="9.4.1" />
  <preference name="AndroidGradlePluginVersion" value="9.1.1"/>
  <preference name="GradlePluginKotlinEnabled" value="true" />
  <preference name="GradlePluginKotlinVersion" value="2.1.21" />
  ```

- **Post-install hook**: `tools/postinstall-android.js`.

## Post-Install Hooks

These scripts run automatically when a developer executes `cordova plugin add com.salesforce`.

### `postinstall-ios.js`

Patches `project.pbxproj` to redirect `AppDelegate.m` to `Plugins/com.salesforce/AppDelegate.m`, so the SDK's bootstrap app delegate is used instead of the default Cordova stub:

```javascript
replaceTextInFile(projectFile,
    'path = AppDelegate.m;',
    'name = AppDelegate.m; path = Plugins/com.salesforce/AppDelegate.m;');
```

### `postinstall-android.js`

Performs four operations on the generated Cordova Android project:

1. **Fixes `CordovaLib/cordova.gradle`** -- Adds a missing `import groovy.xml.XmlParser` (workaround for a cordova-android 15.0.0 bug).

2. **Patches `settings.gradle`** -- Removes `include ":CordovaLib"` and adds an `includeBuild` pointing to `mobile_sdk/SalesforceMobileSDK-Android`, so the Android SDK is consumed as a composite build instead of CordovaLib.

3. **Patches `app/build.gradle`** -- Adds packaging exclusions for duplicate META-INF files, and replaces `implementation(project(path: ":CordovaLib"))` with `api 'com.salesforce.mobilesdk:SalesforceHybrid:14.0.0'`.

4. **Copies Gradle wrapper files** (`gradle.properties`, `gradlew`, `gradlew.bat`, `gradle/`) from the plugin into the app project directory.

## What NOT to Edit Here

| Path | Source repo | Go there for changes |
|------|-------------|---------------------|
| `www/*.js` | SalesforceMobileSDK-Shared | `gen/plugins/com.salesforce/` |
| `src/ios/classes/` | SalesforceMobileSDK-iOS-Hybrid | `shared/hybrid/` |
| `src/ios/resources/` | SalesforceMobileSDK-iOS | `shared/resources/` |
| `src/android/` | SalesforceMobileSDK-Android | (entire repo) |

Only edit these files directly in this repository:
- `plugin.xml`
- `tools/update.sh`, `tools/postinstall-ios.js`, `tools/postinstall-android.js`
- `package.json`

## Release Workflow

1. Run the update script against the release tag:
   ```bash
   ./tools/update.sh -b v14.0.0 -o all
   ```

2. Verify copied files look correct (diff against prior release).

3. Run `setversion.sh` to update the version number across all files:
   ```bash
   # On dev branch (pre-release):
   ./setversion.sh -v 14.0.0 -d yes
   # On master branch (after merging dev → master at release):
   ./setversion.sh -v 14.0.0 -d no
   ```
   This updates `package.json`, bumps `SalesforceHybrid:<version>` in `postinstall-android.js`, and switches `plugin.xml` pod references between `branch="dev"` and `tag="v14.0.0"`.

4. Commit, tag, push:
   ```bash
   git add .
   git commit -m "Release v14.0.0"
   git tag v14.0.0
   git push origin dev
   git push origin v14.0.0
   ```

5. Publish:
   ```bash
   npm publish
   ```

## Related Repositories

- [SalesforceMobileSDK-Shared](https://github.com/forcedotcom/SalesforceMobileSDK-Shared) -- JavaScript source
- [SalesforceMobileSDK-iOS-Hybrid](https://github.com/forcedotcom/SalesforceMobileSDK-iOS-Hybrid) -- iOS bridge source
- [SalesforceMobileSDK-iOS](https://github.com/forcedotcom/SalesforceMobileSDK-iOS) -- iOS SDK
- [SalesforceMobileSDK-Android](https://github.com/forcedotcom/SalesforceMobileSDK-Android) -- Android SDK
- [SalesforceMobileSDK-Package](https://github.com/forcedotcom/SalesforceMobileSDK-Package) -- CLI tools and release automation
- [SalesforceMobileSDK-Templates](https://github.com/forcedotcom/SalesforceMobileSDK-Templates) -- App templates that consume this plugin

## License

Salesforce Mobile SDK License. See [LICENSE.md](LICENSE.md) for details.
