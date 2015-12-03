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
using Salesforce.SDK.Rest;

namespace Salesforce.SDK.SmartStore.Store
{
    internal class TestCredentials
    {
        public String ApiVersion = ApiVersionStrings.VersionNumber;
        public const String AccountType = "com.salesforce.windowssdk.smartsynctest.login";
        public const String OrgId = "00DS0000000HDptMAG";
        public const String Username = "sdktest@cs1.com";
        public const String AccountName = "sdktest@cs1.com";
        public const String UserId = "005S0000003yaERIAY";
        public const String LoginUrl = "https://test.salesforce.com";
        public const String InstanceUrl = "https://cs1.salesforce.com";
        public const String CommunityUrl = "https://cs1.salesforce.com/androidcomm";
        public const String IdentityUrl = "https://test.salesforce.com";

        public const String ClientId =
            "3MVG92.uWdyphVj4bnolD7yuIpCQsNgddWtqRND3faxrv9uKnbj47H4RkwheHA2lKY4cBusvDVp0M6gdGE8hp";

        public const String RefreshToken =
            "5Aep861KIwKdekr90KlxVVUI47zdR6dX_VeBWZBS.SiQYYAy5JPlgkezkgDiE1o9mI4jd6mD4ZFYA==";
    }
}