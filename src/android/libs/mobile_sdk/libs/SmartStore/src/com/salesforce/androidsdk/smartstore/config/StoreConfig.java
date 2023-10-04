/*
 * Copyright (c) 2017-present, salesforce.com, inc.
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
package com.salesforce.androidsdk.smartstore.config;

import android.content.Context;

import com.salesforce.androidsdk.smartstore.store.IndexSpec;
import com.salesforce.androidsdk.smartstore.store.SmartStore;
import com.salesforce.androidsdk.smartstore.util.SmartStoreLogger;
import com.salesforce.androidsdk.util.ResourceReaderHelper;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * Class encapsulating a SmartStore schema (soups).
 *
 * Config expected in a resource or assets file in JSON with the following:
 * {
 *     soups: [
 *          {
 *              soupName: xxx
 *              indexes: [
 *                  {
 *                      path: xxx
 *                      type: xxx
 *                  }
 *              ]
 *          }
 *     ]
 * }
 */

public class StoreConfig {

    private static final String TAG = "StoreConfig";

    public static final String SOUPS = "soups";
    public static final String SOUP_NAME = "soupName";
    public static final String INDEXES = "indexes";

    private JSONArray soupConfigs;

    /**
     * Constructor for config stored in resource file
     * @param ctx Context.
     * @param resourceId Id of resource file.
     */
    public StoreConfig(Context ctx, int resourceId) {
        this(ResourceReaderHelper.readResourceFile(ctx, resourceId));
    }

    /**
     * Constructor for config stored in asset file
     * @param ctx Context.
     * @param assetPath Path of assets file.
     */
    public StoreConfig(Context ctx, String assetPath) {
        this(ResourceReaderHelper.readAssetFile(ctx, assetPath));
    }

    private StoreConfig(String str) {
        try {
            if (str == null) {
                soupConfigs = null;
            } else {
                JSONObject config = new JSONObject(str);
                soupConfigs = config.getJSONArray(SOUPS);
            }
        } catch (JSONException e) {
            SmartStoreLogger.e(TAG, "Unhandled exception parsing json", e);
        }
    }

    /**
     * Return true if soups are defined in config
     * @return
     */
    public boolean hasSoups() {
        return soupConfigs != null && soupConfigs.length() > 0;
    }

    /**
     * Register the soup from the config in the given store
     * NB: only feedback is through the logs - the config is static so getting it right is something the developer should do while writing the app
     * @param store
     */
    public void registerSoups(SmartStore store) {
        if (soupConfigs == null) {
            SmartStoreLogger.d(TAG, "No store config available");
            return;
        }

        for (int i = 0; i< soupConfigs.length(); i++) {
            try {
                JSONObject soupConfig = soupConfigs.getJSONObject(i);
                String soupName = soupConfig.getString(SOUP_NAME);

                // Leaving soup alone if it already exists
                if (store.hasSoup(soupName)) {
                    SmartStoreLogger.d(TAG, "Soup already exists:" + soupName + " - skipping");
                    continue;
                }

                IndexSpec[] indexSpecs = IndexSpec.fromJSON(soupConfig.getJSONArray(INDEXES));
                SmartStoreLogger.d(TAG, "Registering soup:" + soupName);
                store.registerSoup(soupName, indexSpecs);
            } catch (JSONException e) {
                SmartStoreLogger.e(TAG, "Unhandled exception parsing json", e);
            }
        }
    }

}
