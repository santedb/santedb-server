﻿/// <reference path="jquery.min.js"/>

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
                scope : url + "/hdsi" 
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