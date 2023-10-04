/*
 * Copyright (c) 2019-present, salesforce.com, inc.
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

package com.salesforce.androidsdk.rest;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * BatchRequest: Class to represent a batch request.
 *
 */
 public class BatchRequest extends RestRequest {

    public final List<RestRequest> requests;
    public final boolean haltOnError;

    public BatchRequest(String apiVersion, boolean haltOnError, List<RestRequest> requests) throws JSONException {
        super(RestMethod.POST, RestAction.BATCH.getPath(apiVersion), computeBatchRequestJson(haltOnError, requests));
        this.requests = requests;
        this.haltOnError = haltOnError;
    }

    private static JSONObject computeBatchRequestJson(boolean haltOnError, List<RestRequest> requests) throws JSONException {
        JSONArray requestsArrayJson = new JSONArray();
        for (RestRequest request : requests) {
            // Note: unfortunately batch sub request and composite sub request differ
            if (!request.getPath().startsWith(SERVICES_DATA)) {
                throw new RuntimeException("Request not supported in batch: " + request.toString());
            }
            JSONObject requestJson = new JSONObject();
            requestJson.put(METHOD, request.getMethod().toString());
            requestJson.put(URL, request.getPath().substring(SERVICES_DATA.length()));
            requestJson.put(RICH_INPUT, request.getRequestBodyAsJson());
            requestsArrayJson.put(requestJson);
        }
        JSONObject batchRequestJson = new JSONObject();
        batchRequestJson.put(BATCH_REQUESTS, requestsArrayJson);
        batchRequestJson.put(HALT_ON_ERROR, haltOnError);
        return batchRequestJson;
    }


    /**
     * Builder class for BatchRequest
     */
    public static class BatchRequestBuilder {

        private List<RestRequest> requests = new ArrayList<>();
        private boolean haltOnError;

        public BatchRequestBuilder addRequest(RestRequest request) {
            requests.add(request);
            return this;
        }

        public BatchRequestBuilder setHaltOnError(boolean b) {
            haltOnError = b;
            return this;
        }

        public BatchRequest build(String apiVersion) throws JSONException {
            return new BatchRequest(apiVersion, haltOnError, requests);
        }
    }
}
