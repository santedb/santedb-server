﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210628-01" name="Update:20210628-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds key relationship codes which were missing in the original SDB</summary>
 *	<isInstalled>select ck_patch('20210628-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */

 INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('B52C7E95-88B8-4C4C-836A-934277AFDB92'),CHAR_TO_UUID('d39073be-0f8f-440e-b8c8-7034cc138a95'),CHAR_TO_UUID('fafec286-89d5-420b-9085-054aca9d1eef'),'ADMINISTERABLE MATERIAL');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('B52C7E95-88B8-4C4C-836A-934277AFDB92'),CHAR_TO_UUID('d39073be-0f8f-440e-b8c8-7034cc138a95'),CHAR_TO_UUID('d39073be-0f8f-440e-b8c8-7034cc138a95'),'ADMINISTERABLE MATERIAL');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('77B7A04B-C065-4FAF-8EC0-2CDAD4AE372B'),CHAR_TO_UUID('9de2a846-ddf2-4ebc-902e-84508c5089ea'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'ASSIGNED ENTITY/EMPLOY');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('77B7A04B-C065-4FAF-8EC0-2CDAD4AE372B'),CHAR_TO_UUID('6b04fed8-c164-469c-910b-f824c2bda4f0'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'ASSIGNED ENTITY/EMPLOY');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('31B0DFCB-D7BA-452A-98B9-45EBCCD30732'),CHAR_TO_UUID('9de2a846-ddf2-4ebc-902e-84508c5089ea'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),'CAREGIVER');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('31B0DFCB-D7BA-452A-98B9-45EBCCD30732'),CHAR_TO_UUID('6b04fed8-c164-469c-910b-f824c2bda4f0'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),'CAREGIVER');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('31B0DFCB-D7BA-452A-98B9-45EBCCD30732'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),'CAREGIVER');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('9D256279-F1AC-46B3-A974-DD13E2AD4F72'),CHAR_TO_UUID('e29fcfad-ec1d-4c60-a055-039a494248ae'),CHAR_TO_UUID('9de2a846-ddf2-4ebc-902e-84508c5089ea'),'CLAIMANT');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('8FF9D9A5-A206-4566-82CD-67B770D7CE8A'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'COVERAGE SPONSOR');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('8FF9D9A5-A206-4566-82CD-67B770D7CE8A'),CHAR_TO_UUID('e29fcfad-ec1d-4c60-a055-039a494248ae'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),'POLICY COVERED PARTY');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('6B04FED8-C164-469C-910B-F824C2BDA4F0'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'HEALTHCARE PROVIDER');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('6B04FED8-C164-469C-910B-F824C2BDA4F0'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('6b04fed8-c164-469c-910b-f824c2bda4f0'),'HEALTHCARE PROVIDER');--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('b1d2148d-bb35-4337-8fe6-021f5a3ac8a3'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('6b04fed8-c164-469c-910b-f824c2bda4f0'),'CONTACT->PROVIDER') ;--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('b1d2148d-bb35-4337-8fe6-021f5a3ac8a3'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('9de2a846-ddf2-4ebc-902e-84508c5089ea'),'CONTACT->PERSON') ;--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('b1d2148d-bb35-4337-8fe6-021f5a3ac8a3'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),'CONTACT->PATIENT') ;--#!
INSERT INTO ENT_REL_VRFY_CDTBL (REL_TYP_CD_ID, SRC_CLS_CD_ID, TRG_CLS_CD_ID, ERR_DESC) VALUES (CHAR_TO_UUID('b1d2148d-bb35-4337-8fe6-021f5a3ac8a3'),CHAR_TO_UUID('bacd9c6f-3fa9-481e-9636-37457962804d'),CHAR_TO_UUID('7c08bd55-4d42-49cd-92f8-6388d6c4183f'),'CONTACT->ORGANIZATION') ;--#!
-- OPTIONAL
DROP INDEX SEC_DEV_SCRT_IDX ;--#!

-- 
INSERT INTO ENT_REL_VRFY_CDTBL (rel_typ_cd_id , src_cls_cd_id , trg_cls_cd_id , err_desc )
SELECT CD_ID AS REL_TYP_CD_ID, CHAR_TO_UUID('BACD9C6F-3FA9-481E-9636-37457962804D'),CHAR_TO_UUID('BACD9C6F-3FA9-481E-9636-37457962804D'),'FAMILY MEMBER PATIENT<>PATIENT'  
FROM cd_set_mem_assoc_tbl WHERE SET_ID=CHAR_TO_UUID('D3692F40-1033-48EA-94CB-31FC0F352A4E')
AND CD_ID NOT IN (SELECT REL_TYP_CD_ID FROM ENT_REL_VRFY_CDTBL WHERE SRC_CLS_CD_ID = CHAR_TO_UUID('BACD9C6F-3FA9-481E-9636-37457962804D') AND TRG_CLS_CD_ID = CHAR_TO_UUID('BACD9C6F-3FA9-481E-9636-37457962804D'))
;--#!

-- PORTION
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) 
	SELECT 
		CHAR_TO_UUID('CFF670E4-965E-4288-B966-47A44479D2AD'), CD_ID, CD_ID, 'ALIQUOT'
	FROM CD_SET_MEM_ASSOC_TBL WHERE SET_ID = CHAR_TO_UUID('4E6DA567-0094-4F23-8555-11DA499593AF')
 ;--#!

 
DROP INDEX ENT_ID_ENT_VAL_UQ_IDX;--#!
CREATE UNIQUE INDEX ENT_ID_ENT_VAL_UQ_IDX ON ENT_ID_TBL COMPUTED BY (CASE WHEN OBSLT_VRSN_SEQ_ID IS NULL THEN UUID_TO_CHAR(ENT_ID) || ID_VAL END);--#!
SELECT REG_PATCH('20210628-01') FROM RDB$DATABASE; --#!
