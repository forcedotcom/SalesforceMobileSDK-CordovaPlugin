/*
 * Copyright (c) 2014, salesforce.com, inc.
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
using System.Text.RegularExpressions;

namespace Salesforce.SDK.Utilities
{
    public static class ExtensionMethods
    {
        private static readonly Regex QUERY_PARAMS = new Regex(@"[?|&](\w+)=([^?|^&]+)");

        /// <summary>
        /// Parses out parameters from a query string.
        /// </summary>
        /// <remarks>If the query string does not start with a '?' or '&' then the
        /// first parameter will get skipped so make sure your query string starts with '?'.</remarks>
        /// <param name="queryString">The query string to parse.</param>
        /// <returns>A dictionary of parameter names and values.</returns>
        public static Dictionary<string, string> ParseQueryString(this string queryString)
        {
            Match match = QUERY_PARAMS.Match(queryString);
            var results = new Dictionary<string, string>();
            while (match.Success)
            {
                results.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return results;
        }

        public static Dictionary<string, string> ParseQueryString(this Uri uri)
        {
            return ParseQueryString(uri.PathAndQuery);
        }
    }
}