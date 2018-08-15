angular.module('layout').controller('CodeSystemCreatorController', ['$scope', '$rootScope', function ($scope, $rootScope) {

    $rootScope.$watch(function (s) { return s.session; }, function (n, o) {
        if (n)
            QueryTool.execute("GET", "/hdsi/CodeSystem", {
                headers: {
                    "Accept": "application/json+sdb-viewmodel",
                },
                continueWith: function (data) {
                    $scope.filteredCodeSystems = $scope.codeSystems = data.item;
                    $scope.$apply();
                }
            });
    });

    $scope.filterResults = function (searchTerm) {
        $scope.filteredCodeSystems = $scope.codeSystems.filter(function (match) {
            return searchTerm == null || searchTerm == "" || (
                match.oid != null && match.oid.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                match.name != null && match.name.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                match.authority != null && match.authority.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1);
        })
    }

    $scope.removeMnemonic = function(term) {
        term.concept.mnemonic = null;
        term.concept.id = null;
    }

    $scope.selectCodeSystem = function (cs) {
        $scope.current = cs;

        //fetch terms
        QueryTool.execute("GET", "/hdsi/ReferenceTerm", {
            query: "codeSystem=" + $scope.current.id + "&_viewModel=full",
            headers: {
                "Accept": "application/json+sdb-viewmodel",
            },
            continueWith: function(data) {
                $scope.current.refTerms = data.item.map(function(term) {
                    var names = term.name;
                    term.name = [];
                    for(var k in names)
                        term.name.push({language: k, value: names[k]});

                    var retVal = {
                        term: term,
                        concept: {
                            id: null,
                            mnemonic: null
                        }
                    };
                    QueryTool.execute("GET", "/hdsi/Concept", {
                        query: "referenceTerm.term=" + term.id + "&_lean=true&_count=1", 
                        continueWith: function(d) {
                            if(d.item && d.item.length == 1){
                                retVal.concept.id =  d.item[0].id;
                                retVal.concept.mnemonic = d.item[0].mnemonic;
                                try { $scope.$apply(); } catch {}
                            }
                        }
                    });
                    return retVal;
                });
                $scope.$apply();
            }
        });
    }

    $scope.addTerm = function() {
        $scope.current.refTerms.push({
            term: { 
                name: [
                    { language : "en", value : "" }
                ]
    }
        })
    };

    $scope.removeTerm = function(i) {
        $scope.current.refTerms.splice(i, 1);
    };
    
    $scope.cancel = function() {
        if(confirm("Are you sure you want to cancel your changes?"))
            $scope.current = null;
    }

    

    $scope.new = function () {
        $scope.current = {
            $type: "CodeSystem",
            refTerms: [
                {
                    term: {
                        name: [
                            { language : "en", value : "" }
                        ]
                    }
                }
            ]
        }
    }
}]);