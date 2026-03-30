# Cordova Plugin for Salesforce Mobile SDK

The **Salesforce Mobile SDK Cordova Plugin** enables developers to build hybrid mobile applications that integrate with the Salesforce Platform using Apache Cordova (PhoneGap).

## Overview

This plugin provides JavaScript APIs that bridge to native iOS and Android implementations of the Salesforce Mobile SDK. It allows hybrid apps built with HTML, CSS, and JavaScript to leverage:

- **OAuth Authentication** - Salesforce login and user management
- **SmartStore** - Encrypted local storage (SQLCipher-backed)
- **MobileSync** - Bidirectional data synchronization
- **REST API** - Access to Salesforce APIs
- **Multi-User Support** - Account switching and management

## Installation

### Recommended: Use forcehybrid CLI

The easiest way to create a Salesforce hybrid app is using the [forcehybrid](https://npmjs.org/package/forcehybrid) command-line tool:

```bash
# Install the CLI
npm install -g forcehybrid

# Create a new hybrid app
forcehybrid create
    --platform ios,android
    --appname MyHybridApp
    --packagename com.mycompany.myhybridapp
    --organization "My Company"
```

This generates a complete Cordova app with the Salesforce Mobile SDK plugin pre-installed and configured.

### Manual Installation

If you need to add the plugin to an existing Cordova project:

```bash
# From npm
cordova plugin add salesforce-mobilesdk-cordova-plugin

# From GitHub
cordova plugin add https://github.com/forcedotcom/SalesforceMobileSDK-CordovaPlugin.git

# From local directory
cordova plugin add /path/to/SalesforceMobileSDK-CordovaPlugin
```

**Note**: Manual installation requires additional configuration steps. We strongly recommend using the forcehybrid CLI.

## Features

### OAuth & Authentication

Authenticate users with Salesforce and manage sessions:

```javascript
// Get authenticated user credentials
com.salesforce.plugin.oauth.getAuthCredentials(
    function(credentials) {
        console.log('Access Token:', credentials.accessToken);
        console.log('Instance URL:', credentials.instanceUrl);
        console.log('User ID:', credentials.userId);
    },
    function(error) {
        console.error('Authentication error:', error);
    }
);

// Logout
com.salesforce.plugin.oauth.logout();
```

### SmartStore - Encrypted Storage

Store data securely on the device with SQLCipher encryption:

```javascript
// Register a soup (table)
navigator.smartstore.registerSoup(
    false,  // isGlobalStore
    'contacts',
    [
        {path: 'Id', type: 'string'},
        {path: 'Name', type: 'string'},
        {path: 'LastModifiedDate', type: 'string'}
    ],
    function() {
        console.log('Soup registered');
    },
    function(error) {
        console.error('Registration error:', error);
    }
);

// Query the soup
var querySpec = navigator.smartstore.buildExactQuerySpec('Id', '003...', 10);
navigator.smartstore.querySoup(
    false,
    'contacts',
    querySpec,
    function(cursor) {
        console.log('Found', cursor.totalEntries, 'entries');
    },
    function(error) {
        console.error('Query error:', error);
    }
);

// Use Smart SQL
var smartSql = 'SELECT {contacts:Name} FROM {contacts} ORDER BY {contacts:Name}';
navigator.smartstore.runSmartQuery(
    false,
    smartSql,
    function(cursor) {
        console.log('Query results:', cursor.currentPageOrderedEntries);
    },
    function(error) {
        console.error('Smart SQL error:', error);
    }
);
```

### MobileSync - Data Synchronization

Sync data between SmartStore and Salesforce:

```javascript
// Sync down from Salesforce
var target = {
    type: 'soql',
    query: 'SELECT Id, Name, Industry FROM Account'
};

com.salesforce.plugin.mobilesync.syncDown(
    false,  // isGlobalStore
    target,
    'accounts',
    {},
    'accountSync',
    function(sync) {
        if (sync.status === 'DONE') {
            console.log('Sync complete');
        }
    },
    function(error) {
        console.error('Sync error:', error);
    }
);

// Sync up to Salesforce
var syncUpTarget = {type: 'syncUp'};

com.salesforce.plugin.mobilesync.syncUp(
    false,
    syncUpTarget,
    'accounts',
    {},
    'accountSyncUp',
    function(sync) {
        if (sync.status === 'DONE') {
            console.log('Sync up complete');
        }
    },
    function(error) {
        console.error('Sync up error:', error);
    }
);
```

### REST API

Make REST API calls to Salesforce:

```javascript
// SOQL Query
com.salesforce.plugin.network.sendRequest(
    '/services/data/v56.0/query/',
    'SELECT Id, Name FROM Account LIMIT 10',
    function(response) {
        var accounts = JSON.parse(response).records;
        console.log('Accounts:', accounts);
    },
    function(error) {
        console.error('Query error:', error);
    },
    'GET'
);

// Create a record
var newAccount = {Name: 'Acme Corp', Industry: 'Technology'};
com.salesforce.plugin.network.sendRequest(
    '/services/data/v56.0/sobjects/Account/',
    JSON.stringify(newAccount),
    function(response) {
        var result = JSON.parse(response);
        console.log('Created account:', result.id);
    },
    function(error) {
        console.error('Create error:', error);
    },
    'POST',
    'application/json'
);
```

## Platform Support

| Platform | Minimum Version | Cordova Version |
|----------|----------------|-----------------|
| **iOS** | 17.0 | cordova-ios 7.1.1 |
| **Android** | API 28 (Android 9.0) | cordova-android 14.0.1 |

## Requirements

### For iOS Development
- macOS
- Xcode 15+
- CocoaPods
- iOS 17.0+ device or simulator

### For Android Development
- Java 17+
- Android Studio
- Android SDK (API 28+)
- Gradle 8.14.3

## Plugin Architecture

This plugin is an **aggregation point** that combines code from multiple repositories:

```
Source Repositories:
  - SalesforceMobileSDK-Shared (JavaScript)
  - SalesforceMobileSDK-iOS-Hybrid (iOS native bridge)
  - SalesforceMobileSDK-Android (Android native bridge)
           ↓
    Cordova Plugin (this repo)
           ↓
        npm Package
           ↓
     Hybrid Applications
```

### JavaScript Layer (`www/`)
- OAuth plugin
- SmartStore plugin
- MobileSync plugin
- Network (REST API) plugin
- SDKInfo plugin
- Account Manager plugin
- Utility modules (bootstrap, event, logger, promiser, push)

### iOS Implementation (`src/ios/`)
- Cordova plugin bridges (Objective-C)
- App delegate template
- View controller setup
- Resource bundles
- CocoaPods integration for iOS SDK

### Android Implementation (`src/android/`)
- Cordova plugin bridges (Kotlin)
- Activity setup
- Gradle integration for Android SDK
- Android Manifest configuration

## Configuration

### External Client App Setup

To use this plugin, you need a Salesforce External Client App (ECA):

1. Go to **Setup** → **Apps** → **External Client Apps** in Salesforce
2. Click **New External Client App**
3. Fill in the required fields:
   - **External Client App Name**: Your app name
   - **Description**: Brief description of your app
   - **Contact Email**: Your email
4. Under **API Integration**:
   - Enable **OAuth 2.0 Enabled**
   - Set **Callback URL**: `sfdc://oauth/success` (for mobile apps)
   - Select **OAuth Scopes**:
     - Access and manage your data (api)
     - Manage user data via Web browsers (web)
     - Perform requests on your behalf at any time (refresh_token, offline_access)
5. Save and copy the **Consumer Key**

**Note**: External Client Apps replace the legacy Connected App model. For more information, see the [External Client Apps documentation](https://help.salesforce.com/s/articleView?id=platform.hosted_mcp_servers_eca.htm&type=5).

### App Configuration

Configure your app's `bootconfig.json`:

```json
{
  "remoteAccessConsumerKey": "YOUR_CONSUMER_KEY",
  "oauthRedirectURI": "sfdc://oauth/success",
  "oauthScopes": [
    "api",
    "web",
    "refresh_token"
  ],
  "isLocal": true,
  "startPage": "index.html",
  "errorPage": "error.html",
  "shouldAuthenticate": true,
  "attemptOfflineLoad": false
}
```

## Documentation

### API Reference

For detailed API documentation, see:
- **JavaScript APIs**: See comments in `www/` directory files
- **iOS Native**: https://forcedotcom.github.io/SalesforceMobileSDK-iOS
- **Android Native**: https://forcedotcom.github.io/SalesforceMobileSDK-Android

### Developer Guides
- **Mobile SDK Development Guide**: https://developer.salesforce.com/docs/platform/mobile-sdk/guide
- **Mobile SDK Trail**: https://trailhead.salesforce.com/trails/mobile_sdk_intro
- **Cordova Documentation**: https://cordova.apache.org/docs/

## Sample Apps

Sample applications are available in related repositories:

- **AccountEditor**: Basic CRUD operations (in iOS-Hybrid and Android repos)
- **MobileSyncExplorerHybrid**: Complete offline sync demo (in iOS-Hybrid and Android repos)
- **Sample code**: Various samples in the Shared repository

## Version Compatibility

| Plugin Version | iOS SDK | Android SDK | Cordova iOS | Cordova Android |
|---------------|---------|-------------|-------------|-----------------|
| 13.2.0        | 13.2.0  | 13.2.0      | 7.1.1       | 14.0.1          |
| 13.1.0        | 13.1.0  | 13.1.0      | 7.1.0       | 13.0.0          |
| 13.0.0        | 13.0.0  | 13.0.0      | 7.1.0       | 13.0.0          |

See [release notes](https://github.com/forcedotcom/SalesforceMobileSDK-CordovaPlugin/releases) for version details.

## Related Tools & Packages

### CLI Tools
- **forcehybrid**: https://npmjs.org/package/forcehybrid - Create hybrid apps
- **forceios**: https://npmjs.org/package/forceios - Create native iOS apps
- **forcedroid**: https://npmjs.org/package/forcedroid - Create native Android apps
- **forcereactnative**: https://npmjs.org/package/forcereactnative - Create React Native apps

### Related Repositories
- **Shared JavaScript**: https://github.com/forcedotcom/SalesforceMobileSDK-Shared
- **iOS Hybrid**: https://github.com/forcedotcom/SalesforceMobileSDK-iOS-Hybrid
- **iOS SDK**: https://github.com/forcedotcom/SalesforceMobileSDK-iOS
- **Android SDK**: https://github.com/forcedotcom/SalesforceMobileSDK-Android
- **Templates**: https://github.com/forcedotcom/SalesforceMobileSDK-Templates
- **Package/CLI**: https://github.com/forcedotcom/SalesforceMobileSDK-Package

## Support

### Getting Help
- **Issues**: [GitHub Issues](https://github.com/forcedotcom/SalesforceMobileSDK-CordovaPlugin/issues)
- **Questions**: [Salesforce Stack Exchange](https://salesforce.stackexchange.com/questions/tagged/mobilesdk)
- **Community**: [Trailblazer Community](https://trailhead.salesforce.com/trailblazer-community/groups/0F94S000000kH0HSAU)

### Troubleshooting

**iOS Build Errors**:
- Make sure you're using Xcode 15+
- Run `pod install` in the `platforms/ios` directory
- Clean build folder in Xcode (Cmd+Shift+K)

**Android Build Errors**:
- Verify Java 17+ is installed
- Check Gradle version compatibility
- Run `./gradlew clean` in the `platforms/android` directory

**Plugin Installation Issues**:
- Use forcehybrid CLI instead of manual installation
- Remove and re-add the plugin
- Check Cordova and platform versions

## Contributing

We welcome contributions! Please:
1. Read the [CLAUDE.md](CLAUDE.md) file for development guidelines
2. Understand this is primarily a distribution repo (source code lives elsewhere)
3. For JavaScript changes: Contribute to [SalesforceMobileSDK-Shared](https://github.com/forcedotcom/SalesforceMobileSDK-Shared)
4. For iOS changes: Contribute to [SalesforceMobileSDK-iOS-Hybrid](https://github.com/forcedotcom/SalesforceMobileSDK-iOS-Hybrid)
5. For Android changes: Contribute to [SalesforceMobileSDK-Android](https://github.com/forcedotcom/SalesforceMobileSDK-Android)
6. For plugin configuration: Create issues or PRs in this repository

## License

Salesforce Mobile SDK License. See [LICENSE.md](LICENSE.md) for details.

## Security

Please report security vulnerabilities to [security@salesforce.com](mailto:security@salesforce.com).
