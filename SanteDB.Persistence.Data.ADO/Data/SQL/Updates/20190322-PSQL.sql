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

-- Index for performance
CREATE INDEX ACT_VRSN_CRT_UTC_IDX ON ACT_VRSN_TBL(CRT_UTC);
CREATE INDEX ENT_VRSN_CRT_UTC_IDX ON ENT_VRSN_TBL(CRT_UTC);

SELECT REG_PATCH('20190322-01');

COMMIT;