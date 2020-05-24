/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
angular.module('layout').controller('CodeSystemCreatorController', ['$scope', '$rootScope', function ($scope, $rootScope) {

    var uuidv4 = function() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
          var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
          return v.toString(16);
        });
      };

    $rootScope.$watch(function (s) { return s.session; }, function (n, o) {
        if (n) {
            QueryTool.execute("GET", "/hdsi/CodeSystem", {
                headers: {
                    "Accept": "application/json+sdb-viewmodel",
                },
                continueWith: function (data) {
                    $scope.filteredCodeSystems = $scope.codeSystems = data.item;
                    $scope.$apply();
                }
            });
            QueryTool.execute("GET", "/hdsi/ConceptClass", {

                continueWith: function(d) {
                    $scope.conceptClasses = d.item;
                }
            });
        }
    });

    $scope.importTerms = function(conceptSet) {
        if(conceptSet != "" && conceptSet != null) {
            if($scope.current.refTerms.length > 0 && !confirm("This action will erase the registered reference terms. Continue?")) return;
            QueryTool.execute("GET", "/hdsi/ConceptSet/" + conceptSet, {
                headers: { Accept: "application/json+sdb-viewmodel"},
                continueWith: function(d) {
                    $scope.current.oid = $scope.current.oid || d.oid;
                    $scope.current.url = $scope.current.url || d.url;
                    $scope.current.authority = $scope.current.authority || d.mnemonic;
                    $scope.current.refTerms = [];
                    for(var k in d.concept) {
                        QueryTool.execute("GET", "/hdsi/Concept/" + d.concept[k], {
                            continueWith: function(c) {
                                $scope.current.refTerms.push({
                                    term: {
                                        id: uuidv4(),
                                        mnemonic: c.mnemonic,
                                        name: [
                                            {
                                                value: c.name[0].value,
                                                language: c.name[0].language
                                            }
                                        ]
                                    },
                                    concept: {
                                        id: c.id,
                                        mnemonic: c.mnemonic
                                    }
                                });
                                try { $scope.$apply(); } catch {}
                            }
                        })
                    }
                }
            });
        }
    }
    $scope.filterResults = function (searchTerm) {
        $scope.filteredCodeSystems = $scope.codeSystems.filter(function (match) {
            return searchTerm == null || searchTerm == "" || (
                match.oid != null && match.oid.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                match.name != null && match.name.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                match.authority != null && match.authority.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1);
        })
    }

    $scope.createConcept = function(r) {
        $("#dlgCreateConcept").modal('show');
        r.concept = {};
        r.concept.id = "00000000-0000-0000-0000-000000000000";
        r.concept.$type = "Concept";
        r.concept.name = {
            value: "",
            language: "en"
        };
        r.concept.statusConcept = "C8064CBD-FA06-4530-B430-1A52F1530C27"
        $scope.newConcept = r.concept;
    }

    $scope.removeMnemonic = function(term) {
        term.concept.mnemonic = null;
        term.concept.id = null;
    }

    $scope.selectCodeSystem = function (cs) {
        $scope.current = cs;

        //fetch terms
        QueryTool.execute("GET", "/hdsi/ReferenceTerm", {
            query: "codeSystem=" + $scope.current.id + "&_viewModel=full&_count=1000",
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
                id: uuidv4(),
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
        if(confirm("Are you sure you want to cancel your changes?")) {
            $scope.current = null;
            $scope.bundle = null;
        }
    }

    $scope.download = function() {
        var refTerms = $scope.current.refTerms;
        var codeSystem = angular.copy($scope.current);
        delete(codeSystem.refTerms);
        
        // Create a bundle
        var bundle = { $type: "Bundle", item: [codeSystem] };
        // reference terms
        refTerms.forEach(function(r) {
            var names = {};
            r.term.name.forEach(function(n) { 
                names[n.language] = n.value;
            });
            bundle.item.push({
                $type: "ReferenceTerm",
                id: r.term.id,
                mnemonic: r.term.mnemonic,
                name: names,
                codeSystem: codeSystem.id
            });

            // Is the concept new?
            var conceptId = r.concept.id;
            if(conceptId == "00000000-0000-0000-0000-000000000000") {
                conceptId = uuidv4();
                names = {};
                names[r.concept.name.language] = r.concept.name.value;
                var cncpt = {
                    $type: "Concept",
                    id: conceptId,
                    mnemonic: r.concept.mnemonic,
                    conceptClass: r.concept.conceptClass,
                    name: names,
                    statusConcept: r.concept.statusConcept
                };

                if(r.concept.conceptSet)
                    cncpt.conceptSet = [r.concept.conceptSet];
                bundle.item.push(cncpt);
            }
            bundle.item.push({
                $type: "ConceptReferenceTerm",
                id: uuidv4(),
                relationshipType: "2c4dafc2-566a-41ae-9ebc-3097d7d22f4a",
                source: conceptId, 
                term: r.term.id
            });
        });

        $.ajax({ 
            type: "POST", 
            dataType: "xml", 
            contentType: "application/json", 
            url: "./dataset", 
            data: JSON.stringify(bundle), 
            success: function(data, status, jqXHR) {
                $scope.bundle = jqXHR.responseText;
                $scope.$apply();
            }
        });

    }

    $scope.new = function () {
        $scope.current = {
            $type: "CodeSystem",
            id: uuidv4(),
            refTerms: [
                {
                    term: {
                        id: uuidv4(),
                        name: [
                            { language : "en", value : "" }
                        ]
                    }
                }
            ]
        }
    }
}]);