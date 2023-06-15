/**
* <feature scope="SanteDB.Persistence.Data.ADO" id="20220908-01" name="Update:20220908-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
*    <summary>Update: Adds Read Metadata Policy to Users role </summary>
*    <isInstalled>select ck_patch('20220908-01')</isInstalled>
* </feature>
*/
BEGIN TRANSACTION ;

INSERT INTO SEC_ROL_POL_ASSOC_TBL (POL_ID, ROL_ID, POL_ACT) VALUES ('fea891aa-224d-4859-81b3-c1eb2750067e', 'f4e58ae8-8bbd-4635-a6d4-8a195b143436', 2) ON CONFLICT DO NOTHING; -- GRANT Read Metadata


SELECT REG_PATCH('20220908-01');
COMMIT;