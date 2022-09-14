/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220414-01" name="Update:20220414-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds HEAD column to the versioned tables</summary>
 *	<isInstalled>select ck_patch('20220414-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */
-- OPTIONAL
ALTER TABLE ent_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
-- OPTIONAL
ALTER TABLE cd_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
-- OPTIONAL
ALTER TABLE act_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!

UPDATE ent_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE cd_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE act_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!

-- OPTIONAL
CREATE UNIQUE INDEX ent_vrsn_head_uq_idx ON ent_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN ENT_ID END);--#!
-- OPTIONAL
CREATE UNIQUE INDEX act_vrsn_head_uq_idx ON act_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN ACT_ID END);--#!
-- OPTIONAL
CREATE UNIQUE INDEX cd_vrsn_head_uq_idx ON cd_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN CD_ID END);--#!

-- OPTIONAL
CREATE TABLE pat_enc_arg_tbl (
	arg_id UUID NOT NULL,
	act_id UUID NOT NULL,
	efft_vrsn_seq_id INTEGER NOT NULL, -- THE VERSION SEQUENCE WHERE THIS BECOMES EFFECTIVE
	obslt_vrsn_seq_id INTEGER, -- THE VERSION SEQUENCE WHERE THIS IS NO LONGER EFFECTIVE
	typ_cd_id UUID NOT NULL,
	start_utc TIMESTAMP,
	stop_utc TIMESTAMP,
	CONSTRAINT pk_pat_enc_arg_tbl PRIMARY KEY (arg_id),
	CONSTRAINT fk_pat_enc_arg_act_id FOREIGN KEY (act_id) REFERENCES act_tbl(ACT_ID),
	CONSTRAINT ck_pat_enc_arg_act_cls CHECK (IS_ACT_CLS(ACT_ID, 'Encounter')),
	CONSTRAINT fk_pat_enc_arg_efft_vrsn_seq FOREIGN KEY (efft_vrsn_seq_id) REFERENCES act_vrsn_tbl(vrsn_seq_id),
	CONSTRAINT fk_pat_enc_arg_obslt_vrsn_seq FOREIGN KEY (obslt_vrsn_seq_id) REFERENCES act_vrsn_tbl(vrsn_seq_id),
	CONSTRAINT fk_pat_enc_arg_typ_cd FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT ck_pat_enc_arg_time CHECK (CASE WHEN start_utc IS NOT NULL AND stop_utc IS NOT NULL THEN start_utc < stop_utc ELSE true END)
);--#!

-- OPTIONAL
ALTER TABLE PAT_ENC_TBL ADD adm_src_cd_id UUID;--#!
-- OPTIONAL
ALTER TABLE PAT_ENC_TBL ADD CONSTRAINT fk_pat_enc_adm_src_cd_id FOREIGN KEY (adm_src_cd_id) REFERENCES cd_tbl(cd_id);--#!
-- OPTIONAL
ALTER TABLE QTY_OBS_TBL ALTER COLUMN QTY TYPE NUMERIC(15,5);--#!
-- OPTIONAL
ALTER TABLE QTY_OBS_TBL DROP QTY_PRC; --#!

-- VALIDATION OF ENTITY RELATIONSHIPS
-- OPTIONAL
CREATE TABLE rel_vrfy_systbl (
	rel_vrfy_id UUID NOT NULL,
	rel_typ_cd_id UUID NOT NULL, -- THE TYPE OF RELATIONSHIP
	src_cls_cd_id UUID, -- THE CLASS CODE OF THE SOURCE ENTITY
	trg_cls_cd_id UUID, -- THE CLASS CODE OF THE TARGET ENTITY
	err_desc VARCHAR(128) NOT NULL, -- THE ERROR CONDITION
	rel_cls INTEGER DEFAULT 1 NOT NULL CHECK (rel_cls IN (1,2,3)),
	CONSTRAINT pk_rel_vrfy_systbl PRIMARY KEY (rel_vrfy_id),
	CONSTRAINT fk_rel_vrfy_rel_typ_cd FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT fk_rel_vrfy_src_cls_cd FOREIGN KEY (src_cls_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT fk_rel_vrfy_trg_cls_cd FOREIGN KEY (trg_cls_cd_id) REFERENCES cd_tbl(cd_id)
);
--#!

-- OPTIONAL
INSERT INTO rel_vrfy_systbl 
SELECT 
	ent_rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, 1 AS rel_cs
FROM 
	ent_rel_vrfy_cdtbl;
	--#!
-- OPTIONAL
DROP INDEX ent_rel_vrfy_src_trg_unq;--#!
-- OPTIONAL
DROP TRIGGER  TG_ENT_REL_VRFY_CDTBL_SEQ;--#!
-- OPTIONAL
DROP TRIGGER  TG_VRFY_ENT_REL;--#!
-- OPTIONAL
DROP TABLE ENT_REL_VRFY_CDTBL;--#!

-- OPTIONAL
CREATE OR ALTER TRIGGER TG_REL_VRFY_CDTBL_SEQ FOR rel_vrfy_systbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.rel_vrfy_id = gen_uuid();
END;
--#!

-- OPTIONAL
CREATE UNIQUE INDEX rel_vrfy_src_trg_unq ON rel_vrfy_systbl(rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id);
--#!


-- TRIGGER FUNCTION WHICH VERIFIES ENTITY RELATIONSHIP
-- OPTIONAL
CREATE TRIGGER TG_ENT_VRFY_REL FOR ENT_REL_TBL BEFORE INSERT OR UPDATE POSITION 0 AS 
BEGIN
	IF (NEW.obslt_vrsn_seq_id IS NULL AND NOT EXISTS(SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.src_ent_id AND head AND cls_cd_id = COALESCE(src_cls_cd_id, cls_cd_id))
			AND EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.trg_ent_id AND head AND cls_cd_id = COALESCE(trg_cls_cd_id, cls_cd_id))
			AND rel_typ_cd_id = NEW.rel_typ_cd_id
			AND rel_cls = 1)) THEN
		EXCEPTION invalid_relationships 
			'Validation error: Relationship ' || uuid_to_char(NEW.rel_typ_cd_id) || ' between ' || uuid_to_char(NEW.src_ent_id) || ' > ' || uuid_to_char(NEW.trg_ent_id) || ' is invalid';
END;
--#!
-- TRIGGER FUNCTION WHICH VERIFIES ACT PARTICIPATION
-- OPTIONAL
CREATE TRIGGER TG_ACT_VRFY_REL FOR ACT_REL_TBL BEFORE INSERT OR UPDATE POSITION 0 AS 
BEGIN
	IF (NEW.obslt_vrsn_seq_id IS NULL AND NOT EXISTS(SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.src_act_id AND head AND cls_cd_id = COALESCE(src_cls_cd_id, cls_cd_id))
			AND EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.trg_act_id AND head AND cls_cd_id = COALESCE(trg_cls_cd_id, cls_cd_id))
			AND rel_typ_cd_id = NEW.rel_typ_cd_id
			AND rel_cls = 2)) THEN
		EXCEPTION invalid_relationships 
			'Validation error: Relationship ' || uuid_to_char(NEW.rel_typ_cd_id) || ' between ' || uuid_to_char(NEW.src_act_id) || ' > ' || uuid_to_char(NEW.trg_act_id) || ' is invalid';
END;
--#!
-- TRIGGER FUNCTION WHICH VERIFIES ENTITY RELATIONSHIP
-- OPTIONAL
CREATE TRIGGER TG_ACT_VRFY_PTCPT FOR ACT_PTCPT_TBL BEFORE INSERT OR UPDATE POSITION 0 AS 
BEGIN
	IF (NEW.obslt_vrsn_seq_id IS NULL AND NOT EXISTS(SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.act_id AND head AND cls_cd_id = COALESCE(src_cls_cd_id, cls_cd_id))
			AND EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.ent_id AND head AND cls_cd_id = COALESCE(trg_cls_cd_id, cls_cd_id))
			AND rel_typ_cd_id = NEW.rol_cd_id
			AND rel_cls = 3)) THEN
		EXCEPTION invalid_relationships 
			'Validation error: Participation ' || uuid_to_char(NEW.rol_cd_id) || ' between ' || uuid_to_char(NEW.act_id) || ' > ' || uuid_to_char(NEW.ent_id) || ' is invalid';
END;
--#!


SELECT REG_PATCH('20220414-01') FROM RDB$DATABASE; --#!

