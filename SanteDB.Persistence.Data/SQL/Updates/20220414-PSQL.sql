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

SELECT REG_PATCH('20220414-01'); --#!

 