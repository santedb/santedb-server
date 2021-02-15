/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20210214-01" name="Update:20210214-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Performance enhancements to database based on extreme large test environment (> 1m patients)</summary>
 *	<isInstalled>select ck_patch('20210214-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

CLUSTER ent_tbl USING ent_cls_cd_idx;

-- INDEX ON ID VAL
create index ent_id_val_idx on ent_id_tbl (id_val);

create index phon_val_val_btr_idx on phon_val_tbl(val);
create index ent_id_val_gin_idx on ent_id_tbl USING gin (id_val gin_trgm_ops);
SELECT REG_PATCH('20210214-01');
COMMIT;