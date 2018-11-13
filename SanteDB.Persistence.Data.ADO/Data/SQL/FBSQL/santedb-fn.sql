﻿-- RETURNS WHETHER THE USER ACCOUNT IS LOCKED
SET TERM !! ;

CREATE FUNCTION IS_USR_LOCK(
	USR_NAME_IN VARCHAR(64)
) RETURNS BOOLEAN AS 
BEGIN
	RETURN (SELECT (LOCKED > CURRENT_TIMESTAMP) FROM SEC_USR_TBL WHERE USR_NAME = :USR_NAME_IN);
END;

-- AUTHENTICATES THE USER IF APPLICABLE
CREATE PROCEDURE AUTH_USR (
	USR_NAME_IN VARCHAR(64),
	PASSWD_IN VARCHAR(128),
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
    ERR_CODE VARCHAR(128)
) AS
	DECLARE VARIABLE VAR_USR_ID UUID;
 	DECLARE VARIABLE VAR_USR_ERR VARCHAR(128);
	DECLARE VARIABLE VAR_USR_LOCK TIMESTAMP;
	DECLARE VARIABLE VAR_USR_PWD VARCHAR(128);
	DECLARE VARIABLE VAR_FAIL_LOGIN SMALLINT;
	DECLARE VARIABLE VAR_TFA_ENABLED BOOLEAN;
BEGIN
	

	SELECT USR_ID, PASSWD, FAIL_LOGIN, LOCKED, TFA_ENABLED
	FROM 
		SEC_USR_TBL
	WHERE 
		LOWER(SEC_USR_TBL.USR_NAME) = LOWER(:USR_NAME_IN)
		AND SEC_USR_TBL.OBSLT_UTC IS NULL
	INTO
		VAR_USR_ID, VAR_USR_PWD, VAR_FAIL_LOGIN, VAR_USR_LOCK, VAR_TFA_ENABLED;

	IF (IS_USR_LOCK(:USR_NAME_IN)) THEN BEGIN

		VAR_USR_LOCK = DATEADD(second, POWER((VAR_FAIL_LOGIN - :MAX_FAIL_LOGIN_IN), 1.5) * 30, COALESCE(VAR_USR_LOCK, CURRENT_TIMESTAMP));

		UPDATE SEC_USR_TBL SET FAIL_LOGIN = SEC_USR_TBL.FAIL_LOGIN + 1, LOCKED = :VAR_USR_LOCK
			WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;
		
		VAR_USR_ERR = 'AUTH_LCK:' || datediff(minute, CURRENT_TIMESTAMP, VAR_USR_LOCK) || ' minutes';
		--VAR_USR_ERR = 'AUTH_LCK:' || (VAR_USR_LOCK - CURRENT_TIMESTAMP);
	END
	ELSE BEGIN
		-- LOCKOUT ACCOUNTS
		IF (VAR_USR_PWD = :PASSWD_IN) THEN BEGIN
			UPDATE SEC_USR_TBL SET 
				FAIL_LOGIN = 0,
				LAST_LOGIN_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'),
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE USR_ID = :VAR_USR_ID;
			
			VAR_USR_ERR = NULL;
		END
		ELSE IF(VAR_FAIL_LOGIN > :MAX_FAIL_LOGIN_IN) THEN BEGIN
			VAR_USR_LOCK = DATEADD(second, POWER((VAR_FAIL_LOGIN - :MAX_FAIL_LOGIN_IN), 1.5) * 30, COALESCE(VAR_USR_LOCK, CURRENT_TIMESTAMP));

			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1, LOCKED = :VAR_USR_LOCK
				WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;

			VAR_USR_ERR = 'AUTH_LCK:' || datediff(minute, CURRENT_TIMESTAMP, VAR_USR_LOCK) || ' minutes';
		END
		ELSE IF (VAR_TFA_ENABLED = TRUE) THEN BEGIN
			
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1 WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;
			VAR_USR_ERR = 'AUTH_TFA:' || USR_NAME_IN;
		END
		ELSE BEGIN
			UPDATE SEC_USR_TBL SET FAIL_LOGIN = COALESCE(SEC_USR_TBL.FAIL_LOGIN, 0) + 1 WHERE SEC_USR_TBL.USR_ID = :VAR_USR_ID;
			VAR_USR_ERR = 'AUTH_INV:' || USR_NAME_IN;
		END
	END

	FOR SELECT USR_ID, CLS_ID, USR_NAME, EMAIL, EMAIL_CNF, PHN_NUM, 
				PHN_CNF, TFA_ENABLED, LOCKED, PASSWD, SEC_STMP, FAIL_LOGIN, 
				LAST_LOGIN_UTC, CRT_UTC, CRT_PROV_ID, OBSLT_UTC, OBSLT_PROV_ID,
				UPD_UTC, UPD_PROV_ID, :VAR_USR_ERR
			FROM SEC_USR_TBL
			WHERE
				USR_ID = :VAR_USR_ID
			INTO 
				:USR_ID, :CLS_ID, :USR_NAME, :EMAIL, :EMAIL_CNF, :PHN_NUM, 
				:PHN_CNF, :TFA_ENABLED, :LOCKED, :PASSWD, :SEC_STMP, :FAIL_LOGIN, 
				:LAST_LOGIN_UTC, :CRT_UTC, :CRT_PROV_ID, :OBSLT_UTC, :OBSLT_PROV_ID,
				:UPD_UTC, :UPD_PROV_ID, :ERR_CODE
	DO BEGIN
		SUSPEND;
	END
END	!!

-- AUTHENTICATE AN APPICATION
CREATE PROCEDURE AUTH_APP (
	APP_PUB_ID_IN VARCHAR(64),
	APP_SCRT_IN VARCHAR(64),
	MAX_FAIL_AUTH_IN INTEGER
) RETURNS 
(
	APP_ID UUID, -- UNIQUE IDENTIIFER FOR THE DEV
	APP_PUB_ID VARCHAR(64), -- THE PUBLIC IDENTIFIER FOR THE APP
	APP_SCRT VARCHAR(64), -- THE APPLICATION SECRET
	LOCKED TIMESTAMP, -- LOCKOUT PERIOD
	FAIL_AUTH INTEGER, -- FAILED AUTHENTICATION ATTEMPTS
	LAST_AUTH_UTC TIMESTAMP, -- THE LAST AUTHETNICATION TIME
	CRT_UTC TIMESTAMP, -- THE CREATION TIME OF THE APP
	CRT_PROV_ID UUID, -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
	UPD_UTC TIMESTAMP, -- THE CREATION TIME OF THE APP
	UPD_PROV_ID UUID, -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
	OBSLT_UTC TIMESTAMP, -- OBSOLETION TIME
	OBSLT_PROV_ID UUID, -- THE OBSOLETION USER
	RPLC_APP_ID UUID -- THE APPLICATION WICH THIS APPLICATION REPLACES
) AS 
BEGIN
	FOR SELECT * 
		FROM SEC_APP_TBL 
		WHERE 
			APP_PUB_ID = :APP_PUB_ID_IN 
			AND APP_SCRT = :APP_SCRT_IN
			AND (LOCKED IS NULL OR LOCKED < CURRENT_TIMESTAMP)
			AND (FAIL_AUTH IS NULL OR FAIL_AUTH < :MAX_FAIL_AUTH_IN)
		FETCH FIRST 1 ROW ONLY
		INTO :APP_ID, :APP_PUB_ID, :APP_SCRT, :LOCKED, :FAIL_AUTH, :LAST_AUTH_UTC, :CRT_UTC,
			:CRT_PROV_ID, :UPD_UTC, :UPD_PROV_ID, :OBSLT_UTC, :OBSLT_PROV_ID, :RPLC_APP_ID
	DO BEGIN
		SUSPEND;
	END
END!!

-- AUTHENTICATE A DEVICE
CREATE PROCEDURE AUTH_DEV (
	DEV_PUB_ID_IN VARCHAR(64),
	DEV_SCRT_IN VARCHAR(64),
	MAX_FAIL_AUTH_IN INTEGER
) RETURNS (
	DEV_ID UUID, -- UNIQUE IDENTIFIER FOR THE DEVICE
	DEV_SCRT VARCHAR(64), -- THE SECRET OF THE DEVICE (EX: X509 THUMB)
	DEV_PUB_ID VARCHAR(64), -- THE PUBLIC IDENTIIFER OF THE DEVICE
	LOCKED TIMESTAMP, -- LOCKOUT PERIOD
	FAIL_AUTH INTEGER, -- FAILED AUTHENTICATION ATTEMPTS
	LAST_AUTH_UTC TIMESTAMP, -- THE LAST AUTHETNICATION TIME
	CRT_UTC TIMESTAMP, -- THE DATE THE DEVICE WAS CREATED
	CRT_PROV_ID UUID, -- THE USER WHICH CREATED TEH DEVICE
	UPD_UTC TIMESTAMP, 
	UPD_PROV_ID UUID, 
	OBSLT_UTC TIMESTAMP, -- THE TIME THAT THE DEVICE WAS OBSOLETED
	OBSLT_PROV_ID UUID, -- THE USER WHICH OBSOLETD THE DEVICE
	RPLC_DEV_ID UUID -- THE DEVICE THAT THIS DEVICE REPLACES.

) AS 
BEGIN
	FOR SELECT * 
		FROM SEC_DEV_TBL 
		WHERE 
			DEV_PUB_ID = :DEV_PUB_ID_IN 
			AND DEV_SCRT = :DEV_SCRT_IN
			AND (LOCKED IS NULL OR LOCKED < CURRENT_TIMESTAMP)
			AND (FAIL_AUTH IS NULL OR FAIL_AUTH < :MAX_FAIL_AUTH_IN)
		FETCH FIRST 1 ROW ONLY
		INTO :DEV_ID, :DEV_SCRT, :DEV_PUB_ID, :LOCKED, :FAIL_AUTH, :LAST_AUTH_UTC, :CRT_UTC,
			:CRT_PROV_ID, :UPD_UTC, :UPD_PROV_ID, :OBSLT_UTC, :OBSLT_PROV_ID, :RPLC_DEV_ID
	DO BEGIN
		SUSPEND;
	END
END!!

SET TERM ; !!