/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211103-01" name="Update:20211103-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds confidence codes for relationships</summary>
 *	<isInstalled>select ck_patch('20211103-01')</isInstalled>
 * </feature>
 */

 ALTER TABLE SEC_SES_TBL ALTER COLUMN AUD TYPE VARCHAR(256); 
 
SELECT REG_PATCH('20211103-01');
