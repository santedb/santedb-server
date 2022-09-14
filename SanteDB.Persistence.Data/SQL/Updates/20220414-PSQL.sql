/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220414-01" name="Update:20220414-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds head version tracking column</summary>
 *	<isInstalled>select ck_patch('20220414-01')</isInstalled>
 * </feature>
 */

ALTER TABLE ent_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
ALTER TABLE cd_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
ALTER TABLE act_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!

UPDATE ent_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE cd_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE act_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!

CREATE UNIQUE INDEX ent_vrsn_head_uq_idx ON ent_vrsn_tbl(ENT_ID) WHERE (HEAD);--#!
CREATE UNIQUE INDEX act_vrsn_head_uq_idx ON act_vrsn_tbl(ACT_ID) WHERE (HEAD);--#!
CREATE UNIQUE INDEX cd_vrsn_head_uq_idx ON cd_vrsn_tbl(CD_ID) WHERE (HEAD);--#!


CREATE TABLE pat_enc_arg_tbl (
	arg_id UUID NOT NULL DEFAULT uuid_generate_v1(),
	act_id UUID NOT NULL,
	efft_vrsn_seq_id INTEGER NOT NULL, -- THE VERSION SEQUENCE WHERE THIS BECOMES EFFECTIVE
	obslt_vrsn_seq_id INTEGER, -- THE VERSION SEQUENCE WHERE THIS IS NO LONGER EFFECTIVE
	typ_cd_id UUID NOT NULL,
	start_utc TIMESTAMPTZ,
	stop_utc TIMESTAMPTZ,
	CONSTRAINT pk_pat_enc_arg_tbl PRIMARY KEY (arg_id),
	CONSTRAINT fk_pat_enc_arg_act_id FOREIGN KEY (act_id) REFERENCES act_tbl(ACT_ID),
	CONSTRAINT ck_pat_enc_arg_act_cls CHECK (IS_ACT_CLS(ACT_ID, 'Encounter')),
	CONSTRAINT fk_pat_enc_arg_efft_vrsn_seq FOREIGN KEY (efft_vrsn_seq_id) REFERENCES act_vrsn_tbl(vrsn_seq_id),
	CONSTRAINT fk_pat_enc_arg_obslt_vrsn_seq FOREIGN KEY (obslt_vrsn_seq_id) REFERENCES act_vrsn_tbl(vrsn_seq_id),
	CONSTRAINT fk_pat_enc_arg_typ_cd FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT ck_pat_enc_arg_time CHECK (CASE WHEN start_utc IS NOT NULL AND stop_utc IS NOT NULL THEN start_utc < stop_utc ELSE true END)
);--#!
ALTER TABLE PAT_ENC_TBL ADD adm_src_cd_id UUID;--#!
ALTER TABLE PAT_ENC_TBL ADD CONSTRAINT fk_pat_enc_adm_src_cd_id FOREIGN KEY (adm_src_cd_id) REFERENCES cd_tbl(cd_id);--#!

ALTER TABLE QTY_OBS_TBL ALTER COLUMN QTY TYPE NUMERIC(15,8);--#!
ALTER TABLE QTY_OBS_TBL DROP QTY_PRC; --#!

-- VALIDATION OF ENTITY RELATIONSHIPS
CREATE TABLE rel_vrfy_systbl (
	rel_vrfy_id UUID NOT NULL DEFAULT uuid_generate_v1(),
	rel_typ_cd_id UUID NOT NULL, -- THE TYPE OF RELATIONSHIP
	src_cls_cd_id UUID, -- THE CLASS CODE OF THE SOURCE ENTITY
	trg_cls_cd_id UUID, -- THE CLASS CODE OF THE TARGET ENTITY
	err_desc VARCHAR(128) NOT NULL, -- THE ERROR CONDITION
	rel_cls INTEGER NOT NULL DEFAULT 1 CHECK (rel_cls IN (1,2,3)),
	CONSTRAINT pk_rel_vrfy_systbl PRIMARY KEY (rel_vrfy_id),
	CONSTRAINT fk_rel_vrfy_rel_typ_cd FOREIGN KEY (rel_typ_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT fk_rel_vrfy_src_cls_cd FOREIGN KEY (src_cls_cd_id) REFERENCES cd_tbl(cd_id),
	CONSTRAINT fk_rel_vrfy_trg_cls_cd FOREIGN KEY (trg_cls_cd_id) REFERENCES cd_tbl(cd_id)
);

INSERT INTO rel_vrfy_systbl 
SELECT 
	ent_rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, 1 AS rel_cs
FROM 
	ent_rel_vrfy_cdtbl;

DROP INDEX ent_rel_vrfy_src_trg_unq;
DROP FUNCTION vrfy_ent_rel;
DROP FUNCTION trg_vrfy_ent_rel_tbl CASCADE;
DROP TABLE ENT_REL_VRFY_CDTBL;

CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl()
 RETURNS trigger
AS $$
BEGIN
	IF (obslt_vrsn_seq_id IS NULL AND NOT EXISTS(
		SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.src_ent_id AND head AND cls_cd_id = COALESCE(src_cls_cd_id, cls_cd_id))
			AND EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.trg_ent_id AND head AND cls_cd_id = COALESCE(trg_cls_cd_id, cls_cd_id))
			AND rel_typ_cd_id = NEW.rel_typ_cd_id
			AND rel_cls = 1
	)) THEN 
		RAISE EXCEPTION 'Validation error: Relationship %  between % > % is invalid', NEW.rel_typ_cd_id, NEW.src_ent_id, NEW.trg_ent_id
			USING ERRCODE = 'O9001';
	END IF;
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trg_vrfy_act_rel_tbl()
 RETURNS trigger
AS $$
BEGIN
	IF (obslt_vrsn_seq_id IS NULL AND NOT EXISTS(
		SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.src_act_id AND head AND cls_cd_id = src_cls_cd_id)
			AND EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.trg_act_id AND head AND cls_cd_id = trg_cls_cd_id)
			AND rel_typ_cd_id = NEW.rel_typ_cd_id
			AND rel_cls = 2
	)) THEN 
		RAISE EXCEPTION 'Validation error: Relationship %  between % > % is invalid', NEW.rel_typ_cd_id, NEW.src_act_id, NEW.trg_act_id
			USING ERRCODE = 'O9001';
	END IF;
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

create trigger act_rel_tbl_vrfy before insert or update on
    act_rel_tbl for each row execute procedure trg_vrfy_act_rel_tbl();


CREATE OR REPLACE FUNCTION trg_vrfy_act_ptcpt_tbl()
 RETURNS trigger
AS $$
BEGIN
	IF (obslt_vrsn_seq_id IS NULL AND NOT EXISTS(
		SELECT 1 
		FROM 
			rel_vrfy_systbl
		WHERE 
			EXISTS(SELECT 1 FROM act_vrsn_tbl WHERE act_id = NEW.act_id AND head AND cls_cd_id = src_cls_cd_id)
			AND EXISTS(SELECT 1 FROM ent_vrsn_tbl WHERE ent_id = NEW.ent_id AND head AND cls_cd_id = trg_cls_cd_id)
			AND rel_typ_cd_id = NEW.rel_typ_cd_id
			AND rel_cls = 3
	)) THEN 
		RAISE EXCEPTION 'Validation error: Relationship %  between % > % is invalid', NEW.rel_typ_cd_id, NEW.act_id, NEW.ent_id
			USING ERRCODE = 'O9001';
	END IF;
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

create trigger act_ptcpt_tbl_vrfy before insert or update on
    act_ptcpt_tbl for each row execute procedure trg_vrfy_act_ptcpt_tbl();


SELECT REG_PATCH('20220414-01'); --#!

 