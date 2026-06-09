/*
 Copyright (c) 2025-present, salesforce.com, inc. All rights reserved.

 Redistribution and use of this software in source and binary forms, with or without modification,
 are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions
 and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of
 conditions and the following disclaimer in the documentation and/or other materials provided
 with the distribution.
 * Neither the name of salesforce.com, inc. nor the names of its contributors may be used to
 endorse or promote products derived from this software without specific prior written
 permission of salesforce.com, inc.

 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
 WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

import UIKit
import SalesforceHybridSDK
import SalesforceSDKCore

#if compiler(>=6.1)
@objc @implementation
#else
@_objcImplementation
#endif
extension AppDelegate {

    public override func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        // Initialize SalesforceHybridSDKManager
        SalesforceHybridSDKManager.initializeSDK()

        #if DEBUG
        SalesforceHybridSDKManager.shared().isDevSupportEnabled = true
        #else
        SalesforceHybridSDKManager.shared().isDevSupportEnabled = false
        #endif

        // Set up URL cache with localhost substitution support
        let cacheSizeMemory = 8 * 1024 * 1024  // 8 MB
        let cacheSizeDisk   = 32 * 1024 * 1024 // 32 MB
        let sharedCache = SFLocalhostSubstitutionCache(memoryCapacity: cacheSizeMemory, diskCapacity: cacheSizeDisk, diskPath: "nsurlcache")
        URLCache.shared = sharedCache

        window = UIWindow(frame: UIScreen.main.bounds)
        window?.autoresizesSubviews = true

        initializeAppViewState()

        SFSDKAuthHelper.loginIfRequired { [weak self] in
            self?.setupRootViewController()
        }

        // Register for user change notifications
        SFSDKAuthHelper.registerBlock(forCurrentUserChangeNotifications: { [weak self] in
            self?.resetViewState {
                self?.setupRootViewController()
            }
        })

        // Return false: we handle window setup ourselves; Cordova's super implementation
        // would create a second window with a bare WebView which we don't want.
        return false
    }

    public override func application(_ application: UIApplication, didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data) {
        SFPushNotificationManager.sharedInstance().didRegisterForRemoteNotifications(withDeviceToken: deviceToken)
        if SFUserAccountManager.sharedInstance().currentUser?.credentials.accessToken != nil {
            SFPushNotificationManager.sharedInstance().registerSalesforceNotifications(completionBlock: nil, fail: nil)
        }
        super.application(application, didRegisterForRemoteNotificationsWithDeviceToken: deviceToken)
    }

    public override func application(_ app: UIApplication, open url: URL, options: [UIApplication.OpenURLOptionsKey: Any] = [:]) -> Bool {
        // Uncomment to enable IDP Login flow:
        // return SFUserAccountManager.sharedInstance().handleIDPAuthenticationResponse(url, options: options)
        return false
    }

    // MARK: - Private helpers

    private func initializeAppViewState() {
        guard Thread.isMainThread else {
            DispatchQueue.main.async { self.initializeAppViewState() }
            return
        }
        window?.rootViewController = InitialViewController(nibName: nil, bundle: nil)
        window?.makeKeyAndVisible()
    }

    private func setupRootViewController() {
        let config = SalesforceHybridSDKManager.shared().appConfig as? SFHybridViewConfig
        viewController = SFHybridViewController(config: config)
        window?.rootViewController = viewController
    }

    private func resetViewState(_ postResetBlock: @escaping () -> Void) {
        if window?.rootViewController?.presentedViewController != nil {
            window?.rootViewController?.dismiss(animated: false, completion: postResetBlock)
        } else {
            postResetBlock()
        }
    }
}
