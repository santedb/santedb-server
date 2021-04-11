/** 
 * <feature scope="SanteDB.Persistence.Data" id="20200522-01" name="Update:20200522-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add relationship types for MPI Role</summary>
 *	<remarks>This adds various relationship types MPI MDM roles</remarks>
 *	<isInstalled>select ck_patch('20200522-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

-- PATIENTS CAN HAVE FAMILY MEMBERS WHICH ARE MDM CONTROLLED
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc)
SELECT 'bacd9c6f-3fa9-481e-9636-37457962804d', CD_SET_MEM_ASSOC_TBL.CD_ID, '49328452-7e30-4dcd-94cd-fd532d111578', 'ALLOW MDM TO BE TARGET FAMILY MEMBER' FROM CD_SET_MEM_ASSOC_TBL INNER JOIN
CD_SET_TBL USING(SET_ID)
INNER JOIN CD_TBL ON (CD_TBL.CD_ID = '49328452-7e30-4dcd-94cd-fd532d111578')
WHERE MNEMONIC = 'FamilyMember';

-- ALLOW PATIENTS TO BE FAMILY MEMBERS OF PATIENTS
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc)
SELECT 'bacd9c6f-3fa9-481e-9636-37457962804d', CD_SET_MEM_ASSOC_TBL.CD_ID, 'bacd9c6f-3fa9-481e-9636-37457962804d', 'ALLOW PATIENT TO BE FAMILY MEMBER OF OTHER PATIENTS' FROM CD_SET_MEM_ASSOC_TBL INNER JOIN
CD_SET_TBL USING(SET_ID)
INNER JOIN CD_TBL ON (CD_TBL.CD_ID = '49328452-7e30-4dcd-94cd-fd532d111578')
WHERE MNEMONIC = 'FamilyMember'
AND NOT EXISTS (SELECT 1 FROM ENT_REL_VRFY_CDTBL WHERE src_cls_cD_id = 'bacd9c6f-3fa9-481e-9636-37457962804d' and rel_typ_cd_id = CD_SET_MEM_ASSOC_TBL.CD_ID and trg_cls_cd_id = 'bacd9c6f-3fa9-481e-9636-37457962804d');


INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('8ff9d9a5-a206-4566-82cd-67b770d7ce8a', 'bacd9c6f-3fa9-481e-9636-37457962804d','7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'COVERAGE SPONSOR');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('0c157566-d1e9-4976-8542-473caa9ba2a4', 'bacd9c6f-3fa9-481e-9636-37457962804d','7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'STUDENT');


CREATE OR REPLACE FUNCTION public.auth_usr(
    IN usr_name_in text,
    IN passwd_in text,
    IN max_fail_login_in integer)
  RETURNS TABLE(usr_id uuid, cls_id uuid, usr_name character varying, email character varying, email_cnf boolean, phn_num character varying, phn_cnf boolean, tfa_enabled boolean, locked timestamp with time zone, passwd character varying, sec_stmp character varying, fail_login integer, last_login_utc timestamp with time zone, crt_utc timestamp with time zone, crt_prov_id uuid, obslt_utc timestamp with time zone, obslt_prov_id uuid, upd_utc timestamp with time zone, upd_prov_id uuid, pwd_exp_utc date, tfa_mech uuid, err_code character varying) AS
$BODY$
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
				WHERE LOWER(SEC_USR_TBL.USR_NAME) = LOWER(USR_NAME_IN);
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
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1 WHERE LOWER(SEC_USR_TBL.USR_NAME) = LOWER(USR_NAME_IN);
			RETURN QUERY SELECT USR_TPL.*, ('AUTH_INV:' || USR_NAME_IN)::VARCHAR;
		END IF;
	END IF;
END	
$BODY$
  LANGUAGE plpgsql ;


  
CREATE OR REPLACE FUNCTION is_ent_cls(
    ent_id_in uuid,
    cls_mnemonic_in character varying)
  RETURNS boolean AS
$BODY$
BEGIN
RETURN EXISTS (SELECT 1 FROM ENT_TBL INNER JOIN CD_VRSN_TBL ON (ENT_TBL.CLS_CD_ID = CD_VRSN_TBL.CD_ID) WHERE ENT_ID = ENT_ID_IN AND CD_VRSN_TBL.MNEMONIC = CLS_MNEMONIC_IN AND CD_VRSN_TBL.OBSLT_UTC IS NULL);
END
$BODY$
  LANGUAGE plpgsql ;

SELECT REG_PATCH('20200522-01');
COMMIT;