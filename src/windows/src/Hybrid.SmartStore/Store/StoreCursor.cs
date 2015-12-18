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
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Hybrid.SmartStore.Store;

namespace Salesforce.SDK.Hybrid.SmartStore
{
    public sealed class StoreCursor
    {
        private static int _lastId;

        private readonly QuerySpec _querySpec;
        private readonly int _totalPages;
        private readonly long _totalEntries;
        private int _currentPageIndex;
        public int CursorId { get; private set; }

        public StoreCursor()
        {
            throw new NotImplementedException();
        }

        public StoreCursor(ISmartStore store, QuerySpec querySpec)
        {
            if (store == null || querySpec == null || querySpec.PageSize <= 0)
            {
                throw new ArgumentException();
            }

            CursorId = _lastId++;
            _querySpec = querySpec;
            _totalEntries = store.CountQuery(querySpec);
            _totalPages = (int)Math.Ceiling(((double)_totalEntries) / querySpec.PageSize);
            _currentPageIndex = 0;
        }

        public void MoveToPageIndex(int newPageIndex)
        {
            _currentPageIndex = newPageIndex < 0 ? 0 : newPageIndex >= _totalPages ? _totalPages - 1 : newPageIndex;
        }

        public string GetCursorData(ISmartStore smartStore)
        {
            var arr = (JArray)smartStore.Query(_querySpec, _currentPageIndex);

            return new JObject
            {
                {"cursorId", CursorId},
                {"currentPageIndex", _currentPageIndex},
                {"pageSize", _querySpec.PageSize},
                {"totalEntries", _totalEntries},
                {"totalPages", _totalPages},
                {"currentPageOrderedEntries", arr}
            }.ToString();
        }
    }
}
