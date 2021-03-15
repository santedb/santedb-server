﻿/** 
 * <feature scope="SanteDB.Persistence.PubSub.ADO" id="20210311-01" name="Update:20210311-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Installs the Pub/Sub ADO Tables</summary>
 *	<remarks>This table is used to register channels and subscriptions</remarks>
 *	<canInstall>select true from rdb$database where not exists (select 1 from rdb$relations where rdb$relation_name = 'SUB_TBL');</canInstall>
 *  <isInstalled mustSucceed="true">select true from rdb$database where exists (select 1 from rdb$relations where rdb$relation_name = 'SUB_TBL');</isInstalled>
 * </feature>
 */
-- SUBSCRIPTION CHANNEL
CREATE TABLE SUB_CHNL_TBL (
	CHNL_ID UUID NOT NULL,
	CRT_UTC TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL ,
	CRT_USR_ID UUID NOT NULL,
	UPD_UTC TIMESTAMP,
	UPD_USR_ID UUID,
	OBSLT_UTC TIMESTAMP,
	OBSLT_USR_ID UUID,
	IS_ACT BOOLEAN DEFAULT FALSE NOT NULL ,
	NAME VARCHAR(64) NOT NULL,
	URI VARCHAR(512) NOT NULL,
	DSPTCHR_CLS VARCHAR(512) NOT NULL, 
	CONSTRAINT PK_SUB_CHNL_TBL PRIMARY KEY (CHNL_ID)
);--#!

CREATE INDEX SUB_CHNL_IDX ON SUB_CHNL_TBL(NAME);
--#!
CREATE SEQUENCE SUB_CHNL_SET_SEQ;
--#!
-- CHANNEL SETTINGS
CREATE TABLE SUB_CHNL_SET_TBL (
	SET_ID INTEGER NOT NULL,
	CHNL_ID UUID NOT NULL,
	NAME VARCHAR(64) NOT NULL,
	VAL VARCHAR(512) NOT NULL,
	CONSTRAINT PK_SUB_CHNL_SET_TBL PRIMARY KEY (SET_ID),
	CONSTRAINT FK_SUB_CHNL_CHNL_TBL FOREIGN KEY (CHNL_ID) REFERENCES SUB_CHNL_TBL (CHNL_ID)
);
--#!

CREATE TRIGGER TG_SUB_CHNL_SET_SEQ FOR SUB_CHNL_SET_TBL ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.SET_ID = NEXT VALUE FOR SUB_CHNL_SET_SEQ;
END;
--#!
CREATE INDEX SUB_CHNL_SET_CHNL_IDX ON SUB_CHNL_SET_TBL(CHNL_ID);
--#!
CREATE TABLE SUB_TBL (
	SUB_ID UUID NOT NULL,
	NAME VARCHAR(64) NOT NULL,
	CRT_UTC TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL ,
	CRT_USR_ID UUID NOT NULL,
	UPD_UTC TIMESTAMP,
	UPD_USR_ID UUID,
	OBSLT_UTC TIMESTAMP,
	OBSLT_USR_ID UUID,
	IS_ACT BOOLEAN DEFAULT FALSE NOT NULL ,
	NOTE VARCHAR(1024), 
	SPPRT VARCHAR(256),
	NBF TIMESTAMP,
	NAF TIMESTAMP,
	RSRC_CLS VARCHAR(512) NOT NULL, 
	EVT_ID INTEGER NOT NULL,
	CHNL_ID UUID NOT NULL,
	CONSTRAINT PK_SUB_TBL PRIMARY KEY (SUB_ID),
	CONSTRAINT FK_SUB_CHNL_TBL FOREIGN KEY (CHNL_ID) REFERENCES SUB_CHNL_TBL(CHNL_ID)
);
--#!
CREATE UNIQUE INDEX SUB_NAME_IDX ON SUB_TBL(NAME);
--#!
CREATE SEQUENCE SUB_FLT_SEQ;
--#!
CREATE TABLE SUB_FLT_TBL (
	FLT_ID INTEGER NOT NULL,
	SUB_ID UUID NOT NULL,
	FLT VARCHAR(1024) NOT NULL,
	CONSTRAINT PK_SUB_FLT_TBL PRIMARY KEY (FLT_ID),
	CONSTRAINT FK_SUB_FLT_SUB_TBL FOREIGN KEY (SUB_ID) REFERENCES SUB_TBL (SUB_ID)
);
--#!
CREATE TRIGGER TG_SUB_FLT_SEQ FOR SUB_FLT_TBL ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.FLT_ID = NEXT VALUE FOR SUB_FLT_SEQ;
END;
--#!