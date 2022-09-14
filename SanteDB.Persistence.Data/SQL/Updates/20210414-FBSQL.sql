/** 
 * <feature scope="SanteDB.Persistence.Data" id="20210414-01" name="Update:20210414-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds classification codes to relationship tables and refactors gender code</summary>
 *	<isInstalled>select ck_patch('20210414-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */

ALTER TABLE act_rel_tbl ADD cls_cd_id UUID; --#!
ALTER TABLE act_ptcpt_tbl ADD cls_cd_id UUID; --#!
ALTER TABLE ent_rel_tbl ADD cls_cd_id UUID; --#!


ALTER TABLE act_rel_tbl ADD CONSTRAINT ck_act_rel_cls_cd_id CHECK (cls_cd_id IS NULL OR CK_IS_CD_SET_MEM(cls_cd_id, 'RelationshipClass', FALSE)); --#!
ALTER TABLE act_ptcpt_tbl ADD CONSTRAINT ck_act_ptcpt_cls_cd_id CHECK (cls_cd_id IS NULL OR CK_IS_CD_SET_MEM(cls_cd_id, 'RelationshipClass', FALSE)); --#!
ALTER TABLE ent_rel_tbl ADD CONSTRAINT ck_ent_rel_cls_cd_id CHECK (cls_cd_id IS NULL OR CK_IS_CD_SET_MEM(cls_cd_id, 'RelationshipClass', FALSE)); --#!

ALTER TABLE ent_rel_tbl ADD rol_cd_id UUID;--#!

ALTER TABLE act_rel_tbl ADD CONSTRAINT fk_act_rel_cls_cd_cd_id FOREIGN KEY (cls_cd_id) REFERENCES cd_tbl(cd_id);--#!
ALTER TABLE ent_rel_tbl ADD CONSTRAINT fk_ent_rel_cls_cd_cd_id FOREIGN KEY (cls_cd_id) REFERENCES cd_tbl(cd_id);--#!
ALTER TABLE ent_rel_tbl ADD CONSTRAINT fk_ent_rel_rol_cd_cd_id FOREIGN KEY (rol_cd_id) REFERENCES cd_tbl(cd_id);--#!
ALTER TABLE act_ptcpt_tbl ADD CONSTRAINT fk_act_ptcpt_cls_cd_cd_id FOREIGN KEY (cls_cd_id) REFERENCES cd_tbl(cd_id);--#!

ALTER TABLE psn_tbl ADD gndr_cd_id UUID;--#!
ALTER TABLE psn_tbl ADD CONSTRAINT CK_PNS_GNDR_CD CHECK (GNDR_CD_ID IS NULL OR CK_IS_CD_SET_MEM(GNDR_CD_ID, 'AdministrativeGenderCode', TRUE));--#!
UPDATE psn_tbl SET gndr_cd_id = (SELECT pat_tbl.gndr_cd_id FROM pat_tbl WHERE pat_tbl.ent_vrsn_id = psn_tbl.ent_vrsn_id);--#!
ALTER TABLE pat_tbl DROP gndr_cd_id;--#!

UPDATE sec_rol_tbl SET rol_name = 'APPLICATIONS' WHERE rol_name = 'SYNCHRONIZERS';--#!

SELECT REG_PATCH('20210414-01') FROM RDB$DATABASE; --#!
