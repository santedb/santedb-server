/// <reference path="../js/jquery.min.js"/>
/// <reference path="../js/query-tool.js"/>

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
                    catch {}
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