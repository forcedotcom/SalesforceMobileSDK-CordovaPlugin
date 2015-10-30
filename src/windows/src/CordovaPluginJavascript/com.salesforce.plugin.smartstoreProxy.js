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
    var SERVICE = "com.salesforce.smartstore";
    var smartStore = Salesforce.SDK.Hybrid.SmartStore;
    var exec = require("com.salesforce.util.exec").exec;
    /**
  * SoupIndexSpec consturctor
  */
    var SoupIndexSpec = function (path, type) {
        this.path = path;
        this.type = type;
    };

    /**
     * QuerySpec constructor
     */
    var QuerySpec = function (path) {
        // the kind of query, one of: "exact","range", "like" or "smart":
        // "exact" uses matchKey, "range" uses beginKey and endKey, "like" uses likeKey, "smart" uses smartSql
        this.queryType = "exact";

        //path for the original IndexSpec you wish to use for search: may be a compound path eg Account.Owner.Name
        this.indexPath = path;

        //for queryType "exact"
        this.matchKey = null;

        //for queryType "like"
        this.likeKey = null;

        //for queryType "range"
        //the value at which query results may begin
        this.beginKey = null;
        //the value at which query results may end
        this.endKey = null;

        // for queryType "smart"
        this.smartSql = null;

        //"ascending" or "descending" : optional
        this.order = "ascending";

        //the number of entries to copy from native to javascript per each cursor page
        this.pageSize = 10;
    };

    /**
     * StoreCursor constructor
     */
    var StoreCursor = function () {
        //a unique identifier for this cursor, used by plugin
        this.cursorId = null;
        //the maximum number of entries returned per page 
        this.pageSize = 0;
        // the total number of results
        this.totalEntries = 0;
        //the total number of pages of results available
        this.totalPages = 0;
        //the current page index among all the pages available
        this.currentPageIndex = 0;
        //the list of current page entries, ordered as requested in the querySpec
        this.currentPageOrderedEntries = null;
    };

    // ====== Logging support ======
    var logLevel;
    var storeConsole = {};

    var setLogLevel = function (level) {
        logLevel = level;
        var methods = ["error", "info", "warn", "debug"];
        var levelAsInt = methods.indexOf(level.toLowerCase());
        for (var i = 0; i < methods.length; i++) {
            storeConsole[methods[i]] = (i <= levelAsInt ? console[methods[i]].bind(console) : function () { });
        }
    };

    // Showing info and above (i.e. error) by default.
    setLogLevel("info");

    var getLogLevel = function () {
        return logLevel;
    };

    // ====== querySpec factory methods
    // Returns a query spec that will page through all soup entries in order by the given path value
    // Internally it simply does a range query with null begin and end keys
    var buildAllQuerySpec = function (path, order, pageSize) {
        var inst = new QuerySpec(path);
        inst.queryType = "range";
        if (order) { inst.order = order; } // override default only if a value was specified
        if (pageSize) { inst.pageSize = pageSize; } // override default only if a value was specified
        return inst;
    };

    // Returns a query spec that will page all entries exactly matching the matchKey value for path
    var buildExactQuerySpec = function (path, matchKey, pageSize) {
        var inst = new QuerySpec(path);
        inst.matchKey = matchKey;
        if (pageSize) { inst.pageSize = pageSize; } // override default only if a value was specified
        return inst;
    };

    // Returns a query spec that will page all entries in the range beginKey ...endKey for path
    var buildRangeQuerySpec = function (path, beginKey, endKey, order, pageSize) {
        var inst = new QuerySpec(path);
        inst.queryType = "range";
        inst.beginKey = beginKey;
        inst.endKey = endKey;
        if (order) { inst.order = order; } // override default only if a value was specified
        if (pageSize) { inst.pageSize = pageSize; } // override default only if a value was specified
        return inst;
    };

    // Returns a query spec that will page all entries matching the given likeKey value for path
    var buildLikeQuerySpec = function (path, likeKey, order, pageSize) {
        var inst = new QuerySpec(path);
        inst.queryType = "like";
        inst.likeKey = likeKey;
        if (order) { inst.order = order; } // override default only if a value was specified
        if (pageSize) { inst.pageSize = pageSize; } // override default only if a value was specified
        return inst;
    };

    // Returns a query spec that will page all results returned by smartSql
    var buildSmartQuerySpec = function (smartSql, pageSize) {
        var inst = new QuerySpec();
        inst.queryType = "smart";
        inst.smartSql = smartSql;
        if (pageSize) { inst.pageSize = pageSize; } // override default only if a value was specified
        return inst;
    };

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


    // ====== Soup manipulation ======
    var getSmartStore = function (isGlobalStore) {
        var sm = null;
        if (isGlobalStore) {
            sm = smartStore.SmartStore.getGlobalSmartStore();
        } else {
            sm = smartStore.SmartStore.getSmartStore();
        }
        return sm;
    }

    var getDatabaseSize = function (successCB, errorCB, args) {
        errorCB("not supported");
    };

    var registerSoup = function (successCB, errorCB, args) {
        var payload = args[1];
        var specs = JSON.stringify(payload.indexes);
        storeConsole.debug("SmartStore.registerSoup:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",indexSpecs=" + specs);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            var indexspecs = smartStore.IndexSpec.jsonToIndexSpecCollection(specs);
            sm.registerSoup(payload.soupName, indexspecs);
            successCB();
        }
    };

    var removeSoup = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.removeSoup:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            var indexspecs = smartStore.IndexSpec.jsonToIndexSpecCollection(specs);
            sm.dropSoup(payload.soupName);
            successCB();
        }
    };

    var getSoupIndexSpecs = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.getSoupIndexSpecs:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            var specs = sm.getSoupIndexSpecsSerialized(payload.soupName);
            successCB(specs);
        }
    };

    var alterSoup = function (successCB, errorCB, args) {
        errorCB("Not supported");
    };

    var reIndexSoup = function (successCB, errorCB, args) {
        errorCB("Not supported");
    };

    var clearSoup = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.clearSoup:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            sm.clearSoup(payload.soupName);
            successCB();
        }
    };

    var showInspector = function (isGlobalStore) {
        storeConsole.debug("SmartStore.showInspector");
        isGlobalStore = isGlobalStore || false;
        exec(SALESFORCE_MOBILE_SDK_VERSION, null, null, SERVICE, "pgShowInspector", [{ "isGlobalStore": isGlobalStore }]);
    };

    var soupExists = function (successCB, errorCB, args) {
        if (checkFirstArg(arguments)) return;
        storeConsole.debug("SmartStore.soupExists:isGlobalStore=" + isGlobalStore + ",soupName=" + soupName);
        var payload = args[1];
        storeConsole.debug("SmartStore.soupExists:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            successCB(sm.hasSoup(payload.soupName));
        }
    };

    var querySoup = function (successCB, errorCB, args) {
        var payload = args[1];
        var spec = payload.querySpec;
        if (spec.queryType == "smart") throw new Error("Smart queries can only be run using runSmartQuery");
        storeConsole.debug("SmartStore.querySoup:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",indexPath=" + spec.indexPath);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            var qs;
            switch (spec.queryType)
            {
                case "exact":
                    qs = smartStore.QuerySpec.buildExactQuerySpec(payload.soupName, spec.path, spec.exactMatchKey, spec.pageSize);
                    break;
                case "like":
                    qs = smartStore.QuerySpec.buildLikeQuerySpec(payload.soupName, spec.path, spec.likeKey, spec.order, spec.pageSize);
                    break;
                case "range":
                    qs = smartStore.QuerySpec.buildRangeQuerySpec(payload.soupName, spec.path, spec.beginKey, spec.endKey, spec.order, spec.pageSize);
                    break;
                default:
                    qs = smartStore.QuerySpec.buildSmartQuerySpec(spec.smartSql, spec.pageSize);
            }
            var smart = smartStore.QuerySpec.buildSmartQuerySpec(spec.smartSql, spec.pageSize);
            successCB(sm.query(qs, payload.pageIndex));
        }
    };

    var runSmartQuery = function (successCB, errorCB, args) {
        var payload = args[1];
        var spec = payload.querySpec;
        storeConsole.debug("SmartStore.runSmartQuery:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",indexPath=" + spec.indexPath);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            var smart = smartStore.QuerySpec.buildSmartQuerySpec(spec.smartSql, spec.pageSize);
            successCB(sm.query(smart, payload.pageIndex));
        }
    };

    var retrieveSoupEntries = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.soupExists:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",entryIds=" + payload.entryIds);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            successCB(sm.retrieve(payload.soupName, payload.entryIds));
        }
    };

    var upsertSoupEntries = function (successCB, errorCB, args) {
        args[1].externalIdPath = "_soupEntryId";
        upsertSoupEntriesWithExternalId(successCB, errorCB, args);
    };

    var upsertSoupEntriesWithExternalId = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.upsertSoupEntries:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",entries=" + payload.entries.length + ",externalIdPath=" + payload.externalIdPath);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            payload.entries.forEach(function (record) {
                sm.upsert(payload.soupName, JSON.stringify(record), payload.externalIdPath)
            });
            successCB(payload.entries);
        }
    };

    var removeFromSoup = function (successCB, errorCB, args) {
        var payload = args[1];
        storeConsole.debug("SmartStore.removeFromSoup:isGlobalStore=" + payload.isGlobalStore + ",soupName=" + payload.soupName + ",entryIds=" + payload.entryIds);
        var sm = getSmartStore(payload.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            successCB(sm.delete(payload.soupName, payload.entryIds, true));
        }
    };

    //====== Cursor manipulation ======
    var moveCursorToPageIndex = function (successCB, errorCB, args) {
        var cursor = args[1];
        storeConsole.debug("moveCursorToPageIndex:isGlobalStore=" + cursor.isGlobalStore + ",cursorId=" + cursor.cursorId + ",newPageIndex=" + cursor.newPageIndex);
        if (!sm) {
            errorCB("No active account");
        } else {
            successCB(sm.moveCursorToPageIndex(curor.cursorId, cursor.pageIndex));
        }
    };

    var moveCursorToNextPage = function (successCB, errorCB, args) {
        var payload = args[1];
        var newPageIndex = cursor.currentPageIndex + 1;
        if (newPageIndex >= cursor.totalPages) {
            errorCB(cursor, new Error("moveCursorToNextPage called while on last page"));
        } else {
            moveCursorToPageIndex(isGlobalStore, cursor, newPageIndex, successCB, errorCB);
        }
    };

    var moveCursorToPreviousPage = function (successCB, errorCB, args) {
        var cursor = args[1];
        var newPageIndex = cursor.currentPageIndex - 1;
        if (newPageIndex < 0) {
            errorCB(cursor, new Error("moveCursorToPreviousPage called while on first page"));
        } else {
            var sm = getSmartStore(cursor.isGlobalStore);
            if (!sm) {
                errorCB("No active account");
            } else {
                successCB(sm.moveCursorToPageIndex(cursor.cursorId, cusor.numberIndex));
            }
        }
    };

    var closeCursor = function (successCB, errorCB, args) {
        var cursor = args[1];
        storeConsole.debug("closeCursor:isGlobalStore=" + cursor.isGlobalStore + ",cursorId=" + cursor.cursorId);
        var sm = getSmartStore(cursor.isGlobalStore);
        if (!sm) {
            errorCB("No active account");
        } else {
            if (typeof cursor.cursorId == 'undefined')
                successCB();
            else
                successCB(sm.closeCursor(cursor.cursorId));
        }
    };

    /**
     * Part of the module that is public
     */
    module.exports = {
        pgAlterSoup: alterSoup,
        pgBuildAllQuerySpec: buildAllQuerySpec,
        pgBuildExactQuerySpec: buildExactQuerySpec,
        pgBuildLikeQuerySpec: buildLikeQuerySpec,
        pgBuildRangeQuerySpec: buildRangeQuerySpec,
        pgBuildSmartQuerySpec: buildSmartQuerySpec,
        pgClearSoup: clearSoup,
        pgCloseCursor: closeCursor,
        pgGetDatabaseSize: getDatabaseSize,
        getLogLevel: getLogLevel,
        pgGetSoupIndexSpecs: getSoupIndexSpecs,
        pgMoveCursorToNextPage: moveCursorToNextPage,
        pgMoveCursorToPageIndex: moveCursorToPageIndex,
        pgMoveCursorToPreviousPage: moveCursorToPreviousPage,
        pgQuerySoup: querySoup,
        pgReIndexSoup: reIndexSoup,
        pgRegisterSoup: registerSoup,
        pgRemoveFromSoup: removeFromSoup,
        pgRemoveSoup: removeSoup,
        pgRetrieveSoupEntries: retrieveSoupEntries,
        pgRunSmartQuery: runSmartQuery,
        setLogLevel: setLogLevel,
        pgShowInspector: showInspector,
        pgSoupExists: soupExists,
        pgUpsertSoupEntries: upsertSoupEntries,
        pgUpsertSoupEntriesWithExternalId: upsertSoupEntriesWithExternalId,

        // Constructors
        QuerySpec: QuerySpec,
        SoupIndexSpec: SoupIndexSpec,
        StoreCursor: StoreCursor
    };

    require("cordova/exec/proxy").add(SERVICE, module.exports);
