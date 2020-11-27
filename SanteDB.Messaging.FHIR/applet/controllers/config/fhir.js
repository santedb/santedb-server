/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
angular.module('santedb').controller('FhirConfigurationController', ['$scope', '$rootScope', function ($scope, $rootScope) {
    
    // Watch the configuration
    $scope.$parent.$watch("config", function(n, o) {
        if(n) {
            $scope.config = n;
            // Get FHIR configuration
            $scope.fhirConfig = $scope.config.others.find(function(s) {
                return s.$type === "SanteDB.Messaging.FHIR.Configuration.FhirServiceConfigurationSection, SanteDB.Messaging.FHIR";
            });

            // Get the FHIR service configuration for either embedded server or full server
            $scope.serviceConfig = $scope.config.others.find(function (s) {
                return s.$type === "SanteDB.Core.Configuration.RestConfigurationSection, SanteDB.Core" ||
                    s.$type === "SanteDB.DisconnectedClient.Ags.Configuration.AgsConfigurationSection, SanteDB.DisconnectedClient.Ags";
            });
            
        }
    });
  
}]);