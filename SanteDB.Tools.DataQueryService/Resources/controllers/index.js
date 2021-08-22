/// <reference path="../js/jquery.min.js"/>
/// <reference path="../js/query-tool.js"/>
/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
var layoutApp = angular.module('layout', ['santedb']).run();


angular.module('layout').controller('IndexController', ['$scope', '$rootScope', function ($scope, $rootScope) {
    QueryTool.getConfiguration($rootScope);

    $scope.search = { query: { _lean: true } };

    $scope.login = function () {
        QueryTool.authenticate();
    }
    // Query builder push
    $scope.addTerm = function () {
        $scope.search.queryBuild.push({
            op: "="
        });
    }
    $scope.removeTerm = function (i) {
        $scope.search.queryBuild.splice(i, 1);
    }
    $scope.buildQueryString = function () {
        var retVal = "";
        for (var i in $scope.search.queryBuild) {
            var k = $scope.search.queryBuild[i];
            if (k.key)
                retVal += k.key;
            if (k.op && k.key)
                retVal += k.op;
            if (k.val)
                retVal += k.val + "&";
        }

        for (var i in $scope.search.query) {
            if ($scope.search.query[i] !== null)
                retVal += i + "=" + $scope.search.query[i] + "&";
        }

        if (retVal.endsWith("&"))
            retVal = retVal.substr(0, retVal.length - 1);

        return retVal;
    }
    $scope.reset = function () {
        if (confirm("Are you sure you want to reset your query?")) {
            $scope.search.resourceType = null;
        }
    }
    $scope.search = function (inputForm) {

        if (!inputForm.$valid) {
            alert("Your search is invalid. Please correct fields hilighted and try again");
            return;
        }
        $scope.search.searching = true;
        $scope.results = null;
        var start = new Date();
        QueryTool.execute("GET", $rootScope.realm + "/hdsi/" + $scope.search.resourceType, {
            query: $scope.buildQueryString(),
            headers: {
                "Accept": $scope.search.contentType
            },
            continueWith: function (e) {
                $scope.results = e;
            },
            onException: function (e) {
                alert(e.message || e);
            },
            finally: function (e) {
                $scope.search.searching = false;
                $scope.results.ttr = new Date().getTime() - start;
                $scope.$apply();
            }
        });
    }

    $scope.hasParameter = function (resource) {
        for (var p in $rootScope.configuration.parameters) {
            if ($rootScope.configuration.parameters[p].scope.indexOf(resource.resource) > -1)
                return true && resource.cap.indexOf('Get') > -1;
        }
        return false;
    }
    // Realm watch
    $rootScope.$watch('realm', function (n, o) {
        if (o && confirm("You are about to switch realms. Do you want to do this?") || n) {
            QueryTool.$authentication = {};
            QueryTool.execute("OPTIONS", n + "/hdsi/", {
                continueWith: function (e) {
                    // Set the supported options
                    $rootScope.configuration.resources = e.resource;
                    $scope.search.resourceType = null;
                    $scope.search.contentType = "application/json";
                    $scope.search.query = { _lean: true }
                }
            });
            QueryTool.authenticate();
        }
    });


    // Resource type filter
    $scope.$watch("search.resourceType", function (n, o) {
        $scope.search.queryBuild = [{ op: "=" }];
    });

}]);