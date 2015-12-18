 /* Copyright (c) 2015, salesforce.com, inc.
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
     var SERVICE = "com.salesforce.smartsync";
     var manager = Salesforce.SDK.Hybrid.SmartSync;
     var exec = require("com.salesforce.util.exec").exec;
     var core = require("com.salesforce.SalesforceCore").SalesforceJS;
     var oauth = new core.OAuth2();

     // Helper function to handle calls that don't specify isGlobalStore as first argument
     // If missing, the caller is re-invoked with false prepended to the arguments list and true is returned
     // Otherwise, false is returned
     var checkFirstArg = function (argumentsOfCaller) {
         // Turning arguments into array
         var args = Array.prototype.slice.call(argumentsOfCaller);
         // If first argument is not a boolean
         if (typeof (args[0]) !== "boolean") {
             // Pre-pending false
             args.unshift(false);
             // Re-invoking function
             argumentsOfCaller.callee.apply(null, args);
             return true;
         }
             // First argument is a boolean
         else {
             return false;
         }
     };

     var getInstance = function() {
       syncmanager = manager.SyncManager.getInstance();
       return syncmanager;
     }

     var syncDown = function(successCB, errorCB, args) {
         if (checkFirstArg(arguments)) return;
         var payload = args[1];
         var syncmanager = getInstance(account, null);
         if(!syncmanager) {
           errorCB("Error in getting instance for SmartSync");
         }
         else {
           syncmanager.syncDown(payload.target.asJson(), payload.soupName, successCB("Accounts Synced Down"), options);

         }

     };

     var reSync = function(succesCB, errorCB, args) {
       if(checkFirstArg(arguments)) return;
       var payload = args[1];
       var syncmanager = getInstance();
       if(!syncmanager) {
         errorCB("Error in getting instance for SmartSync");
       }
       else {
         syncManager.reSync(payload.syncId, successCB("Resync Successful"));

       }
     };

     var syncUp = function(successCB, errorCB, args) {
       if(checkFirstArg(arguments)) return;
       var payload = args[1];
       var syncmanager = getInstance();
       if(!syncmanager) {
         errorCB("Error in getting instance for SmartSync");
       }
       else {
         syncManager.syncUp(payload.target, payload.soupName, payload.options, successCB("Accounts Synced Up Successfully"));
       }
     };

     var getSyncStatus = function(successCB, errorCB, args) {
       if(checkFirstArg(arguments)) return;
       var payload = args[1];
       var syncmanager = getInstance();
       if(!syncmanager) {
         errorCB("Error in getting instance for SmartSync");
       }
       else {
         syncManager.getSyncStatus(paload.syncId);
         successCB();
       }
     };

     /**
      * Part of the module that is public
      */
     module.exports = {
       syncDown: syncDown,
       reSync: reSync,
       syncUp: syncUp,
       getSyncStatus: getSyncStatus
     };
     require("cordova/exec/proxy").add(SERVICE, module.exports);
