/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20190322-01" name="Update:20190322-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Update: Allow same entitie classes to be children of each other</summary>
 *	<remarks>Updates the relationships</remarks>
 *	<isInstalled>select ck_patch('20190322-01')</isInstalled>
 * </feature>
 */

 BEGIN TRANSACTION;

-- X CAN BE A CHILD OF X
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) SELECT CD_ID, '739457d0-835a-4a9c-811c-42b5e92ed1ca', CD_ID, 'CHILD RECORD' FROM CD_SET_MEM_ASSOC_TBL WHERE SET_ID = '4e6da567-0094-4f23-8555-11da499593af';

-- PSN or PAT CAN BE EMPLOYEE OF ORG
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES ('7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'b43c9513-1c1c-4ed0-92db-55a904c122e6', 'bacd9c6f-3fa9-481e-9636-37457962804d', 'Organization=[Employee]=>Person'); 
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES ('7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'b43c9513-1c1c-4ed0-92db-55a904c122e6', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'Organization=[Employee]=>Patient'); 
-- Index for performance
CREATE INDEX IF NOT EXISTS ACT_VRSN_CRT_UTC_IDX ON ACT_VRSN_TBL(CRT_UTC);
CREATE INDEX IF NOT EXISTS  ENT_VRSN_CRT_UTC_IDX ON ENT_VRSN_TBL(CRT_UTC);

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX phon_val_val_gin_idx ON phon_val_tbl USING gin (val gin_trgm_ops);

SELECT REG_PATCH('20190322-01');

COMMIT;