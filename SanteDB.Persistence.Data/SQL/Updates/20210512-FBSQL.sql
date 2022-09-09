/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210512-01" name="Update:20210512-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds classification codes to relationship tables and refactors gender code</summary>
 *	<isInstalled>select ck_patch('20210512-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */
ALTER TABLE ent_rel_tbl ADD conf REAL;--#!
SELECT REG_PATCH('20210512-01') FROM RDB$DATABASE; --#!
