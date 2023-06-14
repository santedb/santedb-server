/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20230509-02" name="Update:20230509-02" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds external tagging / key tracking to the database</summary>
 *	<isInstalled>select ck_patch('20230509-02')</isInstalled>
 * </feature>
 */
 -- OPTIONAL
 ALTER TABLE ENT_ADDR_TBL ADD EXT_ID VARCHAR(256);
 ALTER TABLE ENT_NAME_TBL ADD EXT_ID VARCHAR(256);
 ALTER TABLE ENT_TEL_TBL ADD EXT_ID VARCHAR(256);
 ALTER TABLE ENT_REL_TBL ADD EXT_ID VARCHAR(256);
SELECT REG_PATCH('20230509-02'); 
