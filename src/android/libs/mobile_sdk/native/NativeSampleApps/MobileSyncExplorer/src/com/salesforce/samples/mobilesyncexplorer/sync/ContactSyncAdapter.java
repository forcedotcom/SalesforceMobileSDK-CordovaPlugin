/*
 * Copyright (c) 2015-present, salesforce.com, inc.
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
package com.salesforce.samples.mobilesyncexplorer.sync;

import android.accounts.Account;
import android.content.AbstractThreadedSyncAdapter;
import android.content.ContentProviderClient;
import android.content.Context;
import android.content.SyncResult;
import android.os.Bundle;

import com.salesforce.androidsdk.accounts.UserAccount;
import com.salesforce.androidsdk.accounts.UserAccountManager;
import com.salesforce.androidsdk.app.SalesforceSDKManager;
import com.salesforce.samples.mobilesyncexplorer.loaders.ContactListLoader;

/**
 * A simple sync adapter to perform background sync of contacts.
 *
 * @author bhariharan
 */
public class ContactSyncAdapter extends AbstractThreadedSyncAdapter {

    // Key for extras bundle
    public static final String SYNC_DOWN_ONLY = "syncDownOnly";

    /**
     * Parameterized constructor.
     *
     * @param context        Context.
     * @param autoInitialize True - if it should be initialized automatically, False - otherwise.
     */
    public ContactSyncAdapter(Context context, boolean autoInitialize,
                              boolean allowParallelSyncs) {
        super(context, autoInitialize, allowParallelSyncs);
    }

    @Override
    public void onPerformSync(Account account, Bundle extras, String authority,
                              ContentProviderClient provider, SyncResult syncResult) {
        final boolean syncDownOnly = extras.getBoolean(SYNC_DOWN_ONLY, false);
        final SalesforceSDKManager sdkManager = SalesforceSDKManager.getInstance();
        final UserAccountManager accManager = sdkManager.getUserAccountManager();
        if (sdkManager.isLoggingOut() || accManager.getAuthenticatedUsers() == null) {
            return;
        }
        if (account != null) {
            final UserAccount user = sdkManager.getUserAccountManager().buildUserAccount(account);
            final ContactListLoader contactLoader = new ContactListLoader(getContext(), user);
            if (syncDownOnly) {
                contactLoader.syncDown();
            } else {
                contactLoader.syncUp(); // does a sync up followed by a sync down
            }
        }
    }
}
