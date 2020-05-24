/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200522-01" name="Update:20200522-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add relationship types for MPI Role</summary>
 *	<remarks>This adds various relationship types MPI MDM roles</remarks>
 *	<isInstalled>select ck_patch('20200522-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

-- PATIENTS CAN HAVE FAMILY MEMBERS WHICH ARE MDM CONTROLLED
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc)
SELECT 'bacd9c6f-3fa9-481e-9636-37457962804d', CD_SET_MEM_ASSOC_TBL.CD_ID, '49328452-7e30-4dcd-94cd-fd532d111578', 'ALLOW MDM TO BE TARGET FAMILY MEMBER' FROM CD_SET_MEM_ASSOC_TBL INNER JOIN
CD_SET_TBL USING(SET_ID)
INNER JOIN CD_TBL ON (CD_TBL.CD_ID = '49328452-7e30-4dcd-94cd-fd532d111578')
WHERE MNEMONIC = 'FamilyMember';

-- ALLOW PATIENTS TO BE FAMILY MEMBERS OF PATIENTS
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc)
SELECT 'bacd9c6f-3fa9-481e-9636-37457962804d', CD_SET_MEM_ASSOC_TBL.CD_ID, 'bacd9c6f-3fa9-481e-9636-37457962804d', 'ALLOW PATIENT TO BE FAMILY MEMBER OF OTHER PATIENTS' FROM CD_SET_MEM_ASSOC_TBL INNER JOIN
CD_SET_TBL USING(SET_ID)
INNER JOIN CD_TBL ON (CD_TBL.CD_ID = '49328452-7e30-4dcd-94cd-fd532d111578')
WHERE MNEMONIC = 'FamilyMember'
AND NOT EXISTS (SELECT 1 FROM ENT_REL_VRFY_CDTBL WHERE src_cls_cD_id = 'bacd9c6f-3fa9-481e-9636-37457962804d' and rel_typ_cd_id = CD_SET_MEM_ASSOC_TBL.CD_ID and trg_cls_cd_id = 'bacd9c6f-3fa9-481e-9636-37457962804d');


SELECT REG_PATCH('20200522-01');
COMMIT;