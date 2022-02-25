﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211229-01" name="Update:20211229-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds narrative structures to database</summary>
 *	<isInstalled>select ck_patch('20211229-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */

CREATE TABLE NAR_TBL (
	ACT_VRSN_ID UUID NOT NULL,
	VER VARCHAR(32),
	LANG_CS VARCHAR(4) NOT NULL,
	TITLE VARCHAR(256) NOT NULL,
	MIME VARCHAR(32) NOT NULL,
	TEXT BLOB,
	CONSTRAINT PK_NAR_TBL PRIMARY KEY (ACT_VRSN_ID),
	CONSTRAINT FK_NAR_ACT_VRSN_TBL FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID)
);--#!
ALTER TABLE pat_tbl ADD NAT_CD_ID UUID;--#!
ALTER TABLE pat_tbl ADD CONSTRAINT ck_nat_cd_id CHECK (NAT_CD_ID IS NULL OR IS_CD_SET_MEM(NAT_CD_ID, 'Nationality') OR IS_CD_SET_MEM(NAT_CD_ID, 'NullReason'));--#!
ALTER TABLE proto_tbl ADD NAR_ID UUID;--#!
ALTER TABLE proto_tbl ADD CONSTRAINT fk_proto_nar_id FOREIGN KEY (NAR_ID) REFERENCES ACT_TBL(ACT_ID);--#!
ALTER TABLE act_proto_assoc_tbl ADD ver VARCHAR(32);--#!

-- ASSRERT ACT IS A PARTICULAR CLASS
CREATE OR ALTER FUNCTION IS_ACT_CLS(
	ACT_ID_IN UUID,
	CLS_MNEMONIC_IN VARCHAR(32)
) RETURNS BOOLEAN AS 
BEGIN
	RETURN EXISTS (SELECT 1 FROM ACT_VRSN_TBL INNER JOIN CD_VRSN_TBL ON (ACT_VRSN_TBL.CLS_CD_ID = CD_VRSN_TBL.CD_ID) WHERE ACT_ID = :ACT_ID_IN AND CD_VRSN_TBL.MNEMONIC = :CLS_MNEMONIC_IN AND ACT_VRSN_TBL.OBSLT_UTC IS NULL AND CD_VRSN_TBL.OBSLT_UTC IS NULL);
END;
--#!

-- ASSRERT ACT IS A PARTICULAR CLASS
CREATE OR ALTER FUNCTION IS_ENT_CLS(
	ENT_ID_IN UUID,
	CLS_MNEMONIC_IN VARCHAR(32)
) RETURNS BOOLEAN AS 
BEGIN
	RETURN EXISTS (SELECT 1 FROM ENT_VRSN_TBL INNER JOIN CD_VRSN_TBL ON (ENT_VRSN_TBL.CLS_CD_ID = CD_VRSN_TBL.CD_ID) WHERE ENT_ID = :ENT_ID_IN AND CD_VRSN_TBL.MNEMONIC = :CLS_MNEMONIC_IN AND ENT_VRSN_TBL.OBSLT_UTC IS NULL AND CD_VRSN_TBL.OBSLT_UTC IS NULL);
END;
--#!
ALTER TABLE proto_tbl ADD CONSTRAINT ck_proto_nar_cls CHECK (NAR_ID IS NULL OR IS_ACT_CLS(NAR_ID, 'ActClassDocument'));--#!
ALTER TABLE proto_tbl ADD hdlr_cls VARCHAR(512);--#!
ALTER TABLE proto_tbl DROP hdlr_id;--#!
DROP TABLE proto_hdlr_tbl;--#!
SELECT REG_PATCH('20211229-01') FROM RDB$DATABASE; --#!
