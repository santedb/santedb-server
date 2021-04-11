/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210217-01" name="Update:20210217-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Performance enhancements to database based on extreme large test environment (> 1m patients)</summary>
 *	<isInstalled>select ck_patch('20210217-01') FROM RDB$DATABASE</isInstalled>
 * </feature>
 */

-- INDEX ON ID VAL
create index ent_id_val_idx on ent_id_tbl (id_val);--#!

create index phon_val_val_idx on phon_val_tbl(val);--#!
update or insert into cd_set_mem_assoc_tbl (set_id, cd_id) values (char_to_uuid('1dabe3e2-44b8-4c45-9102-25ea147e5710'),char_to_uuid('F3132FC0-AADD-40B7-B875-961C40695389')) matching (set_id, cd_id);--#!

SELECT REG_PATCH('20210217-01') FROM RDB$DATABASE;--#!
