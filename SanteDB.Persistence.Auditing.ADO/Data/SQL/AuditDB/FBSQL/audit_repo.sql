/** 
 * <feature scope="SanteDB.Persistence.Audit.ADO" id="00010000-00" name="Initialize:001-01" invariantName="FirebirdSQL">
 *	<summary>Installs the core schema for SanteDB Audit Repository</summary>
 *	<remarks>This script installs the necessary core schema files for SanteDB</remarks>
 *  <isInstalled mustSucceed="true">select true from rdb$database where exists (select 1 from rdb$relations where rdb$relation_name = 'AUD_CD_TBL');</isInstalled>
 * </feature>
 */
 
-- CREATE DOMAIN FOR BOOLEAN
-- MIGRATE:OPTIONAL
CREATE DOMAIN UUID AS CHAR(16);
--#!
CREATE SEQUENCE AUD_META_SEQ START WITH 1 INCREMENT BY 1;--#!
CREATE SEQUENCE AUD_ACT_SEQ START WITH 1 INCREMENT BY 1;--#!
CREATE SEQUENCE AUD_ACT_ASSOC_SEQ START WITH 1 INCREMENT BY 1;--#!
CREATE SEQUENCE AUD_OBJ_SEQ START WITH 1 INCREMENT BY 1;--#!
CREATE SEQUENCE AUD_OBJ_DAT_SEQ START WITH 1 INCREMENT BY 1;--#!
CREATE SEQUENCE AUD_META_VAL_SEQ START WITH 1 INCREMENT BY 1;--#!
 -- TABLE FOR STORAGE OF AUDIT CODES
 CREATE TABLE aud_cd_tbl (
	id UUID NOT NULL, -- UNIQUE IDENTIIFER OF THE CODE
	mnemonic VARCHAR(256) NOT NULL, -- THE MNEMONIC OF THE CODE
	cd_sys VARCHAR(256), -- THE CODIFICATION SYSTEM OF THE CODE IF KNOWN
	CONSTRAINT pk_aud_cd_tbl PRIMARY KEY (id)
 );
 --#!
 CREATE UNIQUE INDEX aud_cd_mnemonic_cd_sys_idx ON aud_cd_tbl(mnemonic, cd_sys);
 --#!
 -- TABLE FOR STORAGE OF AUDITS
 CREATE TABLE aud_tbl (
	id UUID NOT NULL, -- UNIQUE IDENTIIFER FOR THE AUDIT
	outc_cs INT NOT NULL, -- THE OUTCOME OF THE AUDIT
	act_cs INT NOT NULL, -- THE ACTION CODE OF THE AUDIT
	typ_cs INT NOT NULL, -- THE AUDIT TYPE CODE
	evt_utc TIMESTAMP NOT NULL, -- THE TIME THAT THE AUDIT OCCURRED
	crt_utc TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL , -- THE TIME THAT THE AUDIT WAS CREATED
	cls_cd_id UUID, -- THE CLASSIFICATION CODE
	CONSTRAINT pk_aud_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_cls_cd_id FOREIGN KEY (cls_cd_id) REFERENCES aud_cd_tbl(id)
 );
 --#!
 -- TABLE FOR STORAGE OF AUDIT OBJECTS
 CREATE TABLE aud_obj_tbl (
	id BIGINT NOT NULL, -- UNIQUE IDENTIFIER FOR THE AUDIT OBJECT
	aud_id UUID NOT NULL, -- AUDIT TO WHICH THE OBJECT BELONGS
	obj_id VARCHAR(256), -- THE OBJECT IDENTIFIER
	obj_typ INT, -- THE TYPE OF OBJECT
	rol_cs INT, -- THE ROLE THE OBJECT PLAYS IN THE AUDIT
	lcycl_cs INT, -- THE LIFECYCLE OF THE OBJECT
	id_typ_cs INT, -- THE IDENTIFIER TYPE CODE
	cst_id_typ uuid,
	qry_dat BLOB SUB_TYPE TEXT, -- ADDITIONAL QUERY DATA ASSIGNED TO THE OBJECT
	nam_dat BLOB SUB_TYPE TEXT, -- ADDITIONAL NAME DATA ASSIGNED TO THE OBJECT
	CONSTRAINT pk_aud_obj_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_obj_aud_tbl FOREIGN KEY (aud_id) REFERENCES aud_tbl(id),
	CONSTRAINT fk_aud_obj_cst_id foreign key (cst_id_typ) references aud_cd_tbl(id)
);
--#!
CREATE TRIGGER aud_obj_tbl_seq FOR aud_obj_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR AUD_OBJ_SEQ;
END;
--#!
CREATE INDEX aud_obj_obj_id_idx ON aud_obj_tbl(obj_id);
--#!
CREATE INDEX aud_obj_aud_id_idx ON aud_obj_tbl(aud_id);
--#!

--#!
 -- TABLE FOR STORAGE OF AUDIT OBJECT EXTENDED DATA
 CREATE TABLE aud_obj_dat_tbl (
	id BIGINT NOT NULL, -- UNIQUE IDENTIFIER FOR THE AUDIT OBJECT
	obj_id BIGINT NOT NULL, -- AUDIT TO WHICH THE OBJECT BELONGS
	key VARCHAR(256), -- THE KEY IDENTIFIER
	val BLOB, -- ADDITIONAL QUERY DATA ASSIGNED TO THE OBJECT
	CONSTRAINT pk_aud_obj_dat_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_obj_dat_obj_tbl FOREIGN KEY (obj_id) REFERENCES aud_obj_tbl(id)
);
--#!
CREATE TRIGGER aud_obj_dat_tbl_seq FOR aud_obj_dat_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR AUD_OBJ_DAT_SEQ;
END;
--#!
CREATE INDEX aud_obj_dat_id_idx ON aud_obj_dat_tbl(obj_id);
--#!

-- TABLE FOR AUDIT ACTORS
CREATE TABLE aud_act_tbl (
	id BIGINT NOT NULL, -- UNIQUE IDENTIFIER FOR THE AUDIT ACTOR ENTRY
	usr_id VARCHAR(256), -- USER IDENTIFIER AS KNOWN BY THE SYSTEM
	usr_name VARCHAR(256), -- USER NAME AS KNOWN BY THE SYSTEM
	rol_cd_id UUID, -- THE ROLE CODE OF THE ACTOR
	CONSTRAINT pk_aud_act_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_act_rol_cd_id FOREIGN KEY (rol_cd_id) REFERENCES aud_cd_tbl(id)
);
--#!
CREATE TRIGGER aud_act_tbl_seq FOR aud_act_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR AUD_ACT_SEQ;
END;
--#!
-- ASSOCIATION TABLE BETWEEN AUDITS AND ACTORS
CREATE TABLE aud_act_assoc_tbl (
	id BIGINT NOT NULL, -- UNIQUE IDENTIFIER FOR THE AUDIT ACTOR
	aud_id UUID NOT NULL, -- THE AUDIT TO WHICH THE ACTOR ENTRY BELONGS
	act_id BIGINT NOT NULL, -- THE ACTOR TO WHICH THE ASOSCIATION BELONGS
	is_rqo BOOLEAN DEFAULT FALSE NOT NULL, -- TRUE IF THE USER IS THE REQUESTOR OF THE ACTION
	ap VARCHAR(256), -- Access point
	CONSTRAINT pk_aud_act_assoc_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_act_assoc_act_id FOREIGN KEY (act_id) REFERENCES aud_act_tbl (id),
	CONSTRAINT fk_aud_act_assoc_aud_id FOREIGN KEY (aud_id) REFERENCES aud_tbl (id)
);--#!
CREATE TRIGGER aud_act_assoc_tbl_seq FOR aud_act_assoc_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR AUD_ACT_ASSOC_SEQ;
END;
--#!
CREATE TABLE aud_meta_val_cdtbl (
	VAL_ID BIGINT NOT NULL,
	VAL VARCHAR(256) NOT NULL,
	CONSTRAINT PK_AUD_META_VAL_CDTBL PRIMARY KEY (VAL_ID)
);
--#!
CREATE TRIGGER aud_meta_val_cdtbl_seq FOR aud_meta_val_cdtbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.val_id = NEXT VALUE FOR AUD_META_VAL_SEQ;
END;
--#!
-- METADATA TABLE
CREATE TABLE aud_meta_tbl (
	id BIGINT NOT NULL,
	aud_id UUID NOT NULL,
	attr INT NOT NULL,
	val_id BIGINT NOT NULL,
	CONSTRAINT pk_aud_meta_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_meta_aud_id FOREIGN KEY (aud_id) REFERENCES aud_tbl(id),
	CONSTRAINT fk_aud_meta_val_id FOREIGN KEY (val_id) REFERENCES aud_meta_val_cdtbl (val_id)
);--#!
CREATE TRIGGER aud_meta_tbl_seq FOR aud_meta_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR aud_meta_seq;
END;
--#!
CREATE INDEX aud_meta_aud_id_idx ON aud_meta_tbl(aud_id);
--#!
CREATE INDEX aud_act_assoc_aud_id_idx ON aud_act_assoc_tbl(aud_id);
--#!