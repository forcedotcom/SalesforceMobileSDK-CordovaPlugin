/*
 * Copyright (c) 2015, salesforce.com, inc.
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

using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Salesforce.SDK.Rest
{
    public interface IRestResponse
    {
        /// <summary>
        /// True if REST request was successfully executed
        /// </summary>
        bool HasResponse { get; }

        /// <summary>
        /// True if REST request was successfully executed
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Error that was raised if REST request did not execute successfully
        /// </summary>
        Exception Error { get; }

        /// <summary>
        /// Body of the REST response returned by the server as a string
        /// </summary>
        string AsString { get; }

        /// <summary>
        /// Body of the REST response returned by the server as a JArray
        /// </summary>
        JArray AsJArray { get; }

        /// <summary>
        /// Body of the REST response returned by the server as a JObject
        /// </summary>
        JObject AsJObject { get; }

        /// <summary>
        /// HTTP status code fo the response returned by the server
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Body of the REST response returned by the server as a
        /// formatted JObject, JArray, or string
        /// </summary>
        string PrettyBody { get; }

        string ErrorReasonPhrase { get; }

        Dictionary<string, IEnumerable<string>> Headers { get; } 
    }
}
