/// <reference path="santedb.js"/>
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

/**
 * @version 0.9.6 (Edmonton)
 * @copyright (C) 2015-2017, Mohawk College of Applied Arts and Technology
 * @license Apache 2.0
 */

/**
 * @summary Represents SanteDB bindings for angular.
 * @description The purpose of these functions are to provide a mechanism to leverage SanteDB functionality within AngularJS
 * @class Angular
 */
angular.module('santedb', [])
    /**
     * @method sdbSum
     * @memberof Angular
     * @summary Sums the values from the model (an array) together 
     * @example
     *      <span class="label label-info">{{ 'locale.total' | i18n }} : {{ act.participation | oizSum: 'quantity' }}</span>
     */
    .filter('sdbSum', function () {
        return function (modelValue, propname) {
            // TODO: Find a better function for doing this
            var sum = 0;
            if (!Array.isArray(modelValue))
                ;
            else if (!propname) {
                for (var i in modelValue)
                    sum += modelValue[i]
            }
            else
                for (var i in modelValue)
                    sum += modelValue[i][propname];
            return sum;
        };
    })
    /**
     * @method sdbEntityIdentifier
     * @memberof Angular
     * @summary Renders a model value which is an EntityIdentifier in a standard way
     * @see {SanteDBModel.EntityIdentifier}
     * @example
     *      <div class="col-md-2">{{ patient.identifier | oizEntityIdentifier }}</div>
     */
    .filter('sdbEntityIdentifier', function () {
        return function (modelValue) {
            if (modelValue === undefined)
                return "";
            else
                for (var k in modelValue)
                    return modelValue[k].value;
        };
    })
    /**
     * @method sdbConcept
     * @memberof Angular
     * @summary Renders a model concept into a standard display using the concept's display name
     * @see {SanteDBModel.Concept}
     * @example
     *      <div class="col-md-2">Gender:</div>
     *      <div class="col-md-2">{{ patient.genderConceptModel | oizConcept }}</div>
     */
    .filter('sdbConcept', function () {
        return function (modelValue) {
            if (modelValue != null && modelValue.name != null)
                return SanteDB.UserInterface.renderConceptName(modelValue.name);
        }
    })
    /**
     * @method oizEntityName
     * @memberof Angular
     * @summary Renders an entity's name in a standard display format
     * @example
     *      <div class="col-md-2">Name:</div><div class="col-md-4">{{ patient.name.OfficialRecord | oizEntityName }}</div>
     */
    .filter('sdbEntityName', function () {
        return function (modelValue) {
            return SanteDB.UserInterface.renderName(modelValue);
        }
    })
    /** 
     * @method oizEntityAddress
     * @memberof Angular
     * @summary Renders an entity's address in a standardized display form
     * @description This function will render the entity's specified address in the format Street, Precinct, City, County, State, Country
     * @example
     *      <div class="col-md-2">Address:</div><div class="col-md-6">{{ patient.address.Home | oizEntityAddress }}</div>
     */
    .filter('sdbEntityAddress', function () {
        return function (modelValue) {
            return SanteDB.UserInterface.renderAddress(modelValue);
        }
    })
    /** 
     * @method datePrecisionFormat
     * @memberof Angular
     * @sumamry Renders the input date using the specified format identifier for date precision.
     * @param {int} format The format or date precision: Y, m, D, F, etc.
     * @see {SanteDB.App.DatePrecisionFormats}
     * @description To override the formatting for the specified date precision you must override the linked setting in the openiz.js file by setting it in your program
     * @example mycontroller.js
     *      ...
     *       SanteDB.App.DatePrecisionFormats = {
     *       DateFormatYear: 'YYYY',
     *       DateFormatMonth: 'YYYY-MM',
     *       DateFormatDay: 'YYYY-MM-DD',
     *       DateFormatHour: 'YYYY-MM-DD HH',
     *       DateFormatMinute: 'YYYY-MM-DD HH:mm',
     *       DateFormatSecond: 'YYYY-MM-DD HH:mm:ss'
     *   },  // My own precision formats
     * @example mmyview.html
     *      <div class="col-md-2">DOB:</div><div class="col-md-4">{{ patient.dateOfBirth | datePrecisionFormat: patient.dateOfBirthPrecision }}</div>
     *      <div class="col-md-2">Created On:</div><div class="col-md-4">{{ patient.creationTime | datePrecisionFormat: 'D' }}</div>
     */
    .filter('sdbDatePrecisionFormat', function () {
        return function (date, format) {
            var dateFormat;

            switch (format) {
                case 1:   // Year     "Y"
                case 'Y':
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatYear;
                    break;
                case 2:   // Month    "m"
                case 'm':
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatMonth;
                    break;
                case 3:   // Day      "D"
                case 'D':
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatDay;
                    break;
                case 4:   // Hour     "H"
                case 'H':
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatHour;
                    break;
                case 5:   // Minute   "M"
                case 'M':
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatMinute;
                    break;
                case 6:   // Second   "S"
                case 'S':
                case 0:   // Full     "F"
                case 'F':
                default:
                    dateFormat = SanteDB.App.DatePrecisionFormats.DateFormatSecond;
                    break;
            }

            if (date) {
                // Non timed
                switch (format) {
                    case 1:   // Year, Month, Day always expressed in UTC for Javascript will take the original server value and adjust.
                    case 'Y':
                    case 2:
                    case 'm':
                    case 3:
                    case 'D':
                        return moment(date).utc().format(dateFormat);
                    default:
                        return moment(date).format(dateFormat);
                }
            }

            return null;
        };
    })
    /**
     * @method sdbSearch
     * @memberof Angular
     * @summary Binds a select2 search box to the specified select input searching for the specified entities
     * @description This class is the basis for all drop-down searches in SanteDB disconnected client. It is used whenever you would like to have a search inline in a form and displayed nicely
     * @param {string} value The type of object to be searched
     * @param {string} filter The additional criteria by which results should be filtered
     * @param {string} data-searchField The field which should be searched on. The default is name.component.value
     * @param {string} data-default The function which returns a list of objects which represent the default values in the search
     * @param {string} data-groupBy The property which sets the grouping for the results in the drop-down display
     * @param {string} data-groupDisplay The property on the group property which is to be displayed
     * @param {string} data-resultField The field on the result objects which contains the result
     */
    .directive('sdbSearch', function ($timeout) {
        return {
            scope: {
                defaultResults: '='
            },
            link: function (scope, element, attrs, ctrl) {
                $timeout(function () {
                    var modelType = attrs.searchtype || attrs.sdbSearch;
                    var filterString = attrs.filter;
                    var displayString = attrs.display;
                    var searchProperty = attrs.searchfield || "name.component.value";
                    var defaultResults = attrs.default;
                    var groupString = attrs.groupBy;
                    var groupDisplayString = attrs.groupDisplay;
                    var resultProperty = attrs.resultfield || "id";
                    var baseUrl = attrs.url;
                    var filter = {}, defaultFilter = {};
                    if (filterString !== undefined &&
                        filterString != "")
                        filter = JSON.parse(filterString);

                    if (modelType != "SecurityUser" && modelType != "SecurityRole" && modelType != "ConceptSet" && !filter.statusConcept)
                        filter.statusConcept = 'C8064CBD-FA06-4530-B430-1A52F1530C27';

                    // Add appropriate styling so it looks half decent


                    // Bind select 2 search
                    $(element).select2({
                        defaultResults: function () {
                            var s = scope;
                            if (defaultResults != null) {
                                try {
                                    return eval(defaultResults);
                                } catch (e) {

                                }
                            }
                            else {
                                return $.map($('option', element[0]), function (o) {
                                    return { "id": o.value, "text": o.innerText };
                                });
                            }
                        },
                        dataAdapter: $.fn.select2.amd.require('select2/data/extended-ajax'),
                        ajax: {
                            url: baseUrl + ((modelType == "SecurityUser" || modelType == "SecurityRole") ? "/ami/" : "/hdsi/") + modelType,
                            dataType: 'json',
                            delay: 500,
                            method: "GET",
                            headers: {
                                "Accept" : "application/json+sdb-viewmodel"
                            },
                            cache: false,
                            data: function (params) {
                                filter[searchProperty] = "~" + params.term;
                                filter["_count"] = 20;
                                filter["_offset"] = 0;
                                filter["_viewModel"] = "min";
                                filter["_s"] = new Date().getTime();
                                filter["_lean"] = true;
                                return filter;
                            },
                            processResults: function (data, params) {
                                //params.page = params.page || 0;
                                var data = data.$type == "Bundle" ? data.resource : data.resource || data;
                                var retVal = { results: [] };

                                if (groupString == null && data !== undefined) {
                                    retVal.results = retVal.results.concat($.map(data, function (o) {
                                        var text = "";
                                        if (displayString) {
                                            scope = o;
                                            text = eval(displayString);
                                        }
                                        else if (o.name !== undefined) {
                                            if (o.name.OfficialRecord) {
                                                text = SanteDB.UserInterface.renderName(o.name.OfficialRecord);
                                            } else if (o.name.Assigned) {
                                                text = SanteDB.UserInterface.renderName(o.name.Assigned);
                                            }
                                        }
                                        o.text = o.text || text;
                                        o.id = o[resultProperty];
                                        return o;
                                    }));
                                }
                                else {
                                    // Get the group string
                                    for (var itm in data) {
                                        // parent obj
                                        try {
                                            var scope = eval('data[itm].' + groupString);
                                            var groupDisplay = "";
                                            if (groupDisplayString != null)
                                                groupDisplay = eval(groupDisplayString);
                                            else
                                                groupDisplay = scope;

                                            var gidx = $.grep(retVal.results, function (e) { return e.text == groupDisplay });
                                            if (gidx.length == 0)
                                                retVal.results.push({ "text": groupDisplay, "children": [data[itm]] });
                                            else
                                                gidx[0].children.push(data[itm]);
                                        }
                                        catch (e) {
                                            retVal.results.push(data[itm]);
                                        }
                                    }
                                }
                                return retVal;
                            },
                            cache: true
                        },
                        escapeMarkup: function (markup) { return markup; }, // Format normally
                        minimumInputLength: 2,
                        templateSelection: function (selection) {
                            var retVal = "";
                            switch (modelType) {
                                case "UserEntity":
                                case "Provider":
                                case "Person":
                                case "Patient":
                                    retVal += "<span class='glyphicon glyphicon-user'></span>";
                                    break;
                                case "Place":
                                    retVal += "<span class='glyphicon glyphicon-map-marker'></span>";
                                    break;
                                case "Entity":
                                    retVal += "<span class='glyphicon glyphicon-tag'></span>";
                                    break;
                                case "Concept":
                                    retVal += "<span class='glyphicon glyphicon-book'></span>";
                                    break;
                            }
                            retVal += "&nbsp;";


                            if (displayString != null) {
                                var scope = selection;
                                retVal += eval(displayString);
                            }
                            else if (selection.name != null && selection.name.OfficialRecord != null)
                                retVal += SanteDB.UserInterface.renderName(selection.name.OfficialRecord);
                            else if (selection.name != null && selection.name.Assigned != null)
                                retVal += SanteDB.UserInterface.renderName(selection.name.Assigned);
                            else if (selection.name != null && selection.name.$other != null)
                                retVal += SanteDB.UserInterface.renderName(selection.name.$other);
                            else if (selection.text)
                                retVal += selection.text;
                            else if (selection.mnemonic)
                                retVal += selection.mnemonic;
                            else if (selection.element !== undefined)
                                retVal += selection.element.innerText.trim();
                            if (selection.address)
                                retVal += " - <small>(<i class='fa fa-map-marker'></i> " + SanteDB.UserInterface.renderAddress(selection.address) + ")</small>";

                            return retVal;
                        },
                        keepSearchResults: true,
                        templateResult: function (result) {
                            if (result.loading) return result.text;

                            if (displayString != null) {
                                var scope = result;
                                return eval(displayString);
                            }
                            else if (result.classConcept != SanteDBModel.EntityClassKeys.ServiceDeliveryLocation && result.name != null && result.typeConceptModel != null && result.typeConceptModel.name != null && result.name.OfficialRecord) {
                                retVal = "<div class='label label-info'>" +
                                    result.typeConceptModel.name[SanteDB.Localization.getLocale()] + "</div> " + SanteDB.UserInterface.renderName(result.name.OfficialRecord || result.name.$other);
                                if (result.address)
                                    retVal += " - <small>(<i class='fa fa-map-marker'></i> " + SanteDB.UserInterface.renderAddress(result.address) + ")</small>";
                                return retVal;
                            }
                            else if (result.classConcept == SanteDBModel.EntityClassKeys.ServiceDeliveryLocation && result.name != null && result.typeConceptModel != null && result.typeConceptModel.name != null) {
                                retVal = "<div class='label label-info'>" +
                                   result.typeConceptModel.name[SanteDB.Localization.getLocale()] + "</div> " + SanteDB.UserInterface.renderName(result.name.OfficialRecord || result.name.Assigned || result.name.$other);
                                if (result.relationship && result.relationship.Parent && result.relationship.Parent.targetModel && result.relationship.Parent.targetModel.name)
                                    retVal += " - <small>(<i class='fa fa-map-marker'></i> " + SanteDB.UserInterface.renderName(result.relationship.Parent.targetModel.name.OfficialRecord || result.relationship.Parent.targetModel.name.Assigned) + ")</small>";
                                if (result.address)
                                    retVal += " - <small>(<i class='fa fa-map-marker'></i> " + SanteDB.UserInterface.renderAddress(result.address) + ")</small>";
                                return retVal;
                            }
                            else if (result.name != null && result.typeConceptModel != null && result.typeConceptModel.name != null && result.name.Assigned) {
                                var retVal = "<div class='label label-default'>" +
                                    result.typeConceptModel.name[SanteDB.Localization.getLocale()] + "</div> " + SanteDB.UserInterface.Util.renderName(result.name.Assigned || result.name.$other);

                                if (result.address)
                                    retVal += " - <small>(<i class='fa fa-map-marker'></i> " + SanteDB.UserInterface.Util.renderAddress(result.address) + ")</small>";
                                return retVal;
                            }
                            else if (result.name != null && result.name.OfficialRecord)
                                return "<div class='label label-default'>" +
                                    result.$type + "</div> " + SanteDB.UserInterface.renderName(result.name.OfficialRecord);
                            else if (result.name != null && result.name.Assigned)
                                return "<div class='label label-default'>" +
                                    result.$type + "</div> " + SanteDB.UserInterface.renderName(result.name.Assigned)
                            else if (result.name != null && result.name.$other)
                                return "<div class='label label-default'>" +
                                    result.$type + "</div> " + SanteDB.UserInterface.renderName(result.name.$other)
                            else if (result.mnemonic)
                                return "<div class='label label-default'><span class='glyphicon glyphicon-book'></span>" +
                                    result.$type + "</div> " + result.mnemonic;
                            else
                                return result.text;
                        }
                    });

                    //$(element).on("select2:opening", function (e) {
                    //    var s = scope;
                    //    if (defaultResults != null) {
                    //        return eval(defaultResults);
                    //    }
                    //    else {
                    //        return $.map($('option', element[0]), function (o) {
                    //            return { "id": o.value, "text": o.innerText };
                    //        });
                    //    }
                    //}
                    //);
                    // HACK: For angular values, after select2 has "selected" the value, it will be a ? string: ID ? value we do not want this
                    // we want the actual value, so this little thing corrects this bugginess
                    $(element).on("select2:select", function (e) {
                        if (e.currentTarget.value.indexOf("? string:") == 0) {
                            e.currentTarget.value = e.currentTarget.value.substring(9, e.currentTarget.value.length - 2);
                        }
                        e.currentTarget.options.selectedIndex = e.currentTarget.options.length - 1;
                        //{
                        //    while (e.currentTarget.options.length > 1)
                        //        e.currentTarget.options.splice(1);
                        //}
                    });
                });
            }
        };
    });
    