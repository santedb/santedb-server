/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211110-01" name="Update:20211110-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Migrate the old phon value tables out of SanteDB - NOTE: This update may take upwards of 1 hour to apply on larger datasets</summary>
 *	<isInstalled>select ck_patch('20211110-01')</isInstalled>
 * </feature>
 */

CREATE EXTENSION IF NOT EXISTS pg_trgm ;--#!
CREATE EXTENSION IF NOT EXISTS fuzzystrmatch;--#!

-- INFO: Migrating Names 
SELECT ENT_NAME_CMP_TBL.*, PHON_VAL_TBL.VAL  INTO ENT_NAME_CMP_UPD_TBL FROM ENT_NAME_CMP_TBL INNER JOIN PHON_VAL_TBL USING (VAL_SEQ_ID);
DROP TABLE PHON_VAL_TBL CASCADE;
DROP TABLE ENT_NAME_CMP_TBL;
ALTER TABLE ENT_NAME_CMP_UPD_TBL RENAME TO ENT_NAME_CMP_TBL;
--#!
-- INFO:	 Re-applying keys and indexes...
ALTER TABLE ent_name_cmp_tbl ADD CONSTRAINT pk_ent_name_cmp_tbl PRIMARY KEY (cmp_id);
CREATE INDEX ent_name_cmp_name_id_idx ON ent_name_cmp_tbl USING btree (name_id);
ALTER TABLE ent_name_cmp_tbl ADD CONSTRAINT fk_ent_name_cmp_name_id FOREIGN KEY (name_id) REFERENCES ent_name_tbl(name_id);
ALTER TABLE ent_name_cmp_tbl ADD CONSTRAINT fk_ent_name_cmp_typ_cd_id FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id);
drop table if exists phon_val_tbl;
drop sequence if exists phon_val_seq;
alter table ent_name_cmp_tbl rename cmp_seq to seq_id;
alter table ent_name_cmp_tbl ALTER seq_id SET not NULL;
alter table ent_name_cmp_tbl ALTER seq_id SET default nextval('name_val_seq');
alter table ent_name_cmp_tbl alter cmp_id SET not NULL;
alter table ent_name_cmp_tbl alter cmp_id SET default uuid_generate_v1();
ALTER TABLE ENT_NAME_CMP_TBL DROP COLUMN VAL_SEQ_ID;
alter table ent_name_cmp_tbl alter column val set not null;

--#!
-- INFO: Migrating Addresses

SELECT ENT_ADDR_CMP_TBL.*, ENT_ADDR_CMP_VAL_TBL.VAL  INTO ENT_ADDR_CMP_UPD_TBL FROM ENT_ADDR_CMP_TBL INNER JOIN ENT_ADDR_CMP_VAL_TBL USING (VAL_SEQ_ID);
DROP TABLE ENT_ADDR_CMP_VAL_TBL CASCADE;
DROP TABLE ENT_ADDR_CMP_TBL;
ALTER TABLE ENT_ADDR_CMP_UPD_TBL RENAME TO ENT_ADDR_CMP_TBL;
--#!
-- INFO:	Re-Applying keys and indexes....
CREATE INDEX ent_addr_cmp_addr_id_idx ON ent_addr_cmp_tbl USING btree (addr_id);
ALTER TABLE ent_addr_cmp_tbl ADD CONSTRAINT pk_ent_addr_cmp_tbl PRIMARY KEY (cmp_id);
ALTER TABLE ent_addr_cmp_tbl ADD CONSTRAINT fk_ent_addr_cmp_name_id FOREIGN KEY (addr_id) REFERENCES ent_addr_tbl(addr_id);
ALTER TABLE ent_addr_cmp_tbl ADD CONSTRAINT fk_ent_addr_cmp_typ_cd_id FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id);
drop table if exists ent_addr_cmp_val_tbl CASCADE;
alter table ent_addr_cmp_tbl drop column if exists val_seq_id;
alter table ent_addr_cmp_tbl alter column val set not null;
alter sequence ent_addr_cmp_val_seq rename to ent_addr_cmp_seq;
alter table ent_addr_cmp_tbl add seq_id bigint not null default nextval('ent_addr_cmp_seq');
--#!
-- INFO: Adding GIN and Fuzzy Indexes on Columns
CREATE INDEX ENT_NAME_CMP_VAL_IDX ON ENT_NAME_CMP_TBL USING GIN (VAL gin_trgm_ops); --#
CREATE INDEX ENT_ADDR_CMP_VAL_IDX ON ENT_ADDR_CMP_TBL USING GIN (VAL gin_trgm_ops); --#
CREATE INDEX ENT_NAME_CMP_SDX_IDX ON ENT_NAME_CMP_TBL(SOUNDEX(VAL)); --#!
DROP TABLE IF EXISTS ENT_ADDR_CMP_VAL_TBL;--#!
DROP TABLE IF EXISTS PHON_VAL_TBL;--#!
SELECT REG_PATCH('20211110-01');
