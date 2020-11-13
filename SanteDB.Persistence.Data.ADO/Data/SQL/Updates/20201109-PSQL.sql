﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20201109-01" name="Update:20201109-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Allow Impersonation</summary>
 *	<remarks>Adds policies which control impersination</remarks>
 *	<isInstalled>select ck_patch('20201109-01')</isInstalled>
 * </feature>
 */
BEGIN TRANSACTION;

DROP INDEX public.cd_ref_term_cs_mnemonic_uq_idx;

CREATE UNIQUE INDEX cd_ref_term_cs_mnemonic_uq_idx
  ON public.ref_term_tbl
  (cs_id, mnemonic)
  WHERE (obslt_utc IS NULL);

INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) 
SELECT '9bbe0cfe-faab-4dc9-a28f-c001e3e95e6e', src_cls_cd_id, trg_cls_cd_id, REPLACE (err_desc, '[Birthplace]','[PlaceOfDeath]') FROM ENT_REL_VRFY_CDTBL WHERE rel_typ_Cd_id = 'f3ef7e48-d8b7-4030-b431-aff7e0e1cb76'


-- AUTHENTICATES THE USER IF APPLICABLE
CREATE OR REPLACE FUNCTION AUTH_USR (
	USR_NAME_IN IN TEXT,
	PASSWD_IN IN TEXT,
	MAX_FAIL_LOGIN_IN IN INT
) RETURNS TABLE (
    USR_ID UUID,
    CLS_ID UUID,
    USR_NAME VARCHAR(64),
    EMAIL VARCHAR(256),
    EMAIL_CNF BOOLEAN,
    PHN_NUM VARCHAR(128), 
    PHN_CNF BOOLEAN,
    TFA_ENABLED BOOLEAN,
    LOCKED TIMESTAMPTZ, -- TRUE IF THE ACCOUNT HAS BEEN LOCKED
    PASSWD VARCHAR(128),
    SEC_STMP VARCHAR(128),
    FAIL_LOGIN INT,
    LAST_LOGIN_UTC TIMESTAMPTZ,
    CRT_UTC TIMESTAMPTZ,
    CRT_PROV_ID UUID, 
    OBSLT_UTC TIMESTAMPTZ,
    OBSLT_PROV_ID UUID, 
    UPD_UTC TIMESTAMPTZ,
    UPD_PROV_ID UUID, 
	PWD_EXP_UTC DATE, 
	TFA_MECH UUID,
    ERR_CODE VARCHAR(128)
) AS $$
DECLARE
	USR_TPL SEC_USR_TBL;
BEGIN
	SELECT INTO USR_TPL * 
		FROM SEC_USR_TBL
		WHERE LOWER(SEC_USR_TBL.USR_NAME) = LOWER(USR_NAME_IN)
		AND SEC_USR_TBL.OBSLT_UTC IS NULL;

	IF (IS_USR_LOCK(USR_NAME_IN)) THEN
		USR_TPL.LOCKED = COALESCE(USR_TPL.LOCKED, CURRENT_TIMESTAMP) + ((USR_TPL.FAIL_LOGIN - MAX_FAIL_LOGIN_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
		UPDATE SEC_USR_TBL SET FAIL_LOGIN = SEC_USR_TBL.FAIL_LOGIN + 1, LOCKED = USR_TPL.LOCKED
			WHERE SEC_USR_TBL.USR_NAME = USR_NAME_IN;
		RETURN QUERY SELECT USR_TPL.*, ('AUTH_LCK:' || ((USR_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
	ELSE
		
		-- LOCKOUT ACCOUNTS
		IF(USR_TPL.FAIL_LOGIN > MAX_FAIL_LOGIN_IN) THEN 
			USR_TPL.LOCKED = COALESCE(USR_TPL.LOCKED, CURRENT_TIMESTAMP) + ((USR_TPL.FAIL_LOGIN - MAX_FAIL_LOGIN_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1, LOCKED = USR_TPL.LOCKED
				WHERE SEC_USR_TBL.USR_NAME = USR_NAME_IN;
			RETURN QUERY SELECT USR_TPL.*, ('AUTH_LCK:' || ((USR_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
		ELSIF (USR_TPL.PASSWD = PASSWD_IN) THEN
			UPDATE SEC_USR_TBL SET 
				FAIL_LOGIN = 0,
				LAST_LOGIN_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8',
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE LOWER(SEC_USR_TBL.USR_NAME) = LOWER(USR_NAME_IN);
			RETURN QUERY SELECT USR_TPL.*, NULL::VARCHAR LIMIT 1;
		ELSE
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1 WHERE SEC_USR_TBL.USR_NAME = USR_NAME_IN;
			RETURN QUERY SELECT USR_TPL.*, ('AUTH_INV:' || USR_NAME_IN)::VARCHAR;
		END IF;
	END IF;
END	
$$ LANGUAGE PLPGSQL;

SELECT REG_PATCH('20201109-01');

COMMIT;
