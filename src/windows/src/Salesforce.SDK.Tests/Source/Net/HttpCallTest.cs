/*
 * Copyright (c) 2013, salesforce.com, inc.
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
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Salesforce.SDK.App;
using Salesforce.SDK.Core;
using Salesforce.SDK.Logging;
using System.Net;
using System.Threading.Tasks;

namespace Salesforce.SDK.Net
{
    [TestClass]
    public class HttpCallTest
    {
        private static void AssertThrows<T>(Action func) where T : Exception
        {
            try
            {
                func.Invoke();
                Assert.Fail("An exception of type {0} was expected", typeof (T));
            }
            catch (T)
            {
                // good
            }
        }

        [ClassInitialize]
        public static void SetupClass(TestContext context)
        {
            SFApplicationHelper.RegisterServices();
            SDKServiceLocator.RegisterService<ILoggingService, Hybrid.Logging.Logger>();
        }

        [TestMethod]
        public void TestPropertiesWhenNotExecuted()
        {
            HttpCall call = HttpCall.CreateGet("http://www.google.com");
            Assert.IsFalse(call.Executed);
            Assert.IsFalse(call.HasResponse);
            AssertThrows<InvalidOperationException>(() => { bool x = call.Success; });
            AssertThrows<InvalidOperationException>(() => { Exception x = call.Error; });
            AssertThrows<InvalidOperationException>(() => { string x = call.ResponseBody; });
        }

        [TestMethod]
        public async Task TestPropertiesWhenExecuted()
        {
            HttpCall call = await HttpCall.CreateGet("http://www.google.com").ExecuteAsync();
            Assert.IsTrue(call.Executed);
            Assert.IsTrue(call.HasResponse);
            Assert.IsTrue(call.Success);
            Assert.IsNull(call.Error);
            Assert.IsTrue(call.ResponseBody.Contains("Google Search"));
            Assert.AreEqual(HttpStatusCode.OK, call.StatusCode);
        }

        [TestMethod]
        public async Task TestPropertiesWhenInvalidHost()
        {
            HttpCall call = await HttpCall.CreateGet("http://bogus").ExecuteAsync();
            Assert.IsTrue(call.Executed);
            Assert.IsFalse(call.Success);
            Assert.IsNotNull(call.Error);
        }
    }
}