/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210409-01" name="Update:20210409-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Refactored persistence layer patch </summary>
 *	<isInstalled>select ck_patch('20210409-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

-- PUBLIC KEY FOR THE APPLICATION (USED FOR SIGNING DATA WITH THE APP)
ALTER TABLE SEC_APP_TBL ADD SGN_KEY BYTEA;

SELECT REG_PATCH('20210409-01');
COMMIT;