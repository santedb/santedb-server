/** 
 * <feature scope="SanteDB.Persistence.Data" id="0-004" name="Data Initialization" invariantName="FirebirdSQL">
 *	<summary>Install Core Data</summary>
 *	<remarks>Initializes the SanteDB database with default usernames, passwords, and applications</remarks>
 *	<isInstalled>SELECT COUNT(1) = 1 FROM SEC_USR_TBL WHERE USR_NAME = 'Administrator'</isInstalled>
 * </feature>
 */
INSERT INTO SEC_USR_TBL (USR_ID, USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES (char_to_uuid('db67a3c1-c7bc-4e21-8f6c-b1c19a7f2c50'), 'Administrator', '6c351600-9fc3-408b-9d55-428a4d29361b', '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'administrator@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO ent_tbl (ent_id, cls_cd_id, dtr_cd_id) 
	VALUES (char_to_uuid('b55f0836-40e6-4ee2-9522-27e3f8bfe532'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('f29f08de-78a7-4a5e-aeaf-7b545ba19a09'));
--#!	
INSERT INTO ent_vrsn_tbl(ent_vrsn_id, ent_id, sts_cd_id, CRT_PROV_ID) 
	VALUES (char_to_uuid('abfaffc1-5021-40fd-a8e6-9b290f34ead7'), char_to_uuid('b55f0836-40e6-4ee2-9522-27e3f8bfe532'), char_to_uuid('c8064cbd-fa06-4530-b430-1a52f1530c27'), char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO psn_tbl (ent_vrsn_id) VALUES (char_to_uuid('abfaffc1-5021-40fd-a8e6-9b290f34ead7'));
--#!
INSERT INTO usr_ent_tbl (ent_vrsn_id, sec_usr_id) SELECT char_to_uuid('abfaffc1-5021-40fd-a8e6-9b290f34ead7'), usr_id FROM sec_usr_tbl WHERE usr_name = 'Administrator';
--#!
INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Administrator' AND SEC_ROL_TBL.ROL_Name IN ('ADMINISTRATORS');
--#!
INSERT INTO SEC_USR_TBL (USR_ID, USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES (char_to_uuid('28e0b42d-2be3-4139-803b-67ac68756275'), 'Bob', 'dc50d0b3-bc02-45b6-8b87-0b6f3c18acd6', '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'bob@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Bob' AND SEC_ROL_TBL.ROL_Name IN ('USERS');
--#!
INSERT INTO SEC_USR_TBL (USR_ID, USR_NAME, SEC_STMP, PASSWD, EMAIL, PHN_NUM, EMAIL_CNF, PHN_CNF, CRT_PROV_ID)
	VALUES (char_to_uuid('0bfbc6bc-ab38-45a6-8d8b-201776fe4e53'), 'Allison', 'a7ec2ec7-ecde-4ea6-ac24-bf6d032cd90c', '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', 'allison@marc-hi.ca', 'tel:+19055751212;ext=4085', TRUE, TRUE, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_USR_ROL_ASSOC_TBL (USR_ID, ROL_ID)
	SELECT USR_ID, ROL_ID FROM SEC_USR_TBL, SEC_ROL_TBL 
	WHERE SEC_USR_TBL.USR_NAME = 'Allison' AND SEC_ROL_TBL.ROL_Name IN ('CLINICAL_STAFF');
--#!
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_PROV_ID)
	VALUES (char_to_uuid('cb92b9ef-b220-461f-a060-423b05eb7421'), 'fiddler','0180cad1928b9b9887a60a123920a793e7aa7cd339577876f0c233fa2b9fb7d6', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'fiddler';
--#!
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_PROV_ID)
	VALUES (char_to_uuid('6cd81e9f-cc50-4448-b938-e884d591a426'), 'org.santedb.disconnected_client', ('015fe16693e1117c6c235d91dd535302d65e9259720416d606ab1a2b27a37ba3'), char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client';
--#!
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES (char_to_uuid('4C5A581C-A6EE-4267-9231-B0D3D50CC08B'), 'org.santedb.debug', 'cba830db9a6f5a4b638ff95ef70e98aa82d414ac35b351389024ecb6be40ebf0', CURRENT_TIMESTAMP, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!

INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.debug';
--#!	
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES (char_to_uuid('064C3DBD-8F88-4A5D-A1FA-3C3A542B5E98'), 'org.santedb.administration', '59ff5973691ff75f8baa45f1e38fae24875f77ef00987ed22b02df075fb144f9', CURRENT_TIMESTAMP, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.administration';
--#!
	
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES (char_to_uuid('B7ECA9F3-805E-4BE9-A5C7-30E6E495939B'), 'org.santedb.disconnected_client.win32', 'd4f8cf183812156e561d390902c092fa4d1b08001ff875a4bd349ed56e1f31d4', CURRENT_TIMESTAMP, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client.win32';
--#!
	
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_UTC, CRT_PROV_ID) 
		VALUES (char_to_uuid('FEECA9F3-805E-4BE9-A5C7-30E6E495939B'), 'org.santedb.disconnected_client.gateway', '015fe16693e1117c6c235d91dd535302d65e9259720416d606ab1a2b27a37ba3', CURRENT_TIMESTAMP, char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client.gateway';
--#!
INSERT INTO SEC_APP_TBL (APP_ID, APP_PUB_ID, APP_SCRT, CRT_PROV_ID)
	VALUES (char_to_uuid('a0fdceb2-a2d3-11ea-ae5e-00155d4f0905'), 'org.santedb.disconnected_client.android', ('ec1e5ef79b95cc1e8a5dec7492b9eb7e2b413ad7a45c5637d16c11bb68fcd53c'), char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_APP_POL_ASSOC_TBL(SEC_POL_INST_ID, APP_ID, POL_ID, POL_ACT)
	SELECT GEN_UUID(), APP_ID, POL_ID, 2 FROM
		SEC_APP_TBL, SEC_POL_TBL
	WHERE
		SEC_APP_TBL.APP_PUB_ID = 'org.santedb.disconnected_client.android';
--#!