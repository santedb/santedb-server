/** 
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
	TEXT BLOB SUB_TYPE TEXT,
	CONSTRAINT PK_NAR_TBL PRIMARY KEY (ACT_VRSN_ID),
	CONSTRAINT FK_NAR_ACT_VRSN_TBL FOREIGN KEY (ACT_VRSN_ID) REFERENCES ACT_VRSN_TBL(ACT_VRSN_ID)
);--#!
ALTER TABLE pat_tbl ADD NAT_CD_ID UUID;--#!
ALTER TABLE pat_tbl ADD CONSTRAINT ck_nat_cd_id CHECK (NAT_CD_ID IS NULL OR IS_CD_SET_MEM(NAT_CD_ID, 'Nationality') OR IS_CD_SET_MEM(NAT_CD_ID, 'NullReason'));--#!
SELECT REG_PATCH('20211229-01') FROM RDB$DATABASE; --#!
