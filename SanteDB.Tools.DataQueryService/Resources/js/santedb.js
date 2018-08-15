var SanteDB = {

    Localization: {
        /**
         * @summary Gets the current user interface locale name
         * @memberof OpenIZ.Localization
         * @method
         * @returns The ISO language code of the current UI 
         */
        getLocale: function () {
            return (navigator.language || navigator.userLanguage).substring(0, 2);
        },
    },
    Http: {
        /**
         * @method
         * @summary Execute an HTTP operation
         */
        execute: function (operation, url, controlData) {
            controlData.onException = controlData.onException || function (e) { console.error(e); };

            var ajReq = {
                method: operation,
                url: controlData == null || controlData.query == null ? url : url + "?" + controlData.query,
                data: controlData.data && controlData.contentType != "application/x-www-form-urlencoded" ? JSON.stringify(controlData.data) : controlData.data,
                dataType: controlData.contentType == "application/x-www-form-urlencoded" ? null : "json",
                accepts: {
                    viewModel: "application/json+sdb-viewmodel"
                },
                cache: false,
                headers: controlData.headers,
                contentType: controlData.contentType || (controlData.data ? 'application/json+sdb-viewmodel' : null),
                success: function (xhr, data) {

                    if (controlData.continueWith !== undefined)
                        controlData.continueWith(xhr, controlData.state);

                    if (controlData.finally !== undefined)
                        controlData.finally(controlData.state);
                },
                error: function (data) {
                    var error = data.responseJSON;
                    if (controlData.onException === null)
                        console.error(error);
                    else if (error && error.error !== undefined) // oauth 2 error
                        controlData.onException({
                            "type": error.type,
                            "error": error.error,
                            "description": error.error_description,
                            "caused_by": error.caused_by
                        }, controlData.state);

                    else // unknown error
                        controlData.onException({
                            "type": "Exception",
                            "error": "err_general" + error,
                            "description": data,
                        }, controlData.state);

                    if (controlData.finally !== undefined)
                        controlData.finally(controlData.state);
                }
            };

            $.ajax(ajReq);
        }
    },
    UserInterface: {
        /** 
         * @summary Renders the specified concept name from a DOM option
         * @memberof OpenIZ.Util
         * @method
         * @param {OpenIZModel.ConceptName} name The concept name to be rendered
         */
        renderConceptFromDom: function (val) {
            if (val)
                return $("option[value=" + val + "]").first().text();
        },
        /** 
         * @summary Renders the specified concept name
         * @memberof OpenIZ.Util
         * @method
         * @param {OpenIZModel.ConceptName} name The concept name to be rendered
         */
        renderConceptName: function (name) {
            var retVal = "";
            if (name == null)
                retVal = "";
            else if (typeof (name) == "String") retVal = name;
            else if (name[SanteDB.Localization.getLocale()] != null)
                retVal = name[SanteDB.Localization.getLocale()];
            else
                retVal = name[Object.keys(name)];

            if (Array.isArray(retVal))
                return retVal[0];
            else
                return retVal;
        },
        /**
         * @summary Render address for display
         * @method
         * @memberof OpenIZ.Util
         * @param {OpenIZModel.EntityAddress} entity The addres of the entity or the entity itself to render the address of
         * @return {string} The address formatted as an appropriate string for simple formatting
         */
        renderAddress: function (entity) {
            if (entity === undefined) return;

            var address = entity.component !== undefined ? entity :
                entity.address !== undefined ? (entity.address.Direct || entity.address.HomeAddress || result.name.$other) :
                (entity.Direct || entity.HomeAddress || entity.$other);
            var retVal = "";
            if (address.component) {
                if (address.component.AdditionalLocator)
                    retVal += address.component.AdditionalLocator + ", ";
                if (address.component.StreetAddressLine)
                    retVal += address.component.StreetAddressLine + ", ";
                if (address.component.Precinct)
                    retVal += address.component.Precinct + ", ";
                if (address.component.City)
                    retVal += address.component.City + ", ";
                if (address.component.County != null)
                    retVal += address.component.County + ", ";
                if (address.component.State != null)
                    retVal += address.component.State + ", ";
                if (address.component.Country != null)
                    retVal += address.component.Country + ", ";
            }
            return retVal.substring(0, retVal.length - 2);
        },
        /**
         * @summary Render act as a simple string
         * @memberof OpenIZ.Util
         * @method
         * @param {OpenIZModel.Act} act The act to render as a simple string
         * @return {string} The rendered string 
         */
        renderAct: function (act) {
            switch (act.$type) {
                case "SubstanceAdministration":
                    return SanteDB.Localization.getString("locale.encounters.administer") +
                        SanteDB.Util.renderName(act.participation.Product.name.OfficialRecord);
                case "QuantityObservation":
                case "CodedObservation":
                case "TextObservation":
                    return SanteDB.Localization.getString('locale.encounters.observe') +
                        act.typeConceptModel.name[SanteDB.Localization.getLocale()];
                default:
                    return "";
            }
        },
        /** 
         * @summary Render a manufactured material as a simple string
         * @method
         * @memberof OpenIZ.Util
         * @param {OpenIZModel.ManufacturedMaterial} material The material which is to be rendered as a string
         * @return {string} The material rendered as a string in format "<<name>> [LN# <<ln>>]"
         */
        renderManufacturedMaterial: function (material) {
            var name = SanteDB.UserInterface.renderName(material.name.OfficialRecord || material.name.Assigned);
            return name + "[LN#: " + material.lotNumber + "]";
        },
        /** 
         * @summary Renders a name as a simple string
         * @method
         * @meberof OpenIZ.Util
         * @param {OpenIZModel.EntityName} entityName The entity name to be rendered in the appropriate format
         * @return {string} The rendered entity name
         */
        renderName: function (entityName) {
            if (entityName === null || entityName === undefined)
                return "";
            else if (entityName.join !== undefined)
                return entityName.join(' ');
            else if (typeof (entityName) === "string")
                return entityName;
            else {
                if (entityName.component === undefined)
                    entityName = entityName[Object.keys(entityName)[0]];
                if (entityName.component !== undefined) {
                    var nameStr = "";
                    if (entityName.component.Given !== undefined) {
                        if (typeof (entityName.component.Given) === "string")
                            nameStr += entityName.component.Given;
                        else if (entityName.component.Given.join !== undefined)
                            nameStr += entityName.component.Given.join(' ');
                        nameStr += " ";
                    }
                    if (entityName.component.Family !== undefined) {
                        if (typeof (entityName.component.Family) === "string")
                            nameStr += entityName.component.Family;
                        else if (entityName.component.Family.join !== undefined)
                            nameStr += entityName.component.Family.join(' ');
                    }
                    if (entityName.component.$other !== undefined) {
                        if (typeof (entityName.component.$other) === "string")
                            nameStr += entityName.component.$other;
                        else if (entityName.component.$other.join !== undefined)
                            nameStr += entityName.component.$other.join(' ');
                        else if (entityName.component.$other.value !== undefined)
                            nameStr += entityName.component.$other.value;

                    }
                    return nameStr;
                }
                else
                    return "";
            }
        }
    }
}