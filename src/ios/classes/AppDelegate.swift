/*
 Copyright (c) 2026-present, salesforce.com, inc. All rights reserved.

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
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
 */

import UIKit
import SalesforceHybridSDK
import SalesforceSDKCore

@main
#if compiler(>=6.1)
@objc @implementation
#else
@_objcImplementation
#endif
extension AppDelegate {

    public override func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        // Initialize SalesforceHybridSDKManager — required for hybrid apps
        SalesforceHybridSDKManager.initializeSDK()

        #if DEBUG
        SalesforceHybridSDKManager.shared.isDevSupportEnabled = true
        #else
        SalesforceHybridSDKManager.shared.isDevSupportEnabled = false
        #endif

        // Set up URL cache with localhost substitution support for local hybrid apps
        let cacheSizeMemory: UInt = 8 * 1024 * 1024   // 8 MB
        let cacheSizeDisk: UInt   = 32 * 1024 * 1024  // 32 MB
        let sharedCache = SFLocalhostSubstitutionCache(memoryCapacity: cacheSizeMemory, diskCapacity: cacheSizeDisk, diskPath: "nsurlcache")
        URLCache.shared = sharedCache

        if Bundle.main.object(forInfoDictionaryKey: "UIApplicationSceneManifest") != nil {
            // Cordova creates the app's window while connecting its scene. Wait until
            // that window is available before starting authentication so AuthHelper
            // receives the scene that owns the hybrid UI.
            NotificationCenter.default.addObserver(
                self,
                selector: #selector(handleInitialSceneWillEnterForeground(_:)),
                name: UIScene.willEnterForegroundNotification,
                object: nil
            )
        } else {
            // Direct iOS-Hybrid sample apps still use the legacy application lifecycle.
            // Preserve their AppDelegate-owned window and immediate startup behavior.
            startApp()
        }

        // CDVAppDelegate's implementation also returns true and creates no window.
        return true
    }

    public override func application(_ application: UIApplication, didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data) {
        PushNotificationManager.sharedInstance().didRegisterForRemoteNotifications(withDeviceToken: deviceToken)
        if UserAccountManager.shared.currentUserAccount?.credentials.accessToken != nil {
            PushNotificationManager.sharedInstance().registerSalesforceNotifications(completionBlock: nil, failBlock: nil)
        }
        super.application(application, didRegisterForRemoteNotificationsWithDeviceToken: deviceToken)
    }

    public override func application(_ app: UIApplication, open url: URL, options: [UIApplication.OpenURLOptionsKey: Any] = [:]) -> Bool {
        // Uncomment to enable IDP Login flow:
        // return UserAccountManager.shared.handleIDPAuthenticationResponse(url, options: options)
        return false
    }

    // MARK: - Private helpers

    @objc private func handleInitialSceneWillEnterForeground(_ notification: Notification) {
        guard let scene = notification.object as? UIWindowScene,
              scene.session.role == .windowApplication else {
            return
        }

        NotificationCenter.default.removeObserver(
            self,
            name: UIScene.willEnterForegroundNotification,
            object: nil
        )
        startApp(in: scene)
    }

    private func startApp() {
        window = UIWindow(frame: UIScreen.main.bounds)
        window?.autoresizesSubviews = true
        completeAppStartup()
    }

    private func startApp(in scene: UIWindowScene) {
        // Prefer Cordova's delegate-owned storyboard window. Salesforce auth
        // and snapshot windows can temporarily be key during scene transitions.
        let sceneDelegateWindow = (scene.delegate as? UIWindowSceneDelegate)?.window ?? nil
        window = sceneDelegateWindow
            ?? scene.windows.first(where: { $0.windowLevel == .normal })
            ?? UIWindow(windowScene: scene)
        window?.autoresizesSubviews = true

        completeAppStartup(in: scene)
    }

    private func completeAppStartup(in scene: UIWindowScene? = nil) {
        // Register for user-change notifications so view state is reset on account switch.
        if let scene {
            AuthHelper.registerBlock(forCurrentUserChangeNotifications: scene) { [weak self] in
                self?.resetViewState {
                    self?.setupRootViewController()
                }
            }
        } else {
            AuthHelper.registerBlock(forCurrentUserChangeNotifications: { [weak self] in
                self?.resetViewState {
                    self?.setupRootViewController()
                }
            })
        }

        initializeAppViewState()

        if let scene {
            AuthHelper.loginIfRequired(scene) { [weak self] in
                self?.setupRootViewController()
            }
        } else {
            AuthHelper.loginIfRequired { [weak self] in
                self?.setupRootViewController()
            }
        }
    }

    private func initializeAppViewState() {
        guard Thread.isMainThread else {
            DispatchQueue.main.async { self.initializeAppViewState() }
            return
        }
        // InitialViewController is an ObjC class — imported via Bridging-Header.h
        // by the postinstall-ios.js hook which adds #import "InitialViewController.h"
        window?.rootViewController = InitialViewController(nibName: nil, bundle: nil)
        window?.makeKeyAndVisible()
    }

    private func setupRootViewController() {
        let config = SalesforceHybridSDKManager.shared.bootConfig as? SFHybridViewConfig
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
