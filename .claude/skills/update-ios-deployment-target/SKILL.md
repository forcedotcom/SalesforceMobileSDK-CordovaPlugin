# Update iOS Minimum Deployment Target

This skill updates the minimum iOS deployment target for the Salesforce Mobile SDK Cordova Plugin.

## When to Use
- When bumping the minimum supported iOS version for a new SDK release
- Typically done once per major release cycle
- **CRITICAL**: Must be coordinated with and done AFTER iOS SDK and iOS-Hybrid deployment target updates

## What This Skill Does

Updates the iOS deployment target in the Cordova Plugin distribution package:

1. **plugin.xml** - Updates the deployment-target preference for iOS platform
2. **README.md** - Updates minimum iOS version requirements in documentation

**Important Context**: This repository is a **distribution package** that aggregates code from multiple source repositories (Shared, iOS-Hybrid, Android). The iOS native code and build configurations are maintained in the iOS-Hybrid repository. Therefore, this skill only updates the Cordova-specific configuration files, not the underlying iOS code.

## Prerequisites

### CRITICAL: Update Source Repositories First

The following repositories **MUST** be updated with the new deployment target BEFORE updating the Cordova plugin:

1. **SalesforceMobileSDK-iOS** - Core iOS SDK libraries
2. **SalesforceMobileSDK-iOS-Hybrid** - iOS hybrid bridge layer

**Why**: The Cordova plugin references iOS-Hybrid libraries via CocoaPods. The iOS-Hybrid repo contains podspecs and build configurations that define the actual deployment target. The plugin.xml preference in this repo tells Cordova what deployment target to configure, but it must match what the underlying iOS libraries support.

## Usage

When invoked, ask the user for:
- **Current minimum iOS version** (e.g., "17.0")
- **New minimum iOS version** (e.g., "18.0")
- **Confirmation that iOS SDK and iOS-Hybrid are updated** - Verify with user that source repos have been updated first

## Step-by-Step Process

### 1. Update plugin.xml

In `plugin.xml`, locate the iOS platform section and update the deployment-target preference:

```xml
<platform name="ios">
  <config-file target="config.xml" parent="/*">
    <preference name="deployment-target" value="X.0" />
```

Change the value from the old version to the new version (e.g., "17.0" → "18.0").

**Location**: Line ~42 in plugin.xml

### 2. Update README.md

In `README.md`, update all references to the minimum iOS version:

**Section: Platform Requirements table** (~line 210-212):
```markdown
| Platform | Minimum Version | Cordova Version |
|----------|----------------|-----------------|
| **iOS** | X.0 | cordova-ios 7.1.1 |
```

**Section: Prerequisites** (~line 221):
```markdown
- iOS X.0+ device or simulator
```

## Post-Update Tasks

1. **Documentation Review**: Ensure all version references are updated

## Important Notes

- **STOP and FLAG for human review**: This is a significant change that affects all hybrid app consumers
- **Release timing**: Only bump deployment target at major releases, never patches
- **Breaking change**: Document this in migration guide and release notes
- **Source repo dependency**: This repo is a distribution package - iOS-Hybrid MUST be updated first
- **Coordinated release**: This change is part of a cross-repository release process


## Historical References

- Check PR history in forcedotcom/SalesforceMobileSDK-CordovaPlugin for previous deployment target updates
- Related updates in source repositories:
  - iOS SDK: Check SalesforceMobileSDK-iOS for "bump iOS" commits
  - iOS-Hybrid: Check SalesforceMobileSDK-iOS-Hybrid for "bump iOS" commits

## Checklist

Before marking complete:
- [ ] **Confirmed iOS SDK is updated** with new deployment target
- [ ] **Confirmed iOS-Hybrid is updated** with new deployment target
- [ ] plugin.xml deployment-target preference updated
- [ ] README.md updated


## Related Documentation

- **Cordova Plugin README**: `README.md` in this repo
- **iOS SDK Deployment Target Update**: See SalesforceMobileSDK-iOS/.claude/skills/update-ios-deployment-target/
- **iOS-Hybrid Deployment Target Update**: See SalesforceMobileSDK-iOS-Hybrid/.claude/skills/update-ios-deployment-target/
- **Cordova iOS Platform**: https://cordova.apache.org/docs/en/latest/guide/platforms/ios/
- **Mobile SDK Development Guide**: https://developer.salesforce.com/docs/platform/mobile-sdk/guide
