﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20210116-01" name="Update:20200105-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds support for peppered passwords</summary>
 *	<isInstalled>select ck_patch('20210116-01') from rdb$database</isInstalled>
 * </feature>
 */

ALTER TABLE PHON_VAL_TBL ALTER COLUMN VAL TYPE VARCHAR(256);--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('f3ef7e48-d8b7-4030-b431-aff7e0e1cb76','bacd9c6f-3fa9-481e-9636-37457962804d','ACAFE0F2-E209-43BB-8633-3665FD7C90BA', 'Patient==[Birthplace]==>Precinct');--#!
insert into cd_set_mem_assoc_tbl (set_id, cd_id) values ('4e6da567-0094-4f23-8555-11da499593af','ACAFE0F2-E209-43BB-8633-3665FD7C90BA');--#!
--#!
-- AUTHENTICATES THE USER IF APPLICABLE
CREATE PROCEDURE AUTH_USR_EX (
	USR_NAME_IN VARCHAR(64),
	PASSWD_PPR_IN VARCHAR(8190),
	MAX_FAIL_LOGIN_IN INT
) RETURNS (
    USR_ID UUID,
    CLS_ID UUID,
    USR_NAME VARCHAR(64),
    EMAIL VARCHAR(256),
    EMAIL_CNF BOOLEAN,
    PHN_NUM VARCHAR(128), 
    PHN_CNF BOOLEAN,
    TFA_ENABLED BOOLEAN,
    LOCKED TIMESTAMP, -- TRUE IF THE ACCOUNT HAS BEEN LOCKED
    PASSWD VARCHAR(128),
    SEC_STMP VARCHAR(128),
    FAIL_LOGIN INT,
    LAST_LOGIN_UTC TIMESTAMP,
    CRT_UTC TIMESTAMP,
    CRT_PROV_ID UUID, 
    OBSLT_UTC TIMESTAMP,
    OBSLT_PROV_ID UUID, 
    UPD_UTC TIMESTAMP,
    UPD_PROV_ID UUID, 
	PWD_EXP_UTC DATE,
	TFA_MECH UUID,
    ERR_CODE VARCHAR(128)
) AS
	DECLARE VARIABLE VAR_USR_ID UUID;
 	DECLARE VARIABLE VAR_USR_ERR VARCHAR(128);
	DECLARE VARIABLE VAR_USR_LOCK TIMESTAMP;
	DECLARE VARIABLE VAR_USR_PWD VARCHAR(128);
	DECLARE VARIABLE VAR_FAIL_LOGIN SMALLINT;
	DECLARE VARIABLE VAR_PWD_EXP DATE;
BEGIN
	

	SELECT USR_ID, PASSWD, FAIL_LOGIN, LOCKED, PWD_EXP_UTC
	FROM 
		SEC_USR_TBL
	WHERE 
		LOWER(SEC_USR_TBL.USR_NAME) = LOWER(:USR_NAME_IN)
		AND SEC_USR_TBL.OBSLT_UTC IS NULL
	INTO
		VAR_USR_ID, VAR_USR_PWD, VAR_FAIL_LOGIN, VAR_USR_LOCK, VAR_PWD_EXP;

	IF (IS_USR_LOCK(:USR_NAME_IN)) THEN BEGIN

		VAR_USR_LOCK = DATEADD(second, POWER((VAR_FAIL_LOGIN - :MAX_FAIL_LOGIN_IN), 1.5) * 30, COALESCE(VAR_USR_LOCK, CURRENT_TIMESTAMP));

		UPDATE SEC_USR_TBL SET FAIL_LOGIN = SEC_USR_TBL.FAIL_LOGIN + 1, LOCKED = :VAR_USR_LOCK
			WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;
		
		VAR_USR_ERR = 'AUTH_LCK:' || datediff(minute, CURRENT_TIMESTAMP, VAR_USR_LOCK) || ' minutes';
		--VAR_USR_ERR = 'AUTH_LCK:' || (VAR_USR_LOCK - CURRENT_TIMESTAMP);
	END
	ELSE BEGIN
		-- LOCKOUT ACCOUNTS CHECK
		IF(VAR_FAIL_LOGIN > :MAX_FAIL_LOGIN_IN) THEN BEGIN
			VAR_USR_LOCK = DATEADD(second, POWER((VAR_FAIL_LOGIN - :MAX_FAIL_LOGIN_IN), 1.5) * 30, COALESCE(VAR_USR_LOCK, CURRENT_TIMESTAMP));

			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1, LOCKED = :VAR_USR_LOCK
				WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;

			VAR_USR_ERR = 'AUTH_LCK:' || datediff(minute, CURRENT_TIMESTAMP, VAR_USR_LOCK) || ' minutes';
		END
		ELSE IF (CHAR_LENGTH(:PASSWD_PPR_IN) > 0 AND POSITION(VAR_USR_PWD || ';' in :PASSWD_PPR_IN || ';') != 0) THEN BEGIN
			UPDATE SEC_USR_TBL SET 
				FAIL_LOGIN = 0,
				LAST_LOGIN_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'),
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE USR_ID = :VAR_USR_ID;
			
			VAR_USR_ERR = NULL;
		END
		ELSE BEGIN
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1 WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;
			VAR_USR_ERR = 'AUTH_INV:' || USR_NAME_IN;
		END
	END

	FOR SELECT USR_ID, CLS_ID, USR_NAME, EMAIL, EMAIL_CNF, PHN_NUM, 
				PHN_CNF, TFA_ENABLED, LOCKED, PASSWD, SEC_STMP, FAIL_LOGIN, 
				LAST_LOGIN_UTC, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID,
				UPD_UTC, UPD_PROV_ID, PWD_EXP_UTC, TFA_MECH, :VAR_USR_ERR
			FROM SEC_USR_TBL
			WHERE
				USR_ID = :VAR_USR_ID
			INTO 
				:USR_ID, :CLS_ID, :USR_NAME, :EMAIL, :EMAIL_CNF, :PHN_NUM, 
				:PHN_CNF, :TFA_ENABLED, :LOCKED, :PASSWD, :SEC_STMP, :FAIL_LOGIN, 
				:LAST_LOGIN_UTC, :CRT_UTC, :CRT_PROV_ID, :OBSLT_UTC, :OBSLT_PROV_ID,
				:UPD_UTC, :UPD_PROV_ID, :PWD_EXP_UTC, :TFA_MECH, :ERR_CODE
	DO BEGIN
		SUSPEND;
	END
END;
--#!
SELECT REG_PATCH('20210116-01') FROM RDB$DATABASE;
