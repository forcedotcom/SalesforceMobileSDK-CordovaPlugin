/*
 * Copyright (c) 2025-present, salesforce.com, inc.
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
package com.salesforce.androidsdk.rest

import com.salesforce.androidsdk.rest.SfapApiClient.Companion.jsonIgnoreUnknownKeys
import kotlinx.serialization.Serializable

/**
 * Models the Salesforce Notifications API actions endpoint's `error` response.
 * See https://salesforce.quip.com/dwjPAf3py4Br
 * TODO: Replace the documentation link with the final documentation. ECJ20250320
 */
@Serializable
data class NotificationsApiErrorResponseBody(
    val errorCode: String? = null,
    val message: String? = null,
    val messageCode: String? = null
) {

    /** The original JSON used to initialize this response body */
    var sourceJson: String? = null
        private set

    companion object {

        /**
         * Returns an Salesforce Notifications API actions endpoint error
         * response from the JSON text.
         * @param json The JSON text
         * @return The Salesforce Notifications API actions endpoint error
         * response
         */
        fun fromJson(json: String): Array<NotificationsApiErrorResponseBody> {

            val results = jsonIgnoreUnknownKeys.decodeFromString<Array<NotificationsApiErrorResponseBody>>(json)
            results.forEach { it.sourceJson = json }
            return results
        }
    }
}
