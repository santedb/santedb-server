/** 
 * <feature scope="SanteDB.Persistence.Data" id="20180211-01" name="Update:20180211-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add relationship "birthplace" between all entities of the same class</summary>
 *	<remarks>Any entity is technically allowed to replace itself :)</remarks>
 *	<isInstalled>select ck_patch('20180211-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('C6B92576-1D62-4896-8799-6F931F8AB607', '7C08BD55-4D42-49CD-92F8-6388D6C4183F', '21AB7873-8EF3-4D78-9C19-4582B3C40631', 'err_stateDedicatedSDL');

-- RULE BIRTHPLACE CAN BE BETWEEN A PLACE OR ORGANIZATION AND PATIENT OR PERSON
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', '9de2a846-ddf2-4ebc-902e-84508c5089ea', '21ab7873-8ef3-4d78-9c19-4582b3c40631', 'Birthplace Person>Place');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', '9de2a846-ddf2-4ebc-902e-84508c5089ea', '7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'Birthplace Person>Organization');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', 'bacd9c6f-3fa9-481e-9636-37457962804d', '21ab7873-8ef3-4d78-9c19-4582b3c40631', 'Birthplace Person>Place');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', 'bacd9c6f-3fa9-481e-9636-37457962804d', '7c08bd55-4d42-49cd-92f8-6388d6c4183f', 'Birthplace Person>Organization');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', '9de2a846-ddf2-4ebc-902e-84508c5089ea', 'ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c', 'Birthplace Person>SDL');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76', 'bacd9c6f-3fa9-481e-9636-37457962804d', 'ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c', 'Birthplace Person>SDL');

-- RULE CITIZENSHIP
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('35B13152-E43C-4BCB-8649-A9E83BEE33A2', '9de2a846-ddf2-4ebc-902e-84508c5089ea', '48b2ffb3-07db-47ba-ad73-fc8fb8502471', 'Citizenship Person>COUNTRY');
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES ('35B13152-E43C-4BCB-8649-A9E83BEE33A2', 'bacd9c6f-3fa9-481e-9636-37457962804d', '48b2ffb3-07db-47ba-ad73-fc8fb8502471', 'Citizenship Person>COUNTRY');

ALTER TABLE ent_rel_vrfy_cdtbl ALTER COLUMN err_desc TYPE VARCHAR(128);

UPDATE ENT_REL_VRFY_CDTBL 
	SET err_desc = (
		SELECT SRC.MNEMONIC || ' ==[' || TYP.MNEMONIC || ']==> ' || TRG.MNEMONIC 
		FROM 
			ENT_REL_VRFY_CDTBL VFY
			INNER JOIN CD_VRSN_TBL TYP ON (REL_TYP_CD_ID = TYP.CD_ID)
			INNER JOIN CD_VRSN_TBL SRC ON (SRC_CLS_CD_ID = SRC.CD_ID)
			INNER JOIN CD_VRSN_TBL TRG ON (TRG_CLS_CD_ID = TRG.CD_ID)
		WHERE 
			VFY.ENT_REL_VRFY_ID = ENT_REL_VRFY_CDTBL.ENT_REL_VRFY_ID
		FETCH FIRST 1 ROWS ONLY
	);

SELECT REG_PATCH('20180211-01');

-- GET THE SCHEMA VERSION
CREATE OR REPLACE FUNCTION GET_SCH_VRSN() RETURNS VARCHAR(10) AS
$$
BEGIN
	RETURN '1.1.0.0';
END;
$$ LANGUAGE plpgsql;

COMMIT;
