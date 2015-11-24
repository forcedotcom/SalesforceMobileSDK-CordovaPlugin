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
var SERVICE = "com.salesforce.sfaccountmanager";

var exec = require("com.salesforce.util.exec").exec;
var core = require("com.salesforce.SalesforceCore").SalesforceJS;
var oauth2 = new core.OAuth2();

var UserAccount = function(authToken, refreshToken, loginServer, idUrl, instanceServer, orgId, userId, username, clientId) {
    this.authToken = authToken;
    this.refreshToken = refreshToken;
    this.loginServer = loginServer;
    this.idUrl = idUrl;
    this.instanceServer = instanceServer;
    this.orgId = orgId;
    this.userId = userId;
    this.username = username;
    this.clientId = clientId;
};

var logoutInitiated = false;

var getUsers = function (successCB, errorCB, args) {
    var users = oauth2.getUsers(successCB, errorCB);
    if (users)
    {
        successCB(users);
    } else {
        errorCB(users);
    }
};

var getCurrentUser = function (successCB, errorCB, args) {
    var user = oauth2.getUser(successCB, errorCB);
    if (user)
    {
        successCB(user);
    } else {
        errorCB(user);
    }
}

var logout = function (successCB, errorCB, args) {
  if(!logoutInitiated) {
    logoutInitiated = true;
    successCB(oauth2.logout());
  }
}

var switchToUser = function (successCB, errorCB, args) {
    if (args.constructor === Array && args.length > 1) {
        var user = args[1];
        oauth2.switchToUser(user);
    } else {
        oauth2.switchToUser(null);
    }
}

module.exports = {
    UserAccount: UserAccount,
    getUsers: getUsers,
    getCurrentUser: getCurrentUser,
    logout: logout,
    switchToUser: switchToUser
};

require("cordova/exec/proxy").add(SERVICE, module.exports);
