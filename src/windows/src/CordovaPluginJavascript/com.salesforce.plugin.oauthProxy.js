/*
 * Copyright (c) 2015, salesforce.com, inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided
 * that the following conditions are met:
 *
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the
 * following disclaimer.
 *
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
 * the following disclaimer in the documentation and/or other materials provided with the distribution.
 *
 * Neither the name of salesforce.com, inc. nor the names of its contributors may be used to endorse or
 * promote products derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

var SALESFORCE_MOBILE_SDK_VERSION = "4.0.0";
var SERVICE = "com.salesforce.oauth";

var exec = require("com.salesforce.util.exec").exec;
var core = require("com.salesforce.SalesforceCore").SalesforceJS;
var oauth2 = new core.OAuth2();
oauth2.configureOAuth("bootconfig.json", "servers.xml");

var logoutInitiated = false;

var getAuthCredentials = function(success, error) {
    oauth2.getAuthCredentials(success, error);
};

var authenticate = function (success, fail, server) {
    var serv = server;
    if (server instanceof Array) {
        serv = server[1];
    }
    oauth2.login(serv).done(function (account) {
        success(account);
    }, function (error) {
        fail(error);
    });
};

var logout = function() {
    if(!logoutInitiated) {
      logoutInitiated = true;
      oauth2.logout();
    }

};

var getAppHomeUrl = function(success) {
  oauth2.getAppHomeUrl(success);
};

var forcetkRefresh = function(success, error) {
    oauth2.forcetkRefresh(success, error);
}

module.exports = {
    getAuthCredentials: getAuthCredentials,
    authenticate: authenticate,
    logout: logout,
    getAppHomeUrl: getAppHomeUrl,
    forcetkRefresh: forcetkRefresh
};

require("cordova/exec/proxy").add("com.salesforce.oauth", module.exports);
