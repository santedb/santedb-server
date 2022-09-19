/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210514-01" name="Update:20210514-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Upgrades primary database from Version 2.0 to Version 3.0</summary>
 *	<isInstalled>select ck_patch('20210514-01')</isInstalled>
 * </feature>
 */

UPDATE sec_rol_tbl SET rol_name = 'APPLICATIONS' WHERE rol_name = 'SYNCHRONIZERS';
ALTER TABLE sec_ses_tbl DROP CONSTRAINT CK_SEC_SES_RFRSH_EXP; 
ALTER TABLE sec_ses_tbl ADD CONSTRAINT CK_SEC_SES_RFRSH_EXP CHECK (RFRSH_EXP_UTC >= EXP_UTC);

CREATE OR REPLACE VIEW CD_CUR_VRSN_VW AS
	SELECT CD_VRSN_TBL.*, FALSE AS IS_SYS, CD_CLS_TBL.MNEMONIC AS CLS_MNEMONIC 
	FROM CD_VRSN_TBL INNER JOIN CD_TBL USING (CD_ID)
		INNER JOIN CD_CLS_TBL USING (CLS_ID)
		WHERE CD_VRSN_TBL.OBSLT_UTC IS NULL;

ALTER TABLE CD_TBL DROP IF EXISTS IS_SYS;
--#!
-- INFO: Migrating Entities to new format...
CREATE TABLE ENT_VRSN_UPD_TBL AS (SELECT * FROM ENT_VRSN_TBL) WITH NO DATA;

-- REFACTOR THE ENTITY TABLES

ALTER TABLE ENT_VRSN_UPD_TBL add IF NOT EXISTS CLS_CD_ID UUID;
ALTER TABLE ENT_VRSN_UPD_TBL add IF NOT EXISTS TPL_ID UUID;
ALTER TABLE ENT_VRSN_UPD_TBL add IF NOT EXISTS DTR_CD_ID UUID;
INSERT INTO ENT_VRSN_UPD_TBL
	(ENT_VRSN_ID, VRSN_SEQ_ID, ENT_ID, RPLC_VRSN_ID, STS_CD_ID, TYP_CD_ID, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID, CRT_ACT_ID, CLS_CD_ID, TPL_ID, DTR_CD_ID)
	SELECT ENT_VRSN_ID, VRSN_SEQ_ID, ENT_ID, RPLC_VRSN_ID, STS_CD_ID, TYP_CD_ID, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID, CRT_ACT_ID, ENT_TBL.CLS_CD_ID, ENT_TBL.TPL_ID, ENT_TBL.DTR_CD_ID
	FROM 
		ENT_VRSN_TBL 
		INNER JOIN ENT_TBL USING (ENT_ID)
	;

--#! 
-- INFO:	Re-Creating Foreign Keys....
DROP TABLE ENT_VRSN_TBL CASCADE;
ALTER TABLE ENT_VRSN_UPD_TBL RENAME TO ENT_VRSN_TBL;
ALTER TABLE ENT_VRSN_TBL ALTER ENT_VRSN_ID SET DEFAULT uuid_generate_v1();
ALTER TABLE ENT_VRSN_TBL ALTER VRSN_SEQ_ID SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ALTER VRSN_SEQ_ID SET DEFAULT nextval('ENT_VRSN_SEQ');
ALTER TABLE ENT_VRSN_TBL ALTER ENT_ID SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ALTER STS_CD_ID SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ALTER CRT_UTC SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ALTER CRT_UTC SET DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE ENT_VRSN_TBL ALTER CRT_PROV_ID SET NOT NULL;
CREATE UNIQUE INDEX ent_vrsn_vrsn_seq_id_idx ON ent_vrsn_tbl USING btree (vrsn_seq_id);--#!
ALTER TABLE ENT_VRSN_TBL ADD CONSTRAINT pk_ent_vrsn_tbl PRIMARY KEY (ent_vrsn_id);

ALTER TABLE PSN_LNG_TBL ADD constraint fk_psn_lng_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE PSN_LNG_TBL ADD constraint fk_psn_lng_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE PLC_SVC_TBL ADD constraint fk_PLC_SVC__obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE PLC_SVC_TBL ADD constraint fk_PLC_SVC__efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_tel_tbl ADD constraint fk_ent_tel_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_tel_tbl ADD constraint fk_ent_tel_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_rel_tbl ADD constraint fk_ent_rel_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_rel_tbl ADD constraint fk_ent_rel_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_note_tbl ADD constraint fk_ent_note_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_note_tbl ADD constraint fk_ent_note_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_name_tbl ADD constraint fk_ent_name_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_name_tbl ADD constraint fk_ent_name_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_id_tbl ADD constraint fk_ent_id_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_id_tbl ADD constraint fk_ent_id_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_ext_tbl ADD constraint fk_ent_ext_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_ext_tbl ADD constraint fk_ent_ext_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_addr_tbl ADD constraint fk_ent_addr_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_addr_tbl ADD constraint fk_ent_addr_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_pol_assoc_tbl ADD constraint fk_ent_pol_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_pol_assoc_tbl ADD constraint fk_ent_pol_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_crt_act FOREIGN KEY (crt_act_id) REFERENCES act_tbl(act_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_crt_prov_id FOREIGN KEY (crt_prov_id) REFERENCES sec_prov_tbl(prov_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl(ent_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_obslt_prov_id FOREIGN KEY (obslt_prov_id) REFERENCES sec_prov_tbl(prov_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_rplc_vrsn_seq_id FOREIGN KEY (rplc_vrsn_id) REFERENCES ent_vrsn_tbl(ent_vrsn_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_sts_cd_id FOREIGN KEY (sts_cd_id) REFERENCES cd_tbl(cd_id);--#!
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_typ_cd_id FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id);--#!

ALTER TABLE psn_tbl ADD CONSTRAINT FK_PSN_ENT_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!
ALTER TABLE mat_tbl ADD CONSTRAINT FK_MAT_ENT_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!
ALTER TABLE dev_ent_tbl ADD CONSTRAINT FK_DEV_ENT_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!
ALTER TABLE app_ent_tbl ADD CONSTRAINT FK_APP_ENT_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!
ALTER TABLE org_tbl ADD CONSTRAINT FK_ORG_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!
ALTER TABLE plc_tbl ADD CONSTRAINT FK_PLC_VRSN_ID FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID);--#!

--#! 
-- INFO:	Re-creating entity utility functions...
-- ASSRERT ACT IS A PARTICULAR CLASS
CREATE OR REPLACE FUNCTION IS_ENT_CLS(
	ENT_ID_IN IN UUID,
	CLS_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS 
$$
BEGIN
	RETURN EXISTS (SELECT 1 FROM ENT_VRSN_TBL INNER JOIN CD_VRSN_TBL ON (ENT_VRSN_TBL.CLS_CD_ID = CD_VRSN_TBL.CD_ID) WHERE ENT_ID = ENT_ID_IN AND CD_VRSN_TBL.MNEMONIC = CLS_MNEMONIC_IN AND ENT_VRSN_TBL.OBSLT_UTC IS NULL AND CD_VRSN_TBL.OBSLT_UTC IS NULL);
END $$ LANGUAGE PLPGSQL;

-- TRIGGER FUNCTION WHICH VERIFIES ENTITY RELATIONSHIP
DROP TRIGGER IF EXISTS ent_rel_tbl_vrfy ON ent_rel_tbl;
DROP FUNCTION IF EXISTS  trg_vrfy_ent_rel_tbl;
DROP FUNCTION IF EXISTS  vrfy_ent_rel;--#!

CREATE OR REPLACE FUNCTION vrfy_ent_rel(src_ent_id_in uuid, trg_ent_id_in uuid, rel_typ_cd_id_in uuid)
 RETURNS boolean
AS $$
BEGIN 
	RETURN EXISTS (
		SELECT * 
		FROM 
			ent_rel_vrfy_cdtbl 
			INNER JOIN ent_vrsn_tbl src_ent ON (src_ent.ent_id = src_ent_id_in and src_ent.obslt_utc IS NULL)
			INNER JOIN ent_vrsn_tbl trg_ent ON (trg_ent.ent_id = trg_ent_id_in and trg_ent.obslt_utc IS NULL)
		WHERE 
			rel_typ_cd_id = rel_typ_cd_id_in
			AND src_cls_cd_id = src_ent.cls_cd_id 
			AND trg_cls_cd_id = trg_ent.cls_cd_id
	);
END;
$$
 LANGUAGE plpgsql
;

CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl()
 RETURNS trigger
AS $$
BEGIN
	if not vrfy_ent_rel(new.src_ent_id, new.trg_ent_id, new.rel_typ_cd_id) then
		RAISE EXCEPTION 'Validation error: Relationship %  between % > % is invalid', NEW.rel_typ_cd_id, NEW.src_ent_id, NEW.trg_ent_id
			USING ERRCODE = 'O9001';
	END IF;
	RETURN NEW;
END;
$$
 LANGUAGE plpgsql;--#!

create trigger ent_rel_tbl_vrfy before insert or update on
    ent_rel_tbl for each row execute procedure trg_vrfy_ent_rel_tbl();


ALTER TABLE ENT_TBL DROP  IF EXISTS CLS_CD_ID;
ALTER TABLE ENT_TBL DROP  IF EXISTS TPL_ID;
ALTER TABLE ENT_TBL DROP  IF EXISTS DTR_CD_ID;
ALTER TABLE ENT_VRSN_TBL ALTER CLS_CD_ID SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ALTER DTR_CD_ID SET NOT NULL;
ALTER TABLE ENT_VRSN_TBL ADD CONSTRAINT FK_ENT_VRSN_CLS_CD FOREIGN KEY (CLS_CD_ID) REFERENCES CD_TBL(CD_ID);--#!
ALTER TABLE ENT_VRSN_TBL ADD CONSTRAINT FK_ENT_VRSN_DTR_CD FOREIGN KEY (DTR_CD_ID) REFERENCES CD_TBL(CD_ID);--#!
ALTER TABLE ENT_VRSN_TBL ADD CONSTRAINT FK_ENT_VRSN_TPL FOREIGN KEY (TPL_ID) REFERENCES TPL_DEF_TBL(TPL_ID);--#!

--#!
-- INFO: Migrating Act Tables...
-- TODO: This could be updated to make conversion of large ACT datasets faster

CREATE TABLE ACT_VRSN_UPD_TBL AS (SELECT * FROM ACT_VRSN_TBL) WITH NO DATA;

-- REFACTOR THE ACT TABLES

--#!
-- INFO:	Re-Applying Foreign Keys....
ALTER TABLE ACT_VRSN_UPD_TBL add IF NOT EXISTS CLS_CD_ID UUID;
ALTER TABLE ACT_VRSN_UPD_TBL add IF NOT EXISTS TPL_ID UUID;
ALTER TABLE ACT_VRSN_UPD_TBL add IF NOT EXISTS MOD_CD_ID UUID;
INSERT INTO ACT_VRSN_UPD_TBL
	(ACT_VRSN_ID, VRSN_SEQ_ID, ACT_ID, RPLC_VRSN_ID, STS_CD_ID, TYP_CD_ID, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID, CLS_CD_ID, TPL_ID, MOD_CD_ID, NEG_IND, ACT_UTC, ACT_START_UTC, ACT_STOP_UTC)
	SELECT ACT_VRSN_ID, VRSN_SEQ_ID, ACT_ID, RPLC_VRSN_ID, STS_CD_ID, TYP_CD_ID, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID, CLS_CD_ID, TPL_ID, MOD_CD_ID, NEG_IND, ACT_UTC, ACT_START_UTC, ACT_STOP_UTC
	FROM 
		ACT_VRSN_TBL 
		INNER JOIN ACT_TBL USING (ACT_ID)
	;
DROP TABLE ACT_VRSN_TBL CASCADE;
ALTER TABLE ACT_VRSN_UPD_TBL RENAME TO ACT_VRSN_TBL;
ALTER TABLE ACT_VRSN_TBL ALTER ACT_VRSN_ID SET DEFAULT uuid_generate_v1();
ALTER TABLE ACT_VRSN_TBL ALTER VRSN_SEQ_ID SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER VRSN_SEQ_ID SET DEFAULT nextval('ACT_VRSN_SEQ');
ALTER TABLE ACT_VRSN_TBL ALTER ACT_ID SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER STS_CD_ID SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER NEG_IND SET DEFAULT FALSE;
UPDATE ACT_VRSN_TBL SET NEG_IND = FALSE WHERE NEG_IND IS NULL;
ALTER TABLE ACT_VRSN_TBL ALTER NEG_IND SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER CRT_UTC SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER CRT_UTC SET DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE ACT_VRSN_TBL ALTER CRT_PROV_ID SET NOT NULL;

CREATE UNIQUE INDEX act_vrsn_vrsn_seq_id_idx ON act_vrsn_tbl USING btree (vrsn_seq_id);--#!
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT pk_act_vrsn_tbl PRIMARY KEY (act_vrsn_id);

DROP INDEX IF EXISTS ACT_CLS_CD_IDX;
ALTER TABLE ACT_TBL DROP IF EXISTS CLS_CD_ID;
ALTER TABLE ACT_TBL DROP IF EXISTS TPL_ID;
ALTER TABLE ACT_TBL DROP IF EXISTS MOD_CD_ID;
ALTER TABLE ACT_VRSN_TBL ALTER CLS_CD_ID SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ALTER MOD_CD_ID SET NOT NULL;
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT FK_ACT_VRSN_CLS_CD FOREIGN KEY (CLS_CD_ID) REFERENCES CD_TBL(CD_ID);--#!
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT FK_ACT_VRSN_MOD_CD FOREIGN KEY (MOD_CD_ID) REFERENCES CD_TBL(CD_ID);--#!
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT FK_ACT_VRSN_TPL FOREIGN KEY (TPL_ID) REFERENCES TPL_DEF_TBL(TPL_ID);--#!


ALTER TABLE act_rel_tbl ADD constraint fk_act_rel_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_rel_tbl ADD constraint fk_act_rel_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_ptcpt_tbl ADD constraint fk_act_ptcpt_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_ptcpt_tbl ADD constraint fk_act_ptcpt_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_note_tbl ADD constraint fk_act_note_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_note_tbl ADD constraint fk_act_note_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_id_tbl ADD constraint fk_act_id_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_id_tbl ADD constraint fk_act_id_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_ext_tbl ADD constraint fk_act_ext_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_ext_tbl ADD constraint fk_act_ext_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_pol_assoc_tbl ADD constraint fk_act_pol_obslt_vrsn_seq_id FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!
ALTER TABLE act_pol_assoc_tbl ADD constraint fk_act_pol_tbl_efft_vrsn_seq_id FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ACT_VRSN_TBL (VRSN_SEQ_ID);--#!

ALTER TABLE cntrl_act_tbl ADD CONSTRAINT FK_CNTRL_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE obs_tbl ADD CONSTRAINT FK_OBS_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE pat_enc_tbl ADD CONSTRAINT FK_PAT_ENC_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE proc_tbl ADD CONSTRAINT FK_PROC_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE sub_adm_tbl ADD CONSTRAINT FK_SUB_ADM_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE IF EXISTS nar_tbl ADD CONSTRAINT FK_NAR_ACT_VRSN_ID FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_crt_prov_id FOREIGN KEY (crt_prov_id) REFERENCES sec_prov_tbl(prov_id);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_act_id FOREIGN KEY (act_id) REFERENCES act_tbl(act_id);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_obslt_prov_id FOREIGN KEY (obslt_prov_id) REFERENCES sec_prov_tbl(prov_id);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_rplc_vrsn_seq_id FOREIGN KEY (rplc_vrsn_id) REFERENCES act_vrsn_tbl(act_vrsn_id);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_sts_cd_id FOREIGN KEY (sts_cd_id) REFERENCES cd_tbl(cd_id);--#!
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_typ_cd_id FOREIGN KEY (typ_cd_id) REFERENCES cd_tbl(cd_id);--#!

--#!
-- INFO: Re-Applying Indexes ...
CREATE INDEX act_vrsn_crt_utc_idx ON act_vrsn_tbl USING btree (crt_utc);--#!
CREATE INDEX act_vrsn_ent_id_idx ON act_vrsn_tbl USING btree (act_id);--#!
CREATE INDEX act_vrsn_sts_cd_id_idx ON act_vrsn_tbl USING btree (sts_cd_id);--#!
CREATE INDEX ent_vrsn_crt_utc_idx ON ent_vrsn_tbl USING btree (crt_utc);--#!
CREATE INDEX ent_vrsn_ent_id_idx ON ent_vrsn_tbl USING btree (ent_id);--#!
CREATE INDEX ent_vrsn_sts_cd_id_idx ON ent_vrsn_tbl USING btree (sts_cd_id);--#!
-- INFO: Re-Applying Time Check Constraints...
UPDATE ACT_VRSN_TBL SET ACT_UTC = CRT_UTC WHERE ACT_UTC IS NULL AND ACT_START_UTC IS NULL AND ACT_STOP_UTC IS NULL;
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT ck_act_vrsn_act_utc CHECK (((sts_cd_id = '39995c08-0a5c-4549-8ba7-d187f9b3c4fd'::uuid) OR ((act_utc IS NOT NULL) OR (act_start_utc IS NOT NULL) OR (act_stop_utc IS NOT NULL))));
ALTER TABLE act_vrsn_tbl ADD	CONSTRAINT ck_act_vrsn_obslt_usr CHECK ((((obslt_prov_id IS NOT NULL) AND (obslt_utc IS NOT NULL)) OR ((obslt_prov_id IS NULL) AND (obslt_utc IS NULL))));
ALTER TABLE ent_vrsn_tbl ADD	CONSTRAINT ck_ent_vrsn_obslt_usr CHECK ((((obslt_prov_id IS NOT NULL) AND (obslt_utc IS NOT NULL)) OR ((obslt_prov_id IS NULL) AND (obslt_utc IS NULL))));
--#!
-- INFO: Re-Applying Status Check Constraints...
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT ck_act_vrsn_sts_cd CHECK (ck_is_cd_set_mem(sts_cd_id, 'ActStatus'::character varying, false));
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT ck_ent_vrsn_sts_cd CHECK (ck_is_cd_set_mem(sts_cd_id, 'EntityStatus'::character varying, false));
--#!
-- INFO: Re-Applying Class Check Constraints...
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT ck_act_vrsn_cls_cd CHECK (ck_is_cd_set_mem(cls_cd_id, 'ActClass'::character varying, false));
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT ck_ent_vrsn_cls_cd CHECK (ck_is_cd_set_mem(cls_cd_id, 'EntityClass'::character varying, false));
--#!
-- INFO: Re-Applying Mood Code Constraints...
ALTER TABLE act_vrsn_tbl ADD CONSTRAINT fk_act_vrsn_mod_cd CHECK (is_cd_set_mem(dtr_cd_id, 'ActMood'::character varying));
ALTER TABLE ent_vrsn_tbl ADD CONSTRAINT fk_ent_vrsn_dtr_cd CHECK (is_cd_set_mem(dtr_cd_id, 'EntityDeterminer'::character varying));

-- ASSRERT ACT IS A PARTICULAR CLASS
CREATE OR REPLACE FUNCTION IS_ACT_CLS(
	ACT_ID_IN IN UUID,
	CLS_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS 
$$
BEGIN
	RETURN EXISTS (SELECT 1 FROM ACT_VRSN_TBL INNER JOIN CD_VRSN_TBL ON (ACT_VRSN_TBL.CLS_CD_ID = CD_VRSN_TBL.CD_ID) WHERE ACT_ID = ACT_ID_IN AND CD_VRSN_TBL.MNEMONIC = CLS_MNEMONIC_IN AND ACT_VRSN_TBL.OBSLT_UTC IS NULL AND CD_VRSN_TBL.OBSLT_UTC IS NULL);
END
$$ LANGUAGE PLPGSQL;--#!

-- INFO: Adding Geo-Tagging Tables

CREATE TABLE GEO_TBL (
	GEO_ID UUID NOT NULL,
	LAT REAL NOT NULL,
	LNG REAL NOT NULL,
	CONSTRAINT PK_GEO_TBL PRIMARY KEY (GEO_ID)
);

ALTER TABLE ACT_VRSN_TBL ADD GEO_ID UUID;
ALTER TABLE ENT_VRSN_TBL ADD GEO_ID UUID;
ALTER TABLE ACT_VRSN_TBL ADD CONSTRAINT FK_ACT_VRSN_GEO_TBL FOREIGN KEY (GEO_ID) REFERENCES GEO_TBL(GEO_ID);
ALTER TABLE ENT_VRSN_TBL ADD CONSTRAINT FK_ENT_VRSN_GEO_TBL FOREIGN KEY (GEO_ID) REFERENCES GEO_TBL(GEO_ID);
ALTER TABLE PLC_TBL DROP LAT;
ALTER TABLE PLC_TBL DROP LNG;--#!

--#!
-- INFO: Adding new indexes...
CREATE INDEX ACT_VRSN_CLS_CD_IDX ON ACT_VRSN_TBL (CLS_CD_ID);--#!
CREATE INDEX ACT_VRSN_STS_CD_IDX ON ACT_VRSN_TBL (STS_CD_ID);--#!
CREATE INDEX ENT_VRSN_CLS_CD_IDX ON ENT_VRSN_TBL (CLS_CD_ID);--#!
CREATE INDEX ENT_VRSN_STS_CD_IDX ON ENT_VRSN_TBL (STS_CD_ID);--#!


CREATE OR REPLACE FUNCTION is_cd_set_mem(cd_id_in uuid, set_id_in uuid)
 RETURNS boolean
AS $$
BEGIN
 RETURN EXISTS (SELECT 1 FROM CD_SET_MEM_ASSOC_TBL 
		 WHERE
	 		cd_id_in = cd_id AND
			set_id = set_id_in);
END; $$ LANGUAGE PLPGSQL;

CLUSTER ACT_VRSN_TBL USING ACT_VRSN_CLS_CD_IDX;
CLUSTER ENT_VRSN_TBL USING ENT_VRSN_CLS_CD_IDX;


-- TRIGGER VALIDATE ENTITIES
CREATE OR REPLACE FUNCTION trg_vrfy_ent_tbl()
 RETURNS trigger
AS $$
BEGIN
	if not is_cd_set_mem(new.sts_cd_id, 'C7578340-A8FF-4D7D-8105-581016324E68'::uuid) then
		RAISE EXCEPTION 'Code % not valid EntityStatus code', NEW.sts_cd_id
			USING ERRCODE = 'O9002';
	ELSIF NOT is_cd_set_mem(new.CLS_CD_ID, '4E6DA567-0094-4F23-8555-11DA499593AF'::uuid) THEN
		RAISE EXCEPTION 'Code % not valid EntityClass code', NEW.cls_cd_id
			USING ERRCODE = 'O9002';
	ELSIF NOT is_cd_set_mem(new.DTR_CD_ID, 'effc9f86-56e2-4920-a75a-fec310aa1430'::uuid) THEN
		RAISE EXCEPTION 'Code % not valid EntityDeterminer code', NEW.dtr_cd_id
			USING ERRCODE = 'O9002';
	END IF;
	RETURN NEW;
END;
$$
 LANGUAGE plpgsql;--#!

create trigger ent_vrsn_tbl_vrfy before insert or update on
    ent_vrsn_tbl for each row execute procedure trg_vrfy_ent_tbl();
   
 
-- TRIGGER VALIDATE ACTS
CREATE OR REPLACE FUNCTION trg_vrfy_act_tbl()
 RETURNS trigger
AS $$
BEGIN
	if not is_cd_set_mem(new.sts_cd_id, '93A48F6A-6808-4C70-83A2-D02178C2A883'::uuid) then
		RAISE EXCEPTION 'Code % not valid ActStatus code', NEW.sts_cd_id
			USING ERRCODE = 'O9002';
	ELSIF NOT is_cd_set_mem(new.CLS_CD_ID, '62C5FDE0-A3AA-45DF-94E9-242F4451644A'::uuid) THEN
		RAISE EXCEPTION 'Code % not valid ActClass code', NEW.cls_cd_id
			USING ERRCODE = 'O9002';
	ELSIF NOT is_cd_set_mem(new.mod_CD_ID, 'E6A8E44F-0A57-4EBD-80A9-5C53B7A03D76'::uuid) THEN
		RAISE EXCEPTION 'Code % not valid ActMood code', NEW.mod_cd_id
			USING ERRCODE = 'O9002';
	END IF;
	RETURN NEW;
END;
$$
 LANGUAGE plpgsql;

create trigger act_vrsn_tbl_vrfy before insert or update on
    act_vrsn_tbl for each row execute procedure trg_vrfy_act_tbl();--#!
   


SELECT REG_PATCH('20210514-01');

