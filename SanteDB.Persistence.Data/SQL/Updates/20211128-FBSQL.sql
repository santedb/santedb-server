﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211128-01" name="Update:20211128-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Add geo-taggging to devices</summary>
 *	<isInstalled>select ck_patch('20211128-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */
 alter table dev_ent_tbl add geo_id uuid; --#!
 alter table dev_ent_tbl add constraint fk_dev_ent_geo_tag foreign key (geo_id) references geo_tbl(geo_id);--#!
SELECT REG_PATCH('20211128-01') FROM RDB$DATABASE; --#!
