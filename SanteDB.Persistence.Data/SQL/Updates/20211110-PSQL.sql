/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211110-01" name="Update:20211110-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Migrate the old phon value tables out of SanteDB - NOTE: This update may take upwards of 1 hour to apply on larger datasets</summary>
 *	<isInstalled>select ck_patch('20211110-01')</isInstalled>
 * </feature>
 */

CREATE EXTENSION IF NOT EXISTS pg_trgm ;--#!
CREATE EXTENSION IF NOT EXISTS fuzzystrmatch;--#!
 alter table ent_name_cmp_tbl add column if not exists val VARCHAR(256);--#!
alter table ent_addr_cmp_tbl add column if not exists val VARCHAR(256);--#!

-- INFO: Migrating Names 
update ent_name_cmp_tbl set val = phon_val_tbl.val from phon_val_tbl where phon_val_tbl.val_seq_id  = ent_name_cmp_tbl.val_seq_id and ent_name_cmp_tbl.val is null;--#!
-- INFO: Migrating Addresses
update ent_addr_cmp_tbl set val = ent_addr_cmp_val_tbl.val  from ent_addr_cmp_val_tbl where ent_addr_cmp_val_tbl.val_seq_id  = ent_addr_cmp_tbl.val_seq_id and ent_addr_cmp_tbl.val is null;--#!

-- INFO: Dropping redundant columns
alter table ent_name_cmp_tbl drop column if exists val_seq_id;
alter table ent_addr_cmp_tbl drop column if exists val_seq_id;

alter table ent_name_cmp_tbl alter column val set not null;
alter table ent_addr_cmp_tbl alter column val set not null;


drop table if exists phon_val_tbl;
drop table if exists ent_addr_cmp_val_tbl ;
alter table ent_name_cmp_tbl rename cmp_seq to seq_id;
alter sequence ent_addr_cmp_val_seq rename to ent_addr_cmp_seq;
alter table ent_addr_cmp_tbl add seq_id bigint not null default nextval('ent_addr_cmp_seq');

-- INFO: Indexing Columns
CREATE INDEX ENT_NAME_CMP_VAL_IDX ON ENT_NAME_CMP_TBL USING GIN (VAL gin_trgm_ops); --#
CREATE INDEX ENT_ADDR_CMP_VAL_IDX ON ENT_ADDR_CMP_TBL USING GIN (VAL gin_trgm_ops); --#
CREATE INDEX ENT_NAME_CMP_SDX_IDX ON ENT_NAME_CMP_TBL(SOUNDEX(VAL)); --#!
DROP TABLE IF EXISTS ENT_ADDR_CMP_VAL_TBL;--#!
DROP TABLE IF EXISTS PHON_VAL_TBL;--#!
SELECT REG_PATCH('20211110-01');
