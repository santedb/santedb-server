/** 
 * <update id="20190522-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Add relationship "next of kin" between all persons of the same class</summary>
 *	<remarks>Any entity is technically allowed to replace itself :)</remarks>
 *	<isInstalled>select ck_patch('20190522-01')</isInstalled>
 * </update>
 */

BEGIN TRANSACTION ;

ALTER TABLE ENT_EXT_TBL ALTER EXT_DISP TYPE TEXT;
ALTER TABLE ACT_EXT_TBL ALTER EXT_DISP TYPE TEXT;

-- GRANT SYSTEM LOGIN AS A SERVICE
INSERT INTO sec_rol_pol_assoc_tbl (pol_id, rol_id, pol_act) VALUES ('e15b96ab-646c-4c00-9a58-ea09eee67d7c', 'c3ae21d2-fc23-4133-ba42-b0e0a3b817d7', 2);
DROP INDEX SEC_DEV_SCRT_IDX ;--#!

INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc)
	SELECT cd_id, 'bacd9c6f-3fa9-481e-9636-37457962804d', 'bacd9c6f-3fa9-481e-9636-37457962804d', 'err_patient_nok_personOnly'
	FROM cd_set_mem_assoc_tbl
	
	WHERE set_id = 'd3692f40-1033-48ea-94cb-31fc0f352a4e'
	AND NOT EXISTS (SELECT 1 FROM ent_rel_vrfy_cdtbl WHERE src_cls_cd_id = 'bacd9c6f-3fa9-481e-9636-37457962804d' AND trg_cls_cd_id = 'bacd9c6f-3fa9-481e-9636-37457962804d' AND rel_typ_cd_id = cd_id);

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


-- TRIGGER - ENSURE THAT ANY VALUE INSERTED INTO THE ENT_REL_TBL HAS THE PROPER PARENT
CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl () RETURNS TRIGGER AS $$
DECLARE 
	err_ref varchar(128)[];
	
BEGIN
	IF NOT EXISTS (
		SELECT TRUE 
		FROM 
			ent_rel_vrfy_cdtbl 
			INNER JOIN ent_tbl src_ent ON (src_ent.ent_id = NEW.src_ent_id)
			INNER JOIN ent_tbl trg_ent ON (trg_ent.ent_id = NEW.trg_ent_id)
		WHERE 
			rel_typ_cd_id = NEW.rel_typ_cd_id 
			AND src_cls_cd_id = src_ent.cls_cd_id 
			AND trg_cls_cd_id = trg_ent.cls_cd_id
	) THEN
		SELECT DISTINCT 
			('{' || rel_cd.mnemonic || ',' || src_cd.mnemonic || ',' || trg_cd.mnemonic || '}')::VARCHAR[] INTO err_ref
		FROM 
			ent_tbl src_ent 
			CROSS JOIN ent_tbl trg_ent
			CROSS JOIN CD_VRSN_TBL REL_CD
			LEFT JOIN CD_VRSN_TBL SRC_CD ON (SRC_ENT.CLS_CD_ID = SRC_CD.CD_ID)
			LEFT JOIN CD_VRSN_TBL TRG_CD ON (TRG_ENT.CLS_CD_ID = TRG_CD.CD_ID)
		WHERE
			src_ent.ent_id = NEW.src_ent_id
			AND trg_ent.ent_id = NEW.trg_ent_id
			AND REL_CD.CD_ID = NEW.REL_TYP_CD_ID;

		IF err_ref[1] IS NULL OR err_ref[2] IS NULL OR err_ref[3] IS NULL THEN
			RETURN NEW; -- LET THE FK WORK
		ELSE 
			RAISE EXCEPTION 'Validation error: Relationship % [%] between % [%] > % [%] is invalid', NEW.rel_typ_cd_id, err_ref[1], NEW.src_ent_id, err_ref[2], NEW.trg_ent_id, err_ref[3]
				USING ERRCODE = 'O9001';
		END IF;
	END IF;
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

ALTER TABLE PSN_LNG_TBL ALTER COLUMN LNG_CS TYPE VARCHAR(5); -- ALTER TYPE TO PERMIT STORAGE OF LOCALE


--INSERT INTO SEC_ROL_POL_ASSOC_TBL (POL_ID, ROL_ID, POL_ACT)  VALUES ('DA73C05A-3159-48C8-BBCB-741911D91CD2', 'c3ae21d2-fc23-4133-ba42-b0e0a3b817d7', 2); -- GRANT Unrestricted ALL to SYSTEM
SELECT REG_PATCH('20190522-01');
COMMIT;
