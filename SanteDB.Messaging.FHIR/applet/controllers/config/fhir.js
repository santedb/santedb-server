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