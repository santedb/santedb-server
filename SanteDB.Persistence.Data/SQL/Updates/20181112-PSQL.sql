/** 
 * <feature scope="SanteDB.Persistence.Data" id="20181112-01" name="Update:20181112-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Update: Add lockout function to applications and devices</summary>
 *	<remarks>This patch adds lockout and update provenance to the application and device tables</remarks>
 *	<isInstalled>select ck_patch('20181112-01')</isInstalled>
 * </feature>
 */

 BEGIN TRANSACTION;

-- INFO: Adding Entity <> Policy Association
-- ENTITY POLICY ASSOCIATION
CREATE TABLE IF NOT EXISTS ENT_POL_ASSOC_TBL (
	SEC_POL_INST_ID UUID NOT NULL DEFAULT uuid_generate_v1(),
	ENT_ID UUID NOT NULL, -- THE ACT TO WHICH THE POLICY APPLIES
	EFFT_VRSN_SEQ_ID NUMERIC(20) NOT NULL, -- THE VERSION OF THE ACT WHERE THE POLICY ASSOCIATION DID BECOME ACTIVE
	OBSLT_VRSN_SEQ_ID NUMERIC(20), -- THE VERSION OF THE ACT WHERE THE POLICY ASSOCIATION IS OBSOLETE,
	POL_ID UUID NOT NULL, -- THE IDENTIFIER OF THE POLICY WHICH IS ATTACHED TO THE ACT
	CONSTRAINT PK_ENT_POL_ASSOC_TBL PRIMARY KEY(SEC_POL_INST_ID),
	CONSTRAINT FK_ENT_POL_ENT_ID FOREIGN KEY (ENT_ID) REFERENCES ENT_TBL(ENT_ID),
	CONSTRAINT FK_ENT_POL_EFFT_VRSN_SEQ_ID FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL(VRSN_SEQ_ID),
	CONSTRAINT FK_ENT_POL_OBSLT_VRSN_SEQ_ID FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL(VRSN_SEQ_ID),
	CONSTRAINT FK_ENT_POL_POL_ID FOREIGN KEY (POL_ID) REFERENCES SEC_POL_TBL(POL_ID)
);
DROP INDEX IF EXISTS ENT_POL_ASSOC_POL_ID_IDX ;
DROP INDEX IF EXISTS ENT_POL_ASSOC_VRSN_IDX ;
--#!
-- INFO: Indexing Entities
CREATE INDEX ENT_POL_ASSOC_POL_ID_IDX ON ENT_POL_ASSOC_TBL(POL_ID);
CREATE INDEX ENT_POL_ASSOC_VRSN_IDX ON ENT_POL_ASSOC_TBL(EFFT_VRSN_SEQ_ID, OBSLT_VRSN_SEQ_ID);
--#!
-- INFO: Adding lockout to applications
ALTER TABLE SEC_APP_TBL ADD LOCKED TIMESTAMPTZ; -- LOCKOUT PERIOD
ALTER TABLE SEC_APP_TBL ADD FAIL_AUTH INTEGER; -- FAILED AUTHENTICATION ATTEMPTS
ALTER TABLE SEC_APP_TBL ADD LAST_AUTH_UTC TIMESTAMPTZ; -- THE LAST AUTHETNICATION TIME
ALTER TABLE SEC_APP_TBL ADD UPD_UTC TIMESTAMP; -- THE CREATION TIME OF THE APP
ALTER TABLE SEC_APP_TBL ADD UPD_PROV_ID UUID; -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
ALTER TABLE SEC_APP_TBL ADD CONSTRAINT FK_SEC_APP_UPD_PROV_ID FOREIGN KEY (UPD_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID);

ALTER TABLE SEC_DEV_TBL ADD LOCKED TIMESTAMPTZ; -- LOCKOUT PERIOD
ALTER TABLE SEC_DEV_TBL ADD FAIL_AUTH INTEGER; -- FAILED AUTHENTICATION ATTEMPTS
ALTER TABLE SEC_DEV_TBL ADD LAST_AUTH_UTC TIMESTAMPTZ; -- THE LAST AUTHETNICATION TIME
ALTER TABLE SEC_DEV_TBL ADD UPD_UTC TIMESTAMP; -- THE CREATION TIME OF THE APP
ALTER TABLE SEC_DEV_TBL ADD UPD_PROV_ID UUID; -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
ALTER TABLE SEC_DEV_TBL ADD CONSTRAINT FK_SEC_DEV_UPD_PROV_ID FOREIGN KEY (UPD_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID);


DROP FUNCTION AUTH_APP (TEXT, TEXT);

-- AUTHENTICATE AN APPICATION
-- AUTHENTICATE AN APPICATION
CREATE OR REPLACE FUNCTION AUTH_APP (
	APP_PUB_ID_IN IN TEXT,
	APP_SCRT_IN IN TEXT,
	MAX_FAIL_AUTH_IN IN INTEGER
) RETURNS SETOF SEC_APP_TBL AS 
$$ 
DECLARE 
	APP_TPL SEC_APP_TBL;
BEGIN
	SELECT INTO APP_TPL * FROM SEC_APP_TBL WHERE APP_PUB_ID = APP_PUB_ID_IN LIMIT 1;
	IF (APP_TPL.LOCKED > CURRENT_TIMESTAMP) THEN
		APP_TPL.LOCKED = COALESCE(APP_TPL.LOCKED, CURRENT_TIMESTAMP) + ((APP_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
		UPDATE SEC_APP_TBL SET FAIL_AUTH = SEC_APP_TBL.FAIL_AUTH + 1, LOCKED = APP_TPL.LOCKED
			WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
		APP_TPL.APP_PUB_ID := ('ERR:AUTH_LCK:' || ((APP_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT));
		APP_TPL.APP_ID = UUID_NIL();
		APP_TPL.APP_SCRT = NULL;
		RETURN QUERY SELECT APP_TPL.*;
	ELSE
		-- LOCKOUT ACCOUNTS
		IF (APP_TPL.APP_SCRT = APP_SCRT_IN) THEN
			UPDATE SEC_APP_TBL SET 
				FAIL_AUTH = 0,
				LAST_AUTH_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8',
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			RETURN QUERY SELECT APP_TPL.*;
		ELSIF(APP_TPL.FAIL_AUTH > MAX_FAIL_AUTH_IN) THEN 
			APP_TPL.LOCKED = COALESCE(APP_TPL.LOCKED, CURRENT_TIMESTAMP) + ((APP_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
			UPDATE SEC_APP_TBL SET FAIL_AUTH = COALESCE(SEC_APP_TBL.FAIL_AUTH, 0) + 1, LOCKED = APP_TPL.LOCKED
				WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			APP_TPL.APP_PUB_ID := ('AUTH_LCK:' || ((APP_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
			APP_TPL.APP_ID := UUID_NIL();
			APP_TPL.APP_SCRT := NULL;
			RETURN QUERY SELECT APP_TPL.*;
		ELSE
			UPDATE SEC_APP_TBL SET FAIL_AUTH = COALESCE(SEC_APP_TBL.FAIL_AUTH, 0) + 1 WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			APP_TPL.APP_PUB_ID := ('AUTH_INV:' || APP_PUB_ID_IN)::VARCHAR;
			APP_TPL.APP_ID := UUID_NIL();
			APP_TPL.APP_SCRT := NULL;			
			RETURN QUERY SELECT APP_TPL.*;
		END IF;
	END IF;

END
$$ LANGUAGE PLPGSQL;

-- AUTHENTICATE A DEVICE
DROP FUNCTION AUTH_DEV (TEXT, TEXT);
CREATE OR REPLACE FUNCTION AUTH_DEV (
	DEV_PUB_ID_IN IN TEXT,
	DEV_SCRT_IN IN TEXT,
	MAX_FAIL_AUTH_IN IN INTEGER
) RETURNS SETOF SEC_DEV_TBL AS 
$$ 
DECLARE 
	DEV_TPL SEC_DEV_TBL;
BEGIN
	SELECT INTO DEV_TPL * FROM SEC_DEV_TBL WHERE DEV_PUB_ID = DEV_PUB_ID_IN LIMIT 1;
	IF (DEV_TPL.LOCKED > CURRENT_TIMESTAMP) THEN
		DEV_TPL.LOCKED = COALESCE(DEV_TPL.LOCKED, CURRENT_TIMESTAMP) + ((DEV_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
		UPDATE SEC_DEV_TBL SET FAIL_AUTH = SEC_DEV_TBL.FAIL_AUTH + 1, LOCKED = DEV_TPL.LOCKED
			WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
		DEV_TPL.DEV_PUB_ID := ('ERR:AUTH_LCK:' || ((DEV_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT));
		DEV_TPL.DEV_ID = UUID_NIL();
		DEV_TPL.DEV_SCRT = NULL;
		RETURN QUERY SELECT DEV_TPL.*;
	ELSE
		-- LOCKOUT ACCOUNTS
		IF (DEV_TPL.DEV_SCRT = DEV_SCRT_IN) THEN
			UPDATE SEC_DEV_TBL SET 
				FAIL_AUTH = 0,
				LAST_AUTH_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8',
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			RETURN QUERY SELECT DEV_TPL.*;
		ELSIF(DEV_TPL.FAIL_AUTH > MAX_FAIL_AUTH_IN) THEN 
			DEV_TPL.LOCKED = COALESCE(DEV_TPL.LOCKED, CURRENT_TIMESTAMP) + ((DEV_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
			UPDATE SEC_DEV_TBL SET FAIL_AUTH = COALESCE(SEC_DEV_TBL.FAIL_AUTH, 0) + 1, LOCKED = DEV_TPL.LOCKED
				WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			DEV_TPL.DEV_PUB_ID := ('AUTH_LCK:' || ((DEV_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
			DEV_TPL.DEV_ID := UUID_NIL();
			DEV_TPL.DEV_SCRT := NULL;
			RETURN QUERY SELECT DEV_TPL.*;
		ELSE
			UPDATE SEC_DEV_TBL SET FAIL_AUTH = COALESCE(SEC_DEV_TBL.FAIL_AUTH, 0) + 1 WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			DEV_TPL.DEV_PUB_ID := ('AUTH_INV:' || DEV_PUB_ID_IN)::VARCHAR;
			DEV_TPL.DEV_ID := UUID_NIL();
			DEV_TPL.DEV_SCRT := NULL;			
			RETURN QUERY SELECT DEV_TPL.*;
		END IF;
	END IF;

END
$$ LANGUAGE PLPGSQL;
--#!
-- INFO: Updating views and sequence types (performance enhancement)
drop view if exists  act_cur_vrsn_vw cascade;
drop view if exists  cd_cur_vrsn_vw cascade;
drop view if exists ent_cur_vrsn_vw cascade;
drop view if exists ent_cur_id_vw cascade;
--#!
alter table ent_pol_assoc_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_pol_assoc_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_rel_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_rel_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_addr_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_addr_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_ext_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_ext_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_id_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_id_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_name_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_name_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_note_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_note_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_rel_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_rel_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_tel_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table ent_tel_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table plc_svc_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table plc_svc_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table psn_lng_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table psn_lng_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table ent_vrsn_tbl alter vrsn_seq_id type integer;
--#!

alter table act_id_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_id_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_ext_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_ext_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_rel_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_rel_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_ptcpt_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_ptcpt_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_pol_assoc_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_pol_assoc_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_note_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table act_note_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table act_vrsn_tbl alter vrsn_seq_id type integer;
--#!

alter table cd_name_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table cd_name_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table cd_ref_term_assoc_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table cd_ref_term_assoc_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table cd_rel_assoc_tbl alter efft_vrsn_seq_id type integer;
--#!
alter table cd_rel_assoc_tbl alter obslt_vrsn_seq_id type integer;
--#!
alter table cd_vrsn_tbl alter vrsn_seq_id type integer;
--#!
alter table act_ptcpt_tbl alter ptcpt_seq_id type integer;
--#!
-- CURRENT CONCEPT VERSION VIEW
CREATE VIEW CD_CUR_VRSN_VW AS
	SELECT CD_VRSN_TBL.*, CD_TBL.IS_SYS, CD_CLS_TBL.MNEMONIC AS CLS_MNEMONIC 
	FROM CD_VRSN_TBL INNER JOIN CD_TBL USING (CD_ID)
		INNER JOIN CD_CLS_TBL USING (CLS_ID)
		WHERE CD_VRSN_TBL.OBSLT_UTC IS NULL;

-- CONCEPT SET MEMBERS VIEW
CREATE OR REPLACE VIEW CD_SET_MEM_VW AS
	SELECT CD_SET_TBL.SET_ID, CD_SET_TBL.MNEMONIC AS SET_MNEMONIC, CD_CUR_VRSN_VW.CD_ID, CD_CUR_VRSN_VW.CD_VRSN_ID, CD_CUR_VRSN_VW.MNEMONIC AS CD_MNEMONIC, CD_CUR_VRSN_VW.CLS_MNEMONIC
	FROM CD_SET_MEM_ASSOC_TBL INNER JOIN CD_SET_TBL USING (SET_ID) 
	INNER JOIN CD_CUR_VRSN_VW USING(CD_ID);


CREATE OR REPLACE FUNCTION vrfy_ent_rel (
	src_ent_id_in IN UUID,
	trg_ent_id_in IN UUID,
	rel_typ_cd_id_in IN UUID
) RETURNS VARCHAR(128)[] AS $$
DECLARE
	err_ref_out varchar(128)[];
BEGIN 
	IF NOT EXISTS (
		SELECT * 
		FROM 
			ent_rel_vrfy_cdtbl 
			INNER JOIN ent_tbl src_ent ON (src_ent.ent_id = src_ent_id_in)
			INNER JOIN ent_tbl trg_ent ON (trg_ent.ent_id = trg_ent_id_in)
		WHERE 
			rel_typ_cd_id = rel_typ_cd_id_in
			AND src_cls_cd_id = src_ent.cls_cd_id 
			AND trg_cls_cd_id = trg_ent.cls_cd_id
	) THEN
		SELECT DISTINCT 
			('{' || rel_cd.mnemonic || ',' || src_cd.mnemonic || ',' || trg_cd.mnemonic || '}')::VARCHAR[] INTO err_ref_out
		FROM 
			ent_tbl src_ent 
			CROSS JOIN ent_tbl trg_ent
			CROSS JOIN CD_VRSN_TBL REL_CD
			LEFT JOIN CD_VRSN_TBL SRC_CD ON (SRC_ENT.CLS_CD_ID = SRC_CD.CD_ID)
			LEFT JOIN CD_VRSN_TBL TRG_CD ON (TRG_ENT.CLS_CD_ID = TRG_CD.CD_ID)
		WHERE
			src_ent.ent_id = src_ent_id_in
			AND trg_ent.ent_id = trg_ent_id_in
			AND REL_CD.CD_ID = REL_TYP_CD_ID_in;
	END IF;
	RETURN ERR_REF_OUT;
END;
$$ LANGUAGE PLPGSQL;

-- TRIGGER - ENSURE THAT ANY VALUE INSERTED INTO THE ENT_REL_TBL HAS THE PROPER PARENT
CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl () RETURNS TRIGGER AS $$
DECLARE 
	err_ref varchar(128)[];
	
BEGIN
	SELECT * INTO err_ref FROM vrfy_ent_rel(new.src_ent_id, new.trg_ent_id, new.rel_typ_cd_id);
	IF err_ref[1] IS NULL OR err_ref[2] IS NULL OR err_ref[3] IS NULL THEN
		RETURN NEW; -- LET THE FK WORK
	ELSE 
		RAISE EXCEPTION 'Validation error: Relationship % [%] between % [%] > % [%] is invalid', NEW.rel_typ_cd_id, err_ref[1], NEW.src_ent_id, err_ref[2], NEW.trg_ent_id, err_ref[3]
			USING ERRCODE = 'O9001';
	END IF;
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

 -- GET THE SCHEMA VERSION
CREATE OR REPLACE FUNCTION GET_SCH_VRSN() RETURNS VARCHAR(10) AS
$$
BEGIN
	RETURN '1.9.0.0';
END;
$$ LANGUAGE plpgsql;

SELECT REG_PATCH('20181112-01');

COMMIT;