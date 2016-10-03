﻿/*
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
using Salesforce.SDK.Auth;
using Salesforce.SDK.Rest;

namespace Salesforce.SDK
{
    public class TestCredentials
    {
        public const String CallbackUrl = "test://sfdc";
        public static readonly string[] Scopes = { "web" };
        public const String AccessToken = "test_auth_token";
        public const String InstanceServer = "https://cs1.salesforce.com";
        public String ApiVersion = ApiVersionStrings.VersionNumber;
        public const String AccountType = "com.salesforce.windowssdk.smartsynctest.login";
        public const String OrgId = "00DS0000003E98jMAC";
        public const String Username = "ut2@cs1.mobilesdk.org";
        public const String AccountName = "ut2@cs1.mobilesdk.org";
        public const String UserId = "005S0000004s2iyIAA";
        public const String LoginUrl = "https://test.salesforce.com";
        public const String InstanceUrl = "https://cs1.salesforce.com";
        public const String CommunityUrl = "https://cs1.salesforce.com/androidcomm";
        public const String IdentityUrl = "https://test.salesforce.com";

        public const String ClientId =
        "3MVG9Iu66FKeHhINkB1l7xt7kR8czFcCTUhgoA8Ol2Ltf1eYHOU4SqQRSEitYFDUpqRWcoQ2.dBv_a1Dyu5xa";

        public const string RefreshToken =
            "5Aep861KIwKdekr90IDidO4EhfJiYo3fzEvTvsEgM9sfDpGX0qFFeQzHG2mZeUH_.XNSBE0Iz38fnWsyYYkUgTz";

        public static Account TestAccount => new Account(LoginUrl, ClientId, CallbackUrl,
            Scopes, InstanceUrl, IdentityUrl, AccessToken, RefreshToken);
    }
}