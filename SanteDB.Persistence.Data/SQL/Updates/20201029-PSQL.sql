/** 
 * <feature scope="SanteDB.Persistence.Data" id="20201029-01" name="Update:20201029-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Place of Death Associations</summary>
 *	<remarks>Adds allowed relationship types between places of death</remarks>
 *	<isInstalled>select ck_patch('20201029-01')</isInstalled>
 * </feature>
 */
BEGIN TRANSACTION;

INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) 
SELECT '9bbe0cfe-faab-4dc9-a28f-c001e3e95e6e', src_cls_cd_id, trg_cls_cd_id, REPLACE (err_desc, '[Birthplace]','[PlaceOfDeath]') FROM ENT_REL_VRFY_CDTBL WHERE rel_typ_Cd_id = 'f3ef7e48-d8b7-4030-b431-aff7e0e1cb76'
  ON CONFLICT DO NOTHING;

SELECT REG_PATCH('20201029-01');
COMMIT;