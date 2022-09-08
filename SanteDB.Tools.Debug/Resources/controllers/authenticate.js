/// <reference path="../js/jquery.min.js"/>
/// <reference path="../js/query-tool.js"/>
/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
angular.module('layout').controller('AuthenticationController', ['$scope', '$rootScope', function ($scope, $rootScope) {

    // Perform authentication
    $scope.authenticate = function (form) {
        if (form.$invalid) return;

        if ($scope.token) {
            QueryTool.$authentication = $scope.token;
            $rootScope.session = {
                token: $scope.token,
                abandon: function () {
                    $rootScope.session = null;
                    QueryTool.$authentication = null;
                }
            };
            $("#dlgAuthenticate").modal('hide');
        }
        else
            QueryTool.authenticate($rootScope.realm, {
                username: $scope.username,
                password: $scope.password,
                mode: $scope.mode,
                continueWith: function (d) {
                    QueryTool.$authentication = d.access_token;
                    $rootScope.session = {
                        token: d.access_token,
                        abandon: function () {
                            $rootScope.session = null;
                            QueryTool.$authentication = null;
                        }
                    };
                    try {
                        $rootScope.$apply();
                    }
                    catch (err) {}
                },
                onException: function (e) {
                    alert(e.message || e);
                },
                finally: function () {
                    $("#dlgAuthenticate").modal('hide');
                }
            });
    };
}]);