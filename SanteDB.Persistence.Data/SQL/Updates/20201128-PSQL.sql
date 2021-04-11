/** 
 * <feature scope="SanteDB.Persistence.Data" id="20201128-01" name="Update:20201128-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Register </summary>
 *	<remarks>Adds policies which control impersination</remarks>
 *	<isInstalled>select ck_patch('20201128-01')</isInstalled>
 * </feature>
 */
BEGIN TRANSACTION;

INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES ('f45b96fe-646c-4c00-9a58-ea09eee67dad', '1.3.6.1.4.1.33349.3.1.5.9.2.0.4.1', 'Create Local Users', 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES ('f45b96ff-646c-4c00-9a58-ea09eee67dad', '1.3.6.1.4.1.33349.3.1.5.9.2.0.8.1', 'Alter Local Users', 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8');

ALTER TABLE USR_ENT_TBL ALTER COLUMN SEC_USR_ID DROP NOT NULL;
SELECT REG_PATCH('20201128-01');

COMMIT;
