/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211110-01" name="Update:20211110-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Migrate the old phon value tables out of SanteDB</summary>
 *	<isInstalled>select ck_patch('20211110-01')</isInstalled>
 * </feature>
 */

CREATE EXTENSION IF NOT EXISTS pg_trgm ;
 alter table ent_name_cmp_tbl add column val VARCHAR(256);
alter table ent_addr_cmp_tbl add column val VARCHAR(256);


update ent_name_cmp_tbl set val = phon_val_tbl.val from phon_val_tbl where phon_val_tbl.val_seq_id  = ent_name_cmp_tbl.val_seq_id ;
update ent_addr_cmp_tbl set val = ent_addr_cmp_val_tbl.val from ent_addr_cmp_val_tbl where ent_addr_cmp_val_tbl.val_seq_id  = ent_addr_cmp_tbl.val_seq_id ;

alter table ent_name_cmp_tbl drop column val_seq_id;
alter table ent_addr_cmp_tbl drop column val_seq_id;

alter table ent_name_cmp_tbl alter column val set not null;
alter table ent_addr_cmp_tbl alter column val set not null;


drop table phon_val_tbl;
drop table ent_addr_cmp_val_tbl ;
alter table ent_addr_cmp_tbl add seq_id bigint not null default nextval('ent_addr_cmp_seq');
alter table ent_name_cmp_tbl add seq_id bigint not null default nextval('ent_name_cmp_seq')

CREATE INDEX ENT_NAME_CMP_VAL_IDX USING GIST ON ENT_NAME_CMP_TBL(VAL); --#
CREATE INDEX ENT_ADDR_CMP_VAL_IDX USING GIST ON ENT_ADDR_CMP_TBL(VAL); --#

SELECT REG_PATCH('20211110-01');
