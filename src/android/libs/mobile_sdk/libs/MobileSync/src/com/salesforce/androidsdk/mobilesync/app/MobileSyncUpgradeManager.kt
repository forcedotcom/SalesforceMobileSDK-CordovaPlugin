/*
 * Copyright (c) 2014-present, salesforce.com, inc.
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
package com.salesforce.androidsdk.mobilesync.app

import com.salesforce.androidsdk.app.SalesforceSDKManager
import com.salesforce.androidsdk.smartstore.app.SmartStoreUpgradeManager

/**
 * This class handles upgrades from one version to another.
 *
 * @author bhariharan
 */
open class MobileSyncUpgradeManager : SmartStoreUpgradeManager() {
    override fun upgrade() {
        super.upgrade()
        upgradeSObject()
    }

    /**
     * Upgrades mobile sync data from existing client version to the current version.
     */
    @Synchronized
    protected fun upgradeSObject() {
        val installedVersionStr = installedSobjectVersion
        if (installedVersionStr == SalesforceSDKManager.SDK_VERSION) {
            return
        }

        // Update shared preference file to reflect the latest version.
        writeCurVersion(MOBILE_SYNC_KEY, SalesforceSDKManager.SDK_VERSION)

        // Compare SDK versions using SdkVersion class and add upgrade steps here as needed.
    }

    /**
     * Returns the currently installed version of mobile sync.
     *
     * @return Currently installed version of mobile sync.
     */
    val installedSobjectVersion: String
        get() = getInstalledVersion(MOBILE_SYNC_KEY)

    companion object {
        /**
         * Key in shared preference file for mobile sync version.
         */
        private const val MOBILE_SYNC_KEY = "mobile_sync_version"

        /**
         * Returns an instance of this class.
         *
         * @return Instance of this class.
         */
        val instance: MobileSyncUpgradeManager by lazy { MobileSyncUpgradeManager() }
    }
}