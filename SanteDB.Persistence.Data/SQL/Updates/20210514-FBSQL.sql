/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210514-01" name="Update:20210514-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Fixes session abandon constraint</summary>
 *	<isInstalled>select ck_patch('20210514-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */


UPDATE sec_rol_tbl SET rol_name = 'APPLICATIONS' WHERE rol_name = 'SYNCHRONIZERS';--#!
ALTER TABLE sec_ses_tbl DROP CONSTRAINT CK_SEC_SES_RFRSH_EXP; --#!
ALTER TABLE sec_ses_tbl ADD CONSTRAINT CK_SEC_SES_RFRSH_EXP CHECK (RFRSH_EXP_UTC >= EXP_UTC);--#!

SELECT REG_PATCH('20210514-01') FROM RDB$DATABASE; --#!
