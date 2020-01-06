/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200105-01" name="Update:20200105-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="fbsql">
 *	<summary>Update: Add relationship for devices</summary>
 *	<remarks>This allows devices to be tracked to facilities and users</remarks>
 *	<check>select ck_patch('20200105-01')</check>
 * </feature>
 */

BEGIN TRANSACTION ;
--#!
-- ASSIGNED ENTITY
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), char_to_uuid('455f1772-f580-47e8-86bd-b5ce25d351f9'), char_to_uuid('FF34DFA7-C6D3-4F8B-BC9F-14BCDC13BA6C'), 'Device=[DedicatedServiceDeliveryLocation]=>ServiceDeliveryLocation'); 
--#!
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), char_to_uuid('77b7a04b-c065-4faf-8ec0-2cdad4ae372b'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), 'Device=[AssignedEntity]=>Person'); 
--#!


SELECT REG_PATCH('20200105-01') FROM RDB$DATABASE;
--#!
COMMIT;