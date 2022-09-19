/** 
 * <feature scope="SanteDB.Persistence.Data" id="20190322-01" name="Update:20190322-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Update: Allow same entitie classes to be children of each other</summary>
 *	<remarks>Updates the relationships</remarks>
 *	<isInstalled>select ck_patch('20190322-01')</isInstalled>
 * </feature>
 */

 BEGIN TRANSACTION;

 ALTER TABLE ENT_REL_VRFY_CDTBL ALTER err_desc TYPE VARCHAR(256);
-- X CAN BE A CHILD OF X
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) SELECT CD_ID, '739457d0-835a-4a9c-811c-42b5e92ed1ca', CD_ID, 'CHILD RECORD' FROM CD_SET_MEM_ASSOC_TBL WHERE SET_ID = '4e6da567-0094-4f23-8555-11da499593af';

-- PSN or PAT CAN BE EMPLOYEE OF ORG
INSERT INTO ENT_REL_VRFY_CDTBL (trg_cls_cd_id, rel_typ_cd_id, src_cls_cd_id , err_desc) VALUES ('7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'b43c9513-1c1c-4ed0-92db-55a904c122e6', 'bacd9c6f-3fa9-481e-9636-37457962804d', 'Person=[Employee]=>Organization'); 
INSERT INTO ENT_REL_VRFY_CDTBL (trg_cls_cd_id, rel_typ_cd_id, src_cls_cd_id, err_desc) VALUES ('7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'b43c9513-1c1c-4ed0-92db-55a904c122e6', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'Patient=[Employee]=>Organization'); 

-- MISSING POLICY IDENTIFIERS
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (('baa227aa-224d-4859-81b3-c1eb2750067f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.11', 'Access Audit Log', ('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (('baa227aa-224d-4859-81b3-c1eb2750068f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.12', 'Administer Applets', ('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (('baa227aa-224d-4859-81b3-c1eb2750069f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.13', 'Assign Policy', ('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (('baa227aa-224d-4859-81b3-c1eb275006af'), '1.3.6.1.4.1.33349.3.1.5.9.2.2.5', 'Elevate Clinical Data', ('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));

-- Index for performance
CREATE INDEX IF NOT EXISTS ACT_VRSN_CRT_UTC_IDX ON ACT_VRSN_TBL(CRT_UTC);
CREATE INDEX IF NOT EXISTS  ENT_VRSN_CRT_UTC_IDX ON ENT_VRSN_TBL(CRT_UTC);

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX phon_val_val_gin_idx ON phon_val_tbl USING gin (val gin_trgm_ops);

DROP INDEX IF EXISTS act_ptcpt_ent_id_idx;
DROP INDEX IF EXISTS act_ptcpt_rol_cd_idx;
DROP INDEX IF EXISTS act_tag_tag_name_idx;

SELECT REG_PATCH('20190322-01');

COMMIT;