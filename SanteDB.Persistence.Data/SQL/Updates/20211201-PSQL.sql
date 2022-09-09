/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211201-01" name="Update:20211201-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Change the VIP status concept set</summary>
 *	<isInstalled>select ck_patch('20211201-01')</isInstalled>
 * </feature>
 */

ALTER TABLE pat_tbl DROP CONSTRAINT ck_vip_sts_cd;
ALTER TABLE pat_tbl ADD CONSTRAINT ck_vip_sts_cd CHECK (vip_sts_cd_id IS NULL OR IS_CD_SET_MEM(vip_sts_cd_id, 'VeryImportantPersonStatus') OR IS_CD_SET_MEM(vip_sts_cd_id, 'NullReason'));
SELECT REG_PATCH('20211201-01');
