/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20201109-01" name="Update:20201109-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Allow Impersonation</summary>
 *	<remarks>Adds policies which control impersination</remarks>
 *	<isInstalled>select ck_patch('20201109-01')</isInstalled>
 * </feature>
 */
BEGIN TRANSACTION;

DROP INDEX public.cd_ref_term_cs_mnemonic_uq_idx;

CREATE UNIQUE INDEX cd_ref_term_cs_mnemonic_uq_idx
  ON public.ref_term_tbl
  (cs_id, mnemonic)
  WHERE (obslt_utc IS NULL);

SELECT REG_PATCH('20201109-01');

COMMIT;
