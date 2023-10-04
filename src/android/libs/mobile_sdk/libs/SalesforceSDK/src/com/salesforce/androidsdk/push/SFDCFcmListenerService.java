/*
 * Copyright (c) 2018-present, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */
package com.salesforce.androidsdk.push;

import androidx.annotation.NonNull;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;
import com.salesforce.androidsdk.accounts.UserAccount;
import com.salesforce.androidsdk.app.SalesforceSDKManager;
import com.salesforce.androidsdk.util.SalesforceSDKLogger;

/**
 * This class is called when a message is received or the token changes.
 *
 * @author bhariharan
 */
public class SFDCFcmListenerService extends FirebaseMessagingService {
    private static final String TAG = "FcmListenerService";

    @Override
    public void onNewToken(@NonNull String token) {
        try {
            final UserAccount account = SalesforceSDKManager.getInstance().getUserAccountManager().getCurrentUser();

            if (account != null) {
                // Store the new token.
                PushMessaging.setRegistrationId(this, token, account);

                // Send it to SFDC.
                PushMessaging.registerSFDCPush(this, account);
            }
        } catch (Exception e) {
            SalesforceSDKLogger.e(TAG, "Error during FCM registration", e);
        }
    }

    /**
     * Called when message is received.
     *
     * @param message Remote message received.
     */
    @Override
    public void onMessageReceived(@NonNull RemoteMessage message) {
        if (SalesforceSDKManager.hasInstance()) {
            final PushNotificationDecryptor pnDecryptor = PushNotificationDecryptor.getInstance();
            pnDecryptor.onPushMessageReceived(message);
        }
    }
}
