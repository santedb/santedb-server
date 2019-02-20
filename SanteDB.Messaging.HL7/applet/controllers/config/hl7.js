angular.module('santedb').controller('HL7ConfigurationController', ['$scope', '$rootScope', function ($scope, $rootScope) {
    
    // Watch the configuration
    $scope.$parent.$watch("config", function(n, o) {
        if(n) {
            $scope.config = n;
            $scope.hl7Config = $scope.config.others.find(function(s) {
                return s.$type == "SanteDB.Messaging.HL7.Configuration.Hl7ConfigurationSection, SanteDB.Messaging.HL7";
            });

            if($scope.hl7Config)
                delete($scope.hl7Config.localAuthority.$type);
        }
    });
    
    $scope.$parent.$watch("config.sync.subscribe", function(n, o) {
        if(n && $scope.hl7Config) {
            $scope.hl7Config.facility = n[0];
        }
    })
    
}]);