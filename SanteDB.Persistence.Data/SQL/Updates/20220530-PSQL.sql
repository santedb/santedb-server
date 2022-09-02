﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220530-01" name="Update:20220530-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds certificate authentication provider tables</summary>
 *	<isInstalled>select ck_patch('20220530-01')</isInstalled>
 * </feature>
 */

 -- CERTIFICATE AUTHENTICAITON INFORMATION
 CREATE TABLE SEC_CER_TBL (
	CER_ID UUID NOT NULL DEFAULT GENERATE_UUID_V1(), 
	X509_THB VARCHAR(32) NOT NULL, -- THE X509 CERTIFICATE THUMBPRINT IDENTIFIER
	X509_PK BYTEA NOT NULL, -- X509 PUBLIC KEY CERTIFICATE DATA
	EXP_UTC TIMESTAMPTZ NOT NULL, -- THE EXPIRATION OF THE CERTIFICATE (HELPS WITH DATABASE CALLS)
	CRT_UTC TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CRT_PROV_ID UUID,
	OBSLT_UTC TIMESTAMPTZ,
	OBSLT_PROV_ID UUID,
	UPD_UTC TIMESTAMPTZ,
	UPD_PROV_ID UUID,
	USR_ID UUID,
	APP_ID UUID,
	DEV_ID UUID,
	CONSTRAINT PK_SEC_CER_AUTH_TBL PRIMARY KEY (CER_ID),
	CONSTRAINT FK_SEC_CER_CRT_PROV FOREIGN KEY (CRT_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID),
	CONSTRAINT FK_SEC_CER_UPD_PROV FOREIGN KEY (UPD_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID),
	CONSTRAINT FK_SEC_CER_OBSLT_PROV FOREIGN KEY (OBSLT_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID),
	CONSTRAINT FK_SEC_CER_USR_ID FOREIGN KEY (USR_ID) REFERENCES SEC_USR_TBL(USR_ID),
	CONSTRAINT FK_SEC_CER_APP_ID FOREIGN KEY (APP_ID) REFERENCES SEC_APP_TBL(APP_ID),
	CONSTRAINT FK_SEC_CER_DEV_ID FOREIGN KEY (DEV_ID) REFERENCES SEC_DEV_TBL(DEV_ID),
	CONSTRAINT FK_SEC_CER_MAP CHECK (
		USR_ID IS NOT NULL AND APP_ID IS NULL AND DEV_ID IS NULL OR 
		USR_ID IS NULL AND APP_ID IS NOT NULL AND DEV_ID IS NULL OR
		USR_ID IS NULL AND APP_ID IS NULL AND DEV_ID IS NOT NULL
	)
 )--#!

 CREATE UNIQUE INDEX SEC_CER_X509_THB_IDX ON SEC_CER_TBL(X509_THB) WHERE (OBSLT_UTC IS NULL);--#!

 
alter index asgn_aut_name_idx rename to id_dmn_name_idx;
alter index asgn_aut_oid_idx rename to id_dmn_oid_idx;
alter index pk_asgn_aut_tbl rename to pk_id_dmn_tbl;

alter table asgn_aut_tbl rename to id_dmn_tbl;
alter table id_dmn_tbl drop constraint fk_asgn_aut_crt_prov_id;
alter table id_dmn_tbl add constraint fk_id_dmn_aut_crt_prov_id foreign key (crt_prov_id) references sec_prov_tbl(prov_id); 
alter table id_dmn_tbl drop constraint fk_asgn_aut_obslt_prov_id;
alter table id_dmn_tbl add constraint fk_id_dmn_aut_obslt_prov_id foreign key (obslt_prov_id) references sec_prov_tbl(prov_id); 
alter table id_dmn_tbl add constraint fk_id_dmn_aut_apd_prov_id foreign key (upd_prov_id) references sec_prov_tbl(prov_id); 

alter table id_dmn_tbl rename aut_id  to dmn_id;
alter table id_dmn_tbl rename aut_name to dmn_name;
alter table asgn_aut_scp_tbl rename to id_dmn_scp_tbl;
alter table id_dmn_scp_tbl rename aut_id to dmn_id;
alter table ent_id_tbl rename aut_id to dmn_id;
alter table act_id_tbl rename aut_id to dmn_id;
DROP FUNCTION IS_ENT_ID_UNQ;--#!
ALTER TABLE ENT_ID_TBL ADD REL INT DEFAULT 0 NOT NULL; --#!
ALTER TABLE ACT_ID_TBL ADD REL INT DEFAULT 0 NOT NULL; --#!

CREATE TABLE ASGN_AUT_TBL (
	AUT_ID UUID NOT null default uuid_generate_v1(),
	DMN_ID UUID NOT NULL, -- IDENTITY DOMAIN
	APP_ID UUID NOT NULL, -- APPLICATION ALLOWED TO ASSIGN
	CRT_UTC TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL, -- THE TIME THAT THE AA WAS CREATED
	CRT_PROV_ID UUID NOT NULL, -- THE USER WHICH CREATED THE AA
	OBSLT_UTC TIMESTAMP, -- THE TIME THAT THE AA WAS OBSOLETED
	OBSLT_PROV_ID UUID, -- THE USER WHICH OBSOLETED THE AA
	REL INT DEFAULT 0 NOT NULL, -- RELIABILITY,
	CONSTRAINT PK_ASGN_AUT_TBL PRIMARY KEY (AUT_ID),
	CONSTRAINT FK_ASGN_AUT_DMN_TBL FOREIGN KEY (DMN_ID) REFERENCES ID_DMN_TBL (DMN_ID),
	CONSTRAINT FK_ASGN_AUT_APP_TBL FOREIGN KEY (APP_ID) REFERENCES SEC_APP_TBL (APP_ID),
	CONSTRAINT FK_ASGN_AUT_CRT_PROV_ID FOREIGN KEY (CRT_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID),
	CONSTRAINT FK_ASGN_AUT_OBSLT_PROV_ID FOREIGN KEY (OBSLT_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID),
	CONSTRAINT CK_ASGN_AUT_REL CHECK (REL >= 0 AND REL <= 4)
);--#!
CREATE UNIQUE INDEX ASGN_AUT_TBL_UQ_IDX ON ASGN_AUT_TBL(DMN_ID, APP_ID);--#!
insert into asgn_aut_tbl (dmn_id, app_id, crt_utc, crt_prov_id, rel) 
select 
	dmn_id, app_id, crt_utc, crt_prov_id, 1
from id_dmn_tbl
where app_id is not null;
alter table id_dmn_tbl drop app_id;

SELECT REG_PATCH('20220509-01'); --#!