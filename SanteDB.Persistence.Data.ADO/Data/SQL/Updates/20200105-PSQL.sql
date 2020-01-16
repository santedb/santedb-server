/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200105-01" name="Update:20200105-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add relationship for devices</summary>
 *	<remarks>This allows devices to be tracked to facilities and users</remarks>
 *	<isInstalled>select ck_patch('20200105-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

-- ASSIGNED ENTITY
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES ('1373ff04-a6ef-420a-b1d0-4a07465fe8e8', '455f1772-f580-47e8-86bd-b5ce25d351f9', 'FF34DFA7-C6D3-4F8B-BC9F-14BCDC13BA6C', 'Device=[DedicatedServiceDeliveryLocation]=>ServiceDeliveryLocation'); 
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES ('1373ff04-a6ef-420a-b1d0-4a07465fe8e8', '77b7a04b-c065-4faf-8ec0-2cdad4ae372b', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'Device=[AssignedEntity]=>Person'); 
ALTER TABLE ASGN_AUT_TBL ADD POL_ID UUID;
ALTER TABLE ASGN_AUT_TBL ADD UPD_UTC TIMESTAMPTZ;
ALTER TABLE ASGN_AUT_TBL ADD UPD_PROV_ID UUID;
ALTER TABLE ASGN_AUT_TBL ADD CONSTRAINT CK_ASGN_AUT_UPD CHECK (UPD_UTC IS NULL OR UPD_UTC IS NOT NULL AND UPD_PROV_ID IS NOT NULL);
ALTER TABLE SEC_USR_CLM_TBL ADD EXP_UTC TIMESTAMPTZ;
SELECT REG_PATCH('20200105-01');
COMMIT;