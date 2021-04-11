/** 
 * <feature scope="SanteDB.Persistence.Data" id="20200912-01" name="Update:20200912-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Allow Impersonation</summary>
 *	<remarks>Adds policies which control impersination</remarks>
 *	<isInstalled>select ck_patch('20200912-01') from rdb$database</isInstalled>
 * </feature>
 */

INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (char_to_uuid('f45b96ab-646c-4c00-9a58-ea09eee67dad'), '1.3.6.1.4.1.33349.3.1.5.9.2.1.0.2', 'Allow Impersonation of Application', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));--#!
INSERT INTO SEC_ROL_POL_ASSOC_TBL (POL_ID, ROL_ID, POL_ACT)
	SELECT char_to_uuid('f45b96ab-646c-4c00-9a58-ea09eee67dad'), ROL_ID, 1
	FROM SEC_ROL_TBL
	WHERE ROL_NAME LIKE 'Administrators';--#!

ALTER TABLE ENT_ID_TBL ADD iss_dt DATE;--#!
ALTER TABLE ENT_ID_TBL ADD exp_dt DATE;--#!
ALTER TABLE ENT_ID_TBL ADD chk_dgt VARCHAR(10);--#!

ALTER TABLE ACT_ID_TBL ADD iss_dt DATE;--#!
ALTER TABLE ACT_ID_TBL ADD exp_dt DATE;--#!
ALTER TABLE ACT_ID_TBL ADD chk_dgt VARCHAR(10);--#!
ALTER TABLE ASGN_AUT_TBL ADD val_cls VARCHAR(256);--#!
SELECT REG_PATCH('20200912-01') FROM RDB$DATABASE;