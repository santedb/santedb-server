﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="0-004" name="Data Initialization" invariantName="npgsql">
 *	<summary>Initialize Data</summary>
 *	<remarks>Initializes the SanteDB database with default usernames, passwords, and applications</remarks>
 *	<isInstalled>SELECT COUNT(1) = 1 FROM SEC_USR_TBL WHERE USR_NAME = 'Administrator'</isInstalled>
 * </feature>
 */
 DELETE FROM SEC_USR_ROL_ASSOC_TBL WHERE USR_ID IN (SELECT USR_ID FROM SEC_USR_TBL WHERE USR_NAME IN ('Bob','Allison', 'SyncUser', 'Administrator'));
DELETE FROM SEC_USR_TBL WHERE USR_NAME IN ('Bob','Allison', 'SyncUser', 'Administrator');
DELETE FROM SEC_APP_POL_ASSOC_TBL WHERE APP_ID IN (SELECT APP_ID FROM SEC_APP_TBL WHERE APP_PUB_ID = 'fiddler');
DELETE FROM SEC_APP_TBL WHERE APP_PUB_ID = 'fiddler';

INSERT INTO SEC_USR_TBL (USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES ('Administrator', UUID_GENERATE_V4(), '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'administrator@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO ent_tbl (ent_id, cls_cd_id, dtr_cd_id) 
	VALUES ('b55f0836-40e6-4ee2-9522-27e3f8bfe532', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'f29f08de-78a7-4a5e-aeaf-7b545ba19a09');
	
INSERT INTO ent_vrsn_tbl(ent_vrsn_id, ent_id, sts_cd_id, CRT_PROV_ID) 
	VALUES ('abfaffc1-5021-40fd-a8e6-9b290f34ead7', 'b55f0836-40e6-4ee2-9522-27e3f8bfe532', 'c8064cbd-fa06-4530-b430-1a52f1530c27', 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO psn_tbl (ent_vrsn_id) VALUES ('abfaffc1-5021-40fd-a8e6-9b290f34ead7');
INSERT INTO usr_ent_tbl (ent_vrsn_id, sec_usr_id) SELECT 'abfaffc1-5021-40fd-a8e6-9b290f34ead7', usr_id FROM sec_usr_tbl WHERE usr_name = 'Administrator';

INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Administrator' AND SEC_ROL_TBL.ROL_Name IN ('ADMINISTRATORS');

INSERT INTO SEC_USR_TBL (USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES ('Bob', UUID_GENERATE_V4(), '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'bob@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Bob' AND SEC_ROL_TBL.ROL_Name IN ('USERS');

INSERT INTO SEC_USR_TBL (USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES ('Allison', UUID_GENERATE_V4(), '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'allison@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Allison' AND SEC_ROL_TBL.ROL_Name IN ('CLINICAL_STAFF');

INSERT INTO SEC_APP_TBL (APP_PUB_ID, APP_SCRT, CRT_PROV_ID)
	VALUES ('fiddler','0180cad1928b9b9887a60a123920a793e7aa7cd339577876f0c233fa2b9fb7d6', 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_APP_POL_ASSOC_TBL(APP_ID, POL_ID, POL_ACT)
	SELECT APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'fiddler';

INSERT INTO SEC_APP_TBL (APP_PUB_ID, APP_SCRT, CRT_PROV_ID)
	VALUES ('org.santedb.disconnected_client', ('ec1e5ef79b95cc1e8a5dec7492b9eb7e2b413ad7a45c5637d16c11bb68fcd53c'), 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_APP_POL_ASSOC_TBL(APP_ID, POL_ID, POL_ACT)
	SELECT APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client';

INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES ('4C5A581C-A6EE-4267-9231-B0D3D50CC08B', 'org.santedb.debug', 'cba830db9a6f5a4b638ff95ef70e98aa82d414ac35b351389024ecb6be40ebf0', CURRENT_TIMESTAMP, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');


INSERT INTO SEC_APP_POL_ASSOC_TBL(APP_ID, POL_ID, POL_ACT)
	SELECT APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.debug';
		
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES ('064C3DBD-8F88-4A5D-A1FA-3C3A542B5E98', 'org.santedb.administration', '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', CURRENT_TIMESTAMP, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_APP_POL_ASSOC_TBL(APP_ID, POL_ID, POL_ACT)
	SELECT APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.administration';

	
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES ('B7ECA9F3-805E-4BE9-A5C7-30E6E495939B', 'org.santedb.disconnected_client', '015fe16693e1117c6c235d91dd535302d65e9259720416d606ab1a2b27a37ba3', CURRENT_TIMESTAMP, 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

INSERT INTO SEC_APP_POL_ASSOC_TBL(APP_ID, POL_ID, POL_ACT)
	SELECT APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client';

	