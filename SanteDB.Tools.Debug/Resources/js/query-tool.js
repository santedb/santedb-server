/// <reference path="jquery.min.js"/>
/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2021-8-27
 */
var QueryTool = {
    $authentication: null,
    authenticate: function (url, controlData) {
        if(!url)
            $("#dlgAuthenticate").modal('show');
        else {
            var qdata = {
                grant_type : "password",
                username : controlData.username,
                password : controlData.password,
                scope : "*" 
            };
            var headers = {};

            if(controlData.mode == "qstring") {
                qdata.client_secret = "fiddler";
                qdata.client_id = "fiddler";
            }
            else 
                headers = {
                    "Authorization" : "BASIC " + btoa("fiddler:fiddler")
                }
            
            QueryTool.execute("POST", url + "/auth/oauth2_token", {
                data: qdata,
                headers: headers,
                contentType: "application/x-www-form-urlencoded",
                continueWith: controlData.continueWith,
                onException: controlData.onException,
                finally: controlData.finally
            });
        }
    },
    getConfiguration: function (scopeVar) {
        $.getJSON("config.json", function (result) {
            scopeVar.configuration = result;
            scopeVar.configuration.resources = scopeVar.configuration.parameters.id.scope.map(o => { return { resource: o, cap: [ "Get" ] } });
            scopeVar.$apply();
        });
    },
    execute: function (operation, url, controlData) {
        return SanteDB.Http.execute(operation, url, controlData);
    }
};

$.ready(function () {
    $("div.modal").modal();
});
$.ajaxSetup({
    cache: false,
    beforeSend: function (data, settings) {
        if (QueryTool.$authentication &&
            QueryTool.$authentication.length) {
            data.setRequestHeader("Authorization", "Bearer " +
                QueryTool.$authentication);
        }
    },
    converters: {
        "text json viewModel": function (data) {
            return $.parseJSON(data, true);
        }
    }
});

// Handles error conditions related to expiry
$(document).ajaxError(function (e, data, setting, err) {
    if ((data.status == 401 || data.status == 403)) {
        QueryTool.authenticate();
    }
    else
        console.error(err);
});