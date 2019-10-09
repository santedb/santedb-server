/** 
 * <feature scope="SanteDB.Persistence.Data.ADO.Performance" id="ZZ-PARTITION-PGSQL10" name="Performance: Partition PostgreSQL" applyRange="1.0.0.3-1.0.0.0"  invariantName="npgsql">
 *	<summary>Performance: Enhances performance of large SanteDB databases by partitioning certain tables</summary>
 *	<remarks>This update will partition the tables ACT_PTCPT, ACT, and ENT_REL along common lines</remarks>
 *	<isInstalled>select ck_patch('ZZ-PARTITION-PGSQL10')</isInstalled>
 *	<canInstall>select version() ilike '%postgresql 10.%' </canInstall>
 * </feature>
 */
 -- PARTITION THE TABLE ACT_PTCPT_TBL BY TYPE
CREATE TABLE act_ptcpt_part_tbl
(
  act_ptcpt_id uuid NOT NULL DEFAULT uuid_generate_v4(),
  ent_id uuid NOT NULL,
  act_id uuid NOT NULL,
  efft_vrsn_seq_id numeric(20,0) NOT NULL,
  obslt_vrsn_seq_id numeric(20,0),
  qty integer DEFAULT 1,
  rol_cd_id uuid NOT NULL,
  ptcpt_seq_id numeric(20,0) NOT NULL DEFAULT nextval('act_ptcpt_seq'::regclass),
  
  CONSTRAINT ck_act_ptcpt_rol_cd CHECK (ck_is_cd_set_mem(rol_cd_id, 'ActParticipationType'::character varying, true))
) PARTITION BY LIST (rol_cd_id);

-- PARTITIONS FOR THE ACT_PARTICIPATION TABLE
CREATE TABLE act_ptcpt_part_loc_tbl PARTITION OF act_ptcpt_part_tbl FOR VALUES IN ('61848557-d78d-40e5-954f-0b9c97307a04','02bb7934-76b5-4cc5-bd42-58570f15eb4d','ac05185b-5a80-47a8-b924-060deb6d0eb2');
CREATE TABLE act_ptcpt_part_rct_tbl PARTITION OF act_ptcpt_part_tbl FOR VALUES IN ('3f92dbee-a65e-434f-98ce-841feeb02e3f');
CREATE TABLE act_ptcpt_part_cons_tbl PARTITION OF act_ptcpt_part_tbl FOR VALUES IN ('a5cac7f7-e3b7-4dd8-872c-db0e7fcc2d84','99e77288-cb09-4050-a8cf-385513f32f0a');
CREATE TABLE act_ptcpt_part_auth_tbl PARTITION OF act_ptcpt_part_tbl FOR VALUES IN ('f0cb3faf-435d-4704-9217-b884f757bc14','a2594e6e-e8fe-4c68-82a5-d3a46dbec87d','fa5e70a4-a46e-4665-8a20-94d4d7b86fc8');
CREATE TABLE act_ptcpt_part_oth_tbl PARTITION OF act_ptcpt_part_tbl FOR VALUES IN ('9790b291-b8a3-4c85-a240-c2c38885ad5d','5b0fac74-5ac6-44e6-99a4-6813c0e2f4a9', '727b3624-ea62-46bb-a68b-b9e49e302eca');

--#!
-- COPY EXISTING ACT PARTICIPATIONS TO THE NEW TABLE (CAN TAKE ABOUT 1 hr)
INSERT INTO act_ptcpt_part_tbl SELECT * FROM act_ptcpt_tbl;
--#!

-- CREATE NECESSARY INDEXES ON PARTITION TABLES
CREATE INDEX act_ptcpt_part_loc_rol_cd_idx ON act_ptcpt_part_loc_tbl (rol_cd_id);
CREATE INDEX act_ptcpt_part_rct_rol_cd_idx ON act_ptcpt_part_rct_tbl (rol_cd_id);
CREATE INDEX act_ptcpt_part_cons_rol_cd_idx ON act_ptcpt_part_cons_tbl (rol_cd_id);
CREATE INDEX act_ptcpt_part_auth_rol_cd_idx ON act_ptcpt_part_auth_tbl (rol_cd_id);
CREATE INDEX act_ptcpt_part_oth_rol_cd_idx ON act_ptcpt_part_oth_tbl (rol_cd_id);
CREATE INDEX act_ptcpt_part_loc_ent_idx ON act_ptcpt_part_loc_tbl (ent_id);
CREATE INDEX act_ptcpt_part_rct_ent_idx ON act_ptcpt_part_rct_tbl (ent_id);
CREATE INDEX act_ptcpt_part_cons_ent_idx ON act_ptcpt_part_cons_tbl (ent_id);
CREATE INDEX act_ptcpt_part_auth_ent_idx ON act_ptcpt_part_auth_tbl (ent_id);
CREATE INDEX act_ptcpt_part_oth_ent_idx ON act_ptcpt_part_oth_tbl (ent_id);
CREATE INDEX act_ptcpt_part_loc_act_idx ON act_ptcpt_part_loc_tbl (act_id);
CREATE INDEX act_ptcpt_part_rct_act_idx ON act_ptcpt_part_rct_tbl (act_id);
CREATE INDEX act_ptcpt_part_cons_act_idx ON act_ptcpt_part_cons_tbl (act_id);
CREATE INDEX act_ptcpt_part_auth_act_idx ON act_ptcpt_part_auth_tbl (act_id);
CREATE INDEX act_ptcpt_part_oth_act_idx ON act_ptcpt_part_oth_tbl (act_id);
--#!

-- ENFORCE UNIQUENESS
CREATE UNIQUE INDEX act_ptcpt_part_loc_unq_enf_sha1 ON act_ptcpt_part_loc_tbl (digest((act_id::text || ent_id::text) || rol_cd_id::text, 'sha1'::text)) where obslt_vrsn_seq_id is null;
CREATE UNIQUE INDEX act_ptcpt_part_rct_unq_enf_sha1 ON act_ptcpt_part_rct_tbl (digest((act_id::text || ent_id::text) || rol_cd_id::text, 'sha1'::text)) where obslt_vrsn_seq_id is null;
CREATE UNIQUE INDEX act_ptcpt_part_auth_unq_enf_sha1 ON act_ptcpt_part_auth_tbl (digest((act_id::text || ent_id::text) || rol_cd_id::text, 'sha1'::text)) where obslt_vrsn_seq_id is null;
CREATE UNIQUE INDEX act_ptcpt_part_cons_unq_enf_sha1 ON act_ptcpt_part_cons_tbl (digest((act_id::text || ent_id::text) || rol_cd_id::text, 'sha1'::text)) where obslt_vrsn_seq_id is null;
CREATE UNIQUE INDEX act_ptcpt_part_oth_unq_enf_sha1 ON act_ptcpt_part_oth_tbl (digest((act_id::text || ent_id::text) || rol_cd_id::text, 'sha1'::text)) where obslt_vrsn_seq_id is null;
--#!

-- ADD FOREIGN KEYS BACK TO TABLE 
ALTER TABLE act_ptcpt_part_loc_tbl ADD CONSTRAINT pk_act_ptcpt_part_loc_tbl PRIMARY KEY (act_ptcpt_id);
ALTER TABLE act_ptcpt_part_rct_tbl ADD CONSTRAINT pk_act_ptcpt_part_rct_tbl PRIMARY KEY (act_ptcpt_id);
ALTER TABLE act_ptcpt_part_oth_tbl ADD CONSTRAINT pk_act_ptcpt_part_oth_tbl PRIMARY KEY (act_ptcpt_id);
ALTER TABLE act_ptcpt_part_cons_tbl ADD CONSTRAINT pk_act_ptcpt_part_cons_tbl PRIMARY KEY (act_ptcpt_id);
ALTER TABLE act_ptcpt_part_auth_tbl ADD CONSTRAINT pk_act_ptcpt_part_auth_tbl PRIMARY KEY (act_ptcpt_id);
ALTER TABLE act_ptcpt_part_loc_tbl ADD CONSTRAINT fk_act_ptcpt_part_loc_act_id FOREIGN KEY (act_id) REFERENCES act_tbl (act_id);
ALTER TABLE act_ptcpt_part_rct_tbl ADD CONSTRAINT fk_act_ptcpt_part_rct_act_id FOREIGN KEY (act_id) REFERENCES act_tbl (act_id);
ALTER TABLE act_ptcpt_part_oth_tbl ADD CONSTRAINT fk_act_ptcpt_part_oth_act_id FOREIGN KEY (act_id) REFERENCES act_tbl (act_id);
ALTER TABLE act_ptcpt_part_cons_tbl ADD CONSTRAINT fk_act_ptcpt_part_cons_act_id FOREIGN KEY (act_id) REFERENCES act_tbl (act_id);
ALTER TABLE act_ptcpt_part_auth_tbl ADD CONSTRAINT fk_act_ptcpt_part_auth_act_id FOREIGN KEY (act_id) REFERENCES act_tbl (act_id);
ALTER TABLE act_ptcpt_part_loc_tbl ADD CONSTRAINT fk_act_ptcpt_part_loc_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl  (ent_id);
ALTER TABLE act_ptcpt_part_rct_tbl ADD CONSTRAINT fk_act_ptcpt_part_rct_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE act_ptcpt_part_oth_tbl ADD CONSTRAINT fk_act_ptcpt_part_oth_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl  (ent_id);
ALTER TABLE act_ptcpt_part_cons_tbl ADD CONSTRAINT fk_act_ptcpt_part_cons_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE act_ptcpt_part_auth_tbl ADD CONSTRAINT fk_act_ptcpt_part_auth_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl  (ent_id);
--#!

-- CREATE A BACKUP OF THE EXISTING PARTICIPANTS TABLE
alter table act_ptcpt_tbl rename to act_ptcpt_tbl_bak;

-- RENAME THE PARTITIONED TABLE TO BE THE ACTUAL TABLE
alter table act_ptcpt_part_tbl rename to act_ptcpt_tbl;
--#!


-- PARTITION THE TABLE ENT_ROL_TBL BY TYPE
CREATE TABLE ent_rel_part_tbl
(
	ent_rel_id uuid NOT NULL DEFAULT uuid_generate_v4(),
	src_ent_id uuid NOT NULL,
	trg_ent_id uuid NOT NULL,
	efft_vrsn_seq_id numeric(20,0) NOT NULL,
	obslt_vrsn_seq_id numeric(20,0),
	rel_typ_cd_id uuid NOT NULL,
	qty integer DEFAULT 1,
	CONSTRAINT ck_ent_rel_rel_type_cd CHECK (ck_is_cd_set_mem(rel_typ_cd_id, 'EntityRelationshipType'::character varying, false))
) PARTITION BY LIST (rel_typ_cd_id);

-- PARTITIONS FOR THE ENT_REL_TBL TABLE
CREATE TABLE ent_rel_part_dsdl_tbl PARTITION OF ent_rel_part_tbl FOR VALUES IN ('455f1772-f580-47e8-86bd-b5ce25d351f9', '41baf7aa-5ffd-4421-831f-42d4ab3de38a', 'ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c');
CREATE TABLE ent_rel_part_fam_tbl PARTITION OF ent_rel_part_tbl FOR VALUES IN ('1ee4e74f-542d-4544-96f6-266a6247f274', '0ff2ab03-6e0a-40d1-8947-04c4937b4cc4','24380d53-ea22-4820-9f06-8671f774f133','739457d0-835a-4a9c-811c-42b5e92ed1ca','1c0f931c-9c49-4a52-8fbf-5217c52ea778','38d66ec7-0cc8-4609-9675-b6ff91ede605','40d18ecc-8ff8-4e03-8e58-97a980f04060','48c59444-fec0-43b8-aa2c-7aedb70733ad','b630ba2c-8a00-46d8-bf64-870d381d8917','fa646df9-7d64-4d1f-ae9a-6261fd5fd6ae','29ff64e5-b564-411a-92c7-6818c02a9e48','bfcbb345-86db-43ba-b47e-e7411276ac7c','cd1e8904-31dc-4374-902d-c91f1de23c46','f172eee7-7f4b-4022-81d0-76393a1200ae','cdd99260-107c-4a4e-acaf-d7c9c7e90fdd');
CREATE TABLE ent_rel_part_own_tbl PARTITION OF ent_rel_part_tbl FOR VALUES IN ('117da15c-0864-4f00-a987-9b9854cba44e');
CREATE TABLE ent_rel_part_stock_tbl PARTITION OF ent_rel_part_tbl FOR VALUES IN ('08fff7d9-bac7-417b-b026-c9bee52f4a37','639b4b8f-afd3-4963-9e79-ef0d3928796a','6780df3b-afbd-44a3-8627-cbb3dc2f02f6');
CREATE TABLE ent_rel_part_inf_tbl PARTITION OF ent_rel_part_tbl FOR VALUES IN ('ac45a740-b0c7-4425-84d8-b3f8a41fef9f', 'd1578637-e1cb-415e-b319-4011da033813', '77b7a04b-c065-4faf-8ec0-2cdad4ae372b');

--#!
INSERT INTO ENT_REL_PART_TBL SELECT * FROM ENT_REL_TBL;
--#!

-- ADD PK
ALTER TABLE ent_rel_part_dsdl_tbl ADD CONSTRAINT pk_ent_rel_part_dsdl_tbl PRIMARY KEY (ent_rel_id);
ALTER TABLE ent_rel_part_fam_tbl ADD CONSTRAINT pk_ent_rel_part_fam_tbl PRIMARY KEY (ent_rel_id);
ALTER TABLE ent_rel_part_own_tbl ADD CONSTRAINT pk_ent_rel_part_own_tbl PRIMARY KEY (ent_rel_id);
ALTER TABLE ent_rel_part_stock_tbl ADD CONSTRAINT pk_ent_rel_part_stock_tbl PRIMARY KEY (ent_rel_id);
ALTER TABLE ent_rel_part_inf_tbl ADD CONSTRAINT pk_ent_rel_part_inf_tbl PRIMARY KEY (ent_rel_id);
--#!

-- ADD FKS
ALTER TABLE ent_rel_part_dsdl_tbl ADD CONSTRAINT fk_ent_rel_part_dsdl_rel_typ_cd_id FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl (cd_id);
ALTER TABLE ent_rel_part_fam_tbl ADD CONSTRAINT fk_ent_rel_part_fam_rel_typ_cd_id FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl (cd_id);
ALTER TABLE ent_rel_part_own_tbl ADD CONSTRAINT fk_ent_rel_part_own_rel_typ_cd_id FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl (cd_id);
ALTER TABLE ent_rel_part_stock_tbl ADD CONSTRAINT fk_ent_rel_part_stock_rel_typ_cd_id FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl (cd_id);
ALTER TABLE ent_rel_part_inf_tbl ADD CONSTRAINT fk_ent_rel_part_inf_rel_typ_cd_id FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl (cd_id);
ALTER TABLE ent_rel_part_dsdl_tbl ADD CONSTRAINT fk_ent_rel_part_dsdl_src_ent_id FOREIGN KEY (src_ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE ent_rel_part_fam_tbl ADD CONSTRAINT fk_ent_rel_part_fam_src_ent_id FOREIGN KEY (src_ent_id) REFERENCES ent_tbl (ent_id); 
ALTER TABLE ent_rel_part_own_tbl ADD CONSTRAINT fk_ent_rel_part_own_src_ent_id FOREIGN KEY (src_ent_id) REFERENCES ent_tbl (ent_id); 
ALTER TABLE ent_rel_part_stock_tbl ADD CONSTRAINT fk_ent_rel_part_stock_src_ent_id FOREIGN KEY (src_ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE ent_rel_part_inf_tbl ADD CONSTRAINT fk_ent_rel_part_inf_src_ent_id FOREIGN KEY (src_ent_id) REFERENCES ent_tbl (ent_id); 
ALTER TABLE ent_rel_part_dsdl_tbl ADD CONSTRAINT fk_ent_rel_part_dsdl_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE ent_rel_part_fam_tbl ADD CONSTRAINT fk_ent_rel_part_fam_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id); 
ALTER TABLE ent_rel_part_own_tbl ADD CONSTRAINT fk_ent_rel_part_own_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id); 
ALTER TABLE ent_rel_part_stock_tbl ADD CONSTRAINT fk_ent_rel_part_stock_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id);
ALTER TABLE ent_rel_part_inf_tbl ADD CONSTRAINT fk_ent_rel_part_inf_trg_ent_id FOREIGN KEY (trg_ent_id) REFERENCES ent_tbl (ent_id); 
--#!

-- INDEX OF SRC_ENT_ID
CREATE INDEX ent_rel_part_dsdl_src_ent_id_idx ON ent_rel_part_dsdl_tbl(src_ent_id);
CREATE INDEX ent_rel_part_fam_src_ent_id_idx ON ent_rel_part_fam_tbl(src_ent_id);
CREATE INDEX ent_rel_part_own_src_ent_id_idx ON ent_rel_part_own_tbl(src_ent_id);
CREATE INDEX ent_rel_part_stock_src_ent_id_idx ON ent_rel_part_stock_tbl(src_ent_id);
CREATE INDEX ent_rel_part_inf_src_ent_id_idx ON ent_rel_part_inf_tbl(src_ent_id);
--#!

CREATE INDEX ent_rel_part_dsdl_rel_typ_idx ON ent_rel_part_dsdl_tbl(rel_typ_cd_id);
CREATE INDEX ent_rel_part_fam_rel_typ_idx ON ent_rel_part_fam_tbl(rel_typ_cd_id);
CREATE INDEX ent_rel_part_stock_rel_typ_idx ON ent_rel_part_stock_tbl(rel_typ_cd_id);
CREATE INDEX ent_rel_part_inf_rel_typ_idx ON ent_rel_part_inf_tbl(rel_typ_cd_id);
--#!

CREATE INDEX ent_rel_part_dsdl_trg_ent_id_idx ON ent_rel_part_dsdl_tbl(trg_ent_id);
CREATE INDEX ent_rel_part_fam_trg_ent_id_idx ON ent_rel_part_fam_tbl(trg_ent_id);
CREATE INDEX ent_rel_part_own_trg_ent_id_idx ON ent_rel_part_own_tbl(trg_ent_id);
CREATE INDEX ent_rel_part_stock_trg_ent_id_idx ON ent_rel_part_stock_tbl(trg_ent_id);
CREATE INDEX ent_rel_part_inf_trg_ent_id_idx ON ent_rel_part_inf_tbl(trg_ent_id);
--#!

CREATE UNIQUE INDEX ent_rel_part_dsdl_unq_enf_sha1 ON ent_rel_part_dsdl_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
CREATE UNIQUE INDEX ent_rel_part_fam_unq_enf_sha1 ON ent_rel_part_fam_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
CREATE UNIQUE INDEX ent_rel_part_own_unq_enf_sha1 ON ent_rel_part_own_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
CREATE UNIQUE INDEX ent_rel_part_stock_unq_enf_sha1 ON ent_rel_part_stock_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
CREATE UNIQUE INDEX ent_rel_part_inf_unq_enf_sha1 ON ent_rel_part_inf_tbl (digest((src_ent_id::text || trg_ent_id::text) || rel_typ_cd_id::text, 'sha1'::text)) WHERE obslt_vrsn_seq_id IS NULL;
--#!

CREATE TRIGGER ent_rel_part_dsdl_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_dsdl_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
CREATE TRIGGER ent_rel_part_fam_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_fam_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
CREATE TRIGGER ent_rel_part_own_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_own_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
CREATE TRIGGER ent_rel_part_stock_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_stock_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
CREATE TRIGGER ent_rel_part_inf_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_part_inf_tbl FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();
--#!

ALTER TABLE ent_rel_tbl RENAME TO ent_rel_tbl_bak;
ALTER TABLE ent_rel_part_tbl RENAME TO ent_rel_tbl;
--#!


