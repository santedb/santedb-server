/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200812-01" name="Update:20200812-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="fbsql">
 *	<summary>Update: Removes phonetic value columns (built in db functions should be used)</summary>
 *	<remarks>This removes phonetic value columns</remarks>
 *	<isInstalled>select ck_patch('20200812-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

DROP INDEX CD_NAME_PHON_CS_IDX;
DROP INDEX REF_TERM_NAME_PHON_CS_IDX;
DROP INDEX PHON_VAL_PHON_CS_IDX;
ALTER TABLE cd_name_tbl DROP phon_cs;
ALTER TABLE cd_name_tbl DROP phon_alg_id;
ALTER TABLE ref_term_name_tbl DROP phon_cs;
ALTER TABLE ref_term_name_tbl DROP phon_alg_id;
ALTER TABLE phon_val_tbl DROP phon_cs;
ALTER TABLE phon_val_tbl DROP alg_id;

SELECT REG_PATCH('20200812-01');
COMMIT;