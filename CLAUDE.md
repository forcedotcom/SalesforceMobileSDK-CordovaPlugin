# CLAUDE.md — Salesforce Mobile SDK Cordova Plugin

---

## About This Project

The Salesforce Mobile SDK Cordova Plugin is the **distribution package** for Cordova-based hybrid applications. It aggregates JavaScript, iOS native code, and Android native code from multiple source repositories into a single Cordova plugin that can be installed via npm or Cordova CLI.

**Key constraint**: This is a **public npm package** and the primary distribution mechanism for hybrid apps. Every change impacts thousands of external developers. Backward compatibility and semver discipline are critical.

## Repository Role in SDK Architecture

This repository serves as the **aggregation and distribution point**:

```
Source Repositories:
  ├── SalesforceMobileSDK-Shared (JavaScript)
  ├── SalesforceMobileSDK-iOS-Hybrid (iOS bridge)
  ├── SalesforceMobileSDK-iOS (iOS SDK)
  └── SalesforceMobileSDK-Android (Android bridge + SDK)
           │
           ▼
    tools/update.sh (copies files)
           │
           ▼
SalesforceMobileSDK-CordovaPlugin (this repo)
  ├── www/ (JavaScript from Shared)
  ├── src/ios/ (iOS bridge from iOS-Hybrid + iOS SDK)
  ├── src/android/ (entire Android repo as subproject)
  └── plugin.xml (Cordova plugin definition)
           │
           ▼
       npm publish
           │
           ▼
    Hybrid Templates
    (consume via npm/Cordova)
```

## Repository Structure

```
SalesforceMobileSDK-CordovaPlugin/
├── plugin.xml                        # Cordova plugin manifest
├── package.json                      # npm package definition
│
├── www/                              # JavaScript (copied from Shared)
│   ├── com.salesforce.plugin.oauth.js
│   ├── com.salesforce.plugin.smartstore.js
│   ├── com.salesforce.plugin.mobilesync.js
│   ├── com.salesforce.plugin.network.js
│   ├── com.salesforce.plugin.sdkinfo.js
│   ├── com.salesforce.plugin.sfaccountmanager.js
│   ├── com.salesforce.util.bootstrap.js
│   ├── com.salesforce.util.event.js
│   ├── com.salesforce.util.exec.js
│   ├── com.salesforce.util.logger.js
│   ├── com.salesforce.util.promiser.js
│   └── com.salesforce.util.push.js
│
├── src/
│   ├── ios/                          # iOS implementation
│   │   ├── classes/                  # Copied from iOS-Hybrid
│   │   │   ├── AppDelegate.m
│   │   │   ├── InitialViewController.{h,m}
│   │   │   └── UIApplication+SalesforceHybridSDK.{h,m}
│   │   └── resources/                # Copied from iOS SDK
│   │       ├── Images.xcassets
│   │       ├── SalesforceSDKAssets.xcassets
│   │       └── SalesforceSDKResources.bundle
│   │
│   └── android/                      # Android implementation
│       └── libs/mobile_sdk/          # Entire Android repo copied
│           ├── libs/                 # Android libraries
│           │   ├── SalesforceSDK/
│           │   ├── SmartStore/
│           │   ├── MobileSync/
│           │   └── SalesforceHybrid/ # Android hybrid bridge
│           ├── gradle/
│           ├── gradlew
│           └── settings.gradle.kts
│
├── tools/                            # Build and update scripts
│   ├── update.sh                     # Main update script (copies from source repos)
│   ├── postinstall-ios.js            # iOS post-install hook
│   └── postinstall-android.js        # Android post-install hook
│
├── gradle/                           # Gradle wrapper (copied from Android)
├── gradlew                           # Gradle wrapper script
└── gradle.properties                 # Gradle properties
```

## Update Process

### The `tools/update.sh` Script

This script is the **heart of the distribution process**. It clones source repositories and copies files to this repo:

**Usage**:
```bash
./tools/update.sh -b <branch> -o <platform>

# Examples:
./tools/update.sh -b dev -o all       # Update both platforms from dev branch
./tools/update.sh -b main -o ios      # Update only iOS from main branch
./tools/update.sh -b v14.0.0 -o android  # Update Android from tagged release
```

**What it does**:

1. **Clones source repositories** (if not already present):
   - `SalesforceMobileSDK-Shared`
   - `SalesforceMobileSDK-iOS-Hybrid`
   - `SalesforceMobileSDK-iOS`
   - `SalesforceMobileSDK-Android`

2. **Copies JavaScript** from Shared repo:
   - `Shared/gen/plugins/com.salesforce/*.js` → `www/`

3. **Copies iOS code**:
   - iOS-Hybrid: `shared/hybrid/*.{h,m}` → `src/ios/classes/`
   - iOS SDK: `shared/resources/*` → `src/ios/resources/`

4. **Copies Android code**:
   - Entire Android repo → `src/android/libs/mobile_sdk/`
   - Prunes sample apps and React Native libraries
   - Removes symbolic links (npm doesn't handle them well)
   - Copies Gradle wrapper files to root

**Important**: This script is run manually by maintainers during the release process, not automatically on every change.

## plugin.xml — Cordova Plugin Manifest

The `plugin.xml` file defines:

### Plugin Metadata
```xml
<plugin id="com.salesforce" version="14.0.0">
  <name>SalesforceMobileSDK Plugins</name>
  <description>SalesforceMobileSDK Plugins</description>
  <keywords>salesforce,oauth,smartstore,mobilesync</keywords>
</plugin>
```

### JavaScript Modules
Maps JavaScript files to Cordova namespaces:
```xml
<js-module src="www/com.salesforce.plugin.oauth.js" name="plugin.oauth">
<js-module src="www/com.salesforce.plugin.smartstore.js" name="plugin.smartstore">
  <clobbers target="navigator.smartstore" />
</js-module>
```

### iOS Platform Configuration
```xml
<platform name="ios">
  <!-- Cordova plugins → Native classes -->
  <feature name="com.salesforce.oauth">
    <param name="ios-package" value="SalesforceOAuthPlugin"/>
  </feature>

  <!-- CocoaPods dependencies -->
  <podspec>
    <pods use-frameworks="true">
      <pod name="SalesforceHybridSDK" git="..." branch="dev" />
      <pod name="MobileSync" git="..." branch="dev" />
      <!-- ... -->
    </pods>
  </podspec>

  <!-- Source files and resources -->
  <source-file src="src/ios/classes/AppDelegate.m" />
  <resource-file src="src/ios/resources/..." />

  <!-- Post-install hook -->
  <hook type="after_plugin_install" src="tools/postinstall-ios.js" />
</platform>
```

### Android Platform Configuration
```xml
<platform name="android">
  <!-- Gradle and SDK versions -->
  <preference name="android-minSdkVersion" value="31" />
  <preference name="android-targetSdkVersion" value="36" />
  <preference name="GradleVersion" value="9.4.1" />
  <preference name="AndroidGradlePluginVersion" value="9.1.1"/>

  <!-- Cordova plugins → Native classes -->
  <feature name="com.salesforce.oauth">
    <param name="android-package" value="com.salesforce.androidsdk.phonegap.plugin.SalesforceOAuthPlugin"/>
  </feature>

  <!-- Manifest modifications -->
  <edit-config file="app/src/main/AndroidManifest.xml" target="/manifest/application/activity">
    <activity android:name="com.salesforce.androidsdk.phonegap.ui.SalesforceDroidGapActivity" .../>
  </edit-config>

  <!-- Post-install hook -->
  <hook type="after_plugin_install" src="tools/postinstall-android.js" />
</platform>
```

## Post-Install Hooks

### iOS (`tools/postinstall-ios.js`)
Runs after plugin installation:
- Modifies Xcode project settings
- Configures app transport security
- Sets up entitlements for push notifications
- Configures build settings for Cordova

### Android (`tools/postinstall-android.js`)
Runs after plugin installation:
- Configures Gradle build
- Sets up Android SDK paths
- Modifies AndroidManifest.xml
- Configures ProGuard rules

## Development Workflow

### Normal Development (Not This Repo)

**Important**: You typically **do not** make changes directly in this repo. Instead:

1. **Make changes in source repos**:
   - JavaScript changes → `SalesforceMobileSDK-Shared`
   - iOS bridge changes → `SalesforceMobileSDK-iOS-Hybrid`
   - Android bridge changes → `SalesforceMobileSDK-Android`

2. **Test in source repos**:
   - Run tests in Shared, iOS-Hybrid, or Android repos
   - Test with sample apps in those repos

3. **During release, update this repo**:
   - Run `./tools/update.sh -b <branch> -o all`
   - Commit copied files
   - Tag and publish to npm

### Testing the Plugin Locally

To test the assembled plugin before publishing:

```bash
# In a test Cordova project
cordova plugin add /path/to/SalesforceMobileSDK-CordovaPlugin

# Or use the Templates repo test_template.sh script
cd SalesforceMobileSDK-Templates
./test_template.sh --template HybridLocalTemplate
```

### Rare Direct Changes

Occasionally, you might need to modify files in this repo directly:

**When to modify this repo**:
- `plugin.xml` - Only for Cordova configuration changes
- `package.json` - Only for npm metadata
- `tools/postinstall-*.js` - Only for install hook logic
- `README.md` - Documentation updates

**When NOT to modify this repo**:
- `www/*.js` - These are copied from Shared
- `src/ios/` - These are copied from iOS-Hybrid and iOS SDK
- `src/android/` - This is copied from Android repo

If you find yourself wanting to modify `www/` or `src/`, you're in the wrong repo — go to the source repo instead.

## Code Standards

Since this is primarily a distribution repo:

### For plugin.xml
- **Valid XML**: Ensure well-formed XML
- **Version consistency**: Version should match iOS SDK, Android SDK, and Shared
- **Cordova compatibility**: Follow Cordova plugin specification
- **Both platforms**: iOS and Android configurations must be in sync

### For Post-Install Scripts
- **Error handling**: Gracefully handle missing files or configurations
- **Cross-platform**: Consider macOS, Windows, Linux
- **Logging**: Provide clear output for debugging
- **Idempotent**: Safe to run multiple times

### For Source Code (if modified)
- Follow standards from the source repository (Shared, iOS-Hybrid, or Android)

## Testing Standards

### Integration Testing

The primary test is **does the plugin install and work in a Cordova app**:

1. **Template Generation**: Use Templates repo to generate hybrid apps
2. **Build Test**: Ensure apps build on iOS and Android
3. **Runtime Test**: Verify plugins work on device/simulator
4. **Feature Test**: Test OAuth, SmartStore, MobileSync functionality

### Automated Testing

- **Nightly builds**: GitHub Actions test plugin installation and build
- **Template tests**: `test_template.sh` in Templates repo (not yet active for hybrid)
- **Source repo tests**: Unit tests in Shared, iOS-Hybrid, and Android repos

## Release Process

This repo follows a specific release workflow:

### Pre-Release
1. **Update source repos**: Merge and tag Shared, iOS-Hybrid, iOS, Android repos
2. **Update submodules**: Ensure iOS-Hybrid and Android have updated Shared submodules

### Release
1. **Run update script**:
```bash
./tools/update.sh -b v13.2.0 -o all
```

2. **Verify changes**:
   - Check that `www/` has latest JavaScript
   - Check that `src/ios/` has latest iOS code
   - Check that `src/android/` has latest Android code
   - Verify version numbers in all files

3. **Update plugin.xml**:
   - Bump version number
   - Update Cordova engine requirements if needed
   - Update pod/gradle dependency versions

4. **Update package.json**:
   - Bump version number to match

5. **Test locally**:
   - Test plugin installation in a test Cordova project
   - Build and run on iOS and Android

6. **Commit and tag**:
```bash
git add .
git commit -m "Release v14.0.0"
git tag v14.0.0
git push origin dev
git push origin v14.0.0
```

7. **Publish to npm**:
```bash
npm publish
```

### Post-Release
1. **Update Templates**: Templates repo should reference new npm version
2. **Update documentation**: Release notes, migration guides
3. **Announce**: Developer community, blog post, etc.

## Version Compatibility

| Cordova Plugin | Shared | iOS Hybrid | Android | Cordova iOS | Cordova Android |
|---------------|--------|------------|---------|-------------|-----------------|
| 14.0.0        | 14.0.0 | 14.0.0     | 14.0.0  | 7.1.1       | 15.0.0          |
| 13.2.0        | 13.2.0 | 13.2.0     | 13.2.0  | 7.1.1       | 14.0.1          |
| 13.1.0        | 13.1.0 | 13.1.0     | 13.1.0  | 7.1.0       | 13.0.0          |
| 13.0.0        | 13.0.0 | 13.0.0     | 13.0.0  | 7.1.0       | 13.0.0          |

## Code Review Checklist

When reviewing PRs (rare in this repo):

- [ ] **Source repo changes first**: Has the source repo been updated?
- [ ] **Update script run**: Was `tools/update.sh` used to copy files?
- [ ] **Both platforms**: Are iOS and Android both updated?
- [ ] **Version consistency**: Do all version numbers match?
- [ ] **plugin.xml valid**: Is the XML well-formed and complete?
- [ ] **No manual edits to copied files**: Are `www/` and `src/` untouched?
- [ ] **Test before merge**: Has the plugin been tested in a real app?
- [ ] **Documentation**: README and changelog updated?

## Agent Behavior Guidelines

### Do
- Understand this is primarily a distribution repo
- Direct users to source repos (Shared, iOS-Hybrid, Android) for code changes
- Help with plugin.xml configuration and structure
- Help with post-install hook scripts
- Verify version consistency across all files
- Test plugin installation locally before releasing

### Don't
- Don't modify `www/` directly (modify Shared repo instead)
- Don't modify `src/ios/` directly (modify iOS-Hybrid or iOS repos instead)
- Don't modify `src/android/` directly (modify Android repo instead)
- Don't publish to npm without human approval
- Don't modify update script without understanding the full workflow
- Don't merge without testing plugin installation

### Escalation — Stop and Flag for Human Review
- Any npm publish operation
- Changes to `plugin.xml` structure or dependencies
- Modification of post-install hooks
- Changes to `tools/update.sh` script
- Dependency version bumps (Cordova, pods, gradle)
- New Cordova engine requirements
- Removal of any previously supported Cordova version

## Key Domain Concepts

- **Cordova Plugin**: npm package following Apache Cordova plugin specification
- **plugin.xml**: Manifest defining plugin structure, dependencies, and platform-specific configurations
- **Post-Install Hook**: Script that runs after `cordova plugin add` to configure the app project
- **Platform Tag**: XML section in plugin.xml defining platform-specific (iOS/Android) configuration
- **CocoaPods Integration**: Cordova's mechanism for installing iOS dependencies
- **Gradle Sub-Project**: Android's mechanism for including SDK libraries

## Related Documentation

- **Mobile SDK Development Guide**: https://developer.salesforce.com/docs/platform/mobile-sdk/guide
- **Shared JavaScript**: See `SalesforceMobileSDK-Shared/CLAUDE.md`
- **iOS Hybrid**: See `SalesforceMobileSDK-iOS-Hybrid/CLAUDE.md`
- **Android**: See `SalesforceMobileSDK-Android/CLAUDE.md` (libs/SalesforceHybrid)
- **Cordova Plugin Specification**: https://cordova.apache.org/docs/en/latest/plugin_ref/spec.html
- **npm Package**: https://www.npmjs.com/package/salesforce-mobilesdk-cordova-plugin
