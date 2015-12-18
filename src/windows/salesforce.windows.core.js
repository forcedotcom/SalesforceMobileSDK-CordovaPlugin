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
/// <reference path="../typings/WinJS-3.0.d.ts"/>
/// <reference path="../typings/Salesforce.SDK.Hybrid.d.ts"/>
/// <reference path="../typings/winrt.d.ts"/>
var SalesforceJS;
(function (SalesforceJS) {
    var BootConfig = (function () {
        function BootConfig() {
        }
        return BootConfig;
    })();
    SalesforceJS.BootConfig = BootConfig;
    var Server = (function () {
        function Server(n, a) {
            this.name = n;
            this.address = a;
        }
        return Server;
    })();
    SalesforceJS.Server = Server;
    var ServerConfig = (function () {
        function ServerConfig() {
        }
        return ServerConfig;
    })();
    SalesforceJS.ServerConfig = ServerConfig;
    var OAuth2 = (function () {
        function OAuth2() {
            this.auth = Salesforce.SDK.Hybrid.Auth;
            this.rest = Salesforce.SDK.Hybrid.Rest;
            this.config = new BootConfig();
            this.servers = new ServerConfig();
        }
        OAuth2.prototype.configureOAuth = function (bootConfig, serverConfig) {
            if ((/^\s*$/).test(bootConfig)) {
                bootConfig = "bootconfig.json";
            }
            if ((/^\s*$/).test(serverConfig)) {
                serverConfig = "servers.xml";
            }
            var self = this;
            return new WinJS.Promise(function (resolve, reject, progress) {
                WinJS.xhr({ url: bootConfig }).then(function (response) {
                    self.loadBootConfig(response.responseText);
                    progress();
                }).then(function () {
                    WinJS.xhr({ url: serverConfig }).done(function (response) {
                        self.loadServerXml(response);
                        resolve(self);
                    });
                });
            });
        };
        OAuth2.prototype.loadBootConfig = function (response) {
            this.config = JSON.parse(response);
        };
        OAuth2.prototype.loadServerXml = function (response) {
            var data = response.responseXML;
            var serverItems = data.querySelectorAll("servers > server");
            var serversList = new Array();
            for (var i = 0; i < serverItems.length; i++) {
                var server = serverItems[i];
                serversList.push({ name: server.attributes.getNamedItem("name").value, address: server.attributes.getNamedItem("url").value });
            }
            this.servers["serverList"] = serversList;
        };
        OAuth2.prototype.loginDefaultServer = function (success, fail) {
            var self = this;
            if (this.servers.serverList == null) {
                this.configureOAuth("bootconfig.json", "servers.xml").then(function(response) {
                    self.login(response["servers"]["serverList"][0]).done(function(account) {
                        success(self.auth.Account.toJson(account));
                    }, function(error) {
                        fail(error);
                    });
                });
            } else {
                self.login(this["servers"]["serverList"][0]).done(function (account) {
                    success(self.auth.Account.toJson(account));
                }, function (error) {
                    fail(error);
                });
            }
        };
        OAuth2.prototype.login = function (server) {
            var auth = Salesforce.SDK.Hybrid.Auth;
            auth.HybridAccountManager.initEncryption();
            var boot = this.config;
            return new WinJS.Promise(function (resolve, reject, progress) {
                var options = new auth.LoginOptions(server.address, boot.remoteAccessConsumerKey, boot.oauthRedirectURI, boot.oauthScopes);
                var startUriStr = auth.OAuth2.computeAuthorizationUrl(options);
                var startUri = new Windows.Foundation.Uri(startUriStr);
                var endUri = new Windows.Foundation.Uri(boot.oauthRedirectURI);
                Windows.Security.Authentication.Web.WebAuthenticationBroker.authenticateAsync(Windows.Security.Authentication.Web.WebAuthenticationOptions.none, startUri, endUri).done(function (result) {
                    if (result.responseData == "") {
                        reject(result.responseStatus);
                    }
                    else {
                        var responseResult = new Windows.Foundation.Uri(result.responseData);
                        var authResponse = responseResult.fragment.substring(1);
                        auth.HybridAccountManager.createNewAccount(options, authResponse).done(function (newAccount) {
                            if (newAccount != null) {
                                resolve(newAccount);
                            }
                            else {
                                reject(result.responseErrorDetail);
                            }
                        });
                    }
                }, function (err) {
                    reject(err);
                });
                progress();
            });
        };
        OAuth2.prototype.logout = function () {
            var _this = this;
            return new WinJS.Promise(function (resolve) {
                var cm = new _this.rest.ClientManager();
                var client = cm.peekRestClient();
                if (client != null) {
                    cm.logout().done(function () {
                        resolve();
                    });
                }
                else {
                    resolve();
                }
            });

        };
        OAuth2.prototype.getAuthCredentials = function (success, fail) {
            var account = this.auth.HybridAccountManager.getAccount();
            if (account != null) {
                success(this.auth.Account.toJson(account));
            }
            else {
              this.loginDefaultServer(success, fail);
            }
        };
        OAuth2.prototype.forcetkRefresh = function (success, fail) {
            var account = this.auth.HybridAccountManager.getAccount();
            if (account != null) {
                this.auth.OAuth2.refreshAuthToken(account).done(function (resolve) {
                    success(resolve);
                }, function (reject) {
                    fail(reject);
                });
            }
            else {
                fail(null);
            }
        };
        OAuth2.prototype.getUsers = function (success, fail) {
            var imapAccounts = this.auth.HybridAccountManager.getAccounts();
            if (imapAccounts != null && imapAccounts.size > 0) {
                var first = imapAccounts.first();
                var accounts = new Array();
                accounts.push(first.current.value);
                while (first.moveNext()) {
                    accounts.push(first.current.value);
                }
                success(accounts);
            }
            else {
                fail(null);
            }
        };
        OAuth2.prototype.getUser = function (success, fail) {
            var account = this.auth.HybridAccountManager.getAccount();
            if (account != null) {
                success(account);
            }
            else {
                fail(null);
            }
        };
        OAuth2.prototype.switchToUser = function (account) {
            return this.auth.HybridAccountManager.switchToAccount(account);
        };
        OAuth2.prototype.getAppHomeUrl = function () {
            if (this.config == null) {
                return null;
            }
            return this.config.startPage;
        };
        return OAuth2;
    })();
    SalesforceJS.OAuth2 = OAuth2;
})(SalesforceJS || (SalesforceJS = {}));



module.exports = {
   SalesforceJS : SalesforceJS
};
//# sourceMappingURL=salesforce.windows.core.js.map
require("cordova/exec/proxy").add("com.salesforce.SalesforceCore", module.exports);
