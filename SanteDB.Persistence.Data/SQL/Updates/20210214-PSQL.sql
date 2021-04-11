/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210214-01" name="Update:20210214-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Performance enhancements to database based on extreme large test environment (> 1m patients)</summary>
 *	<isInstalled>select ck_patch('20210214-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

-- INDEX ON ID VAL
create index if not exists ent_id_val_idx on ent_id_tbl (id_val);

create index if not exists phon_val_val_btr_idx on phon_val_tbl(val);
create index  if not exists ent_id_val_gin_idx on ent_id_tbl USING gin (id_val gin_trgm_ops);
insert into cd_set_mem_assoc_tbl (set_id, cd_id) values ('1dabe3e2-44b8-4c45-9102-25ea147e5710','F3132FC0-AADD-40B7-B875-961C40695389') on conflict do nothing;

SELECT REG_PATCH('20210214-01');
COMMIT;