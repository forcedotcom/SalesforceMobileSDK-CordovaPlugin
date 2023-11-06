val hasNodeModules = nodeModulesMsdk.exists() || nodeModulesTemplate.exists()

include("libs:SalesforceAnalytics")
include("libs:SalesforceSDK")
include("libs:SmartStore")
include("libs:MobileSync")
include("libs:SalesforceHybrid")
if (hasNodeModules) {
} else {
}

dependencyResolutionManagement {
    repositories {
        mavenLocal()
        google()
        mavenCentral()
    }
}
