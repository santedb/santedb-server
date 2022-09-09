/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211217-01" name="Update:20211217-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Fix VIP and Occupation status codes</summary>
 *	<isInstalled>select ck_patch('20211217-01') from rdb$database</isInstalled>
 * </feature>
 */

 INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('BFCBB345-86DB-43BA-B47E-E7411276AC7C'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'Organization=>Parent=>Organization');--#!
SELECT REG_PATCH('20211217-01') FROM RDB$DATABASE; --#!
