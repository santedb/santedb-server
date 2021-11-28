/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211128-01" name="Update:20211128-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds geotagging to devices</summary>
 *	<isInstalled>select ck_patch('20211128-01')</isInstalled>
 * </feature>
 */
 ALTER TABLE DEV_ENT_TBL ADD GEO_ID UUID;
 alter table dev_ent_tbl add constraint fk_dev_ent_geo_tag foreign key (GEO_ID) references geo_tbl(geo_id);--#!

SELECT REG_PATCH('20211128-01');
