/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211110-01" name="Update:20211110-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Upgrades the manner in which address components are saved</summary>
 *	<isInstalled>select ck_patch('20211110-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */
 
alter table ent_name_cmp_tbl add val VARCHAR(256);--#!
alter table ent_addr_cmp_tbl add val VARCHAR(256);--#!


update ent_name_cmp_tbl set val = (SELECT phon_val_tbl.val from phon_val_tbl where phon_val_tbl.val_seq_id  = ent_name_cmp_tbl.val_seq_id );--#!
update ent_addr_cmp_tbl set val = (SELECT ent_addr_cmp_val_tbl.val from ent_addr_cmp_val_tbl where ent_addr_cmp_val_tbl.val_seq_id  = ent_addr_cmp_tbl.val_seq_id );--#!

drop index ENT_NAME_CMP_PHON_VAL_ID_IDX; --#!
drop index ENT_ADDR_CMP_VAL_ID_IDX; --#!
alter table ent_name_cmp_tbl drop val_seq_id;--#!
alter table ent_addr_cmp_tbl drop val_seq_id;--#!

alter table ent_name_cmp_tbl alter column val set not null;--#!
alter table ent_addr_cmp_tbl alter column val set not null;--#!

drop trigger TG_ENT_ADDR_CMP_VAL_TBL_SEQ;--#!
drop trigger TG_PHON_VAL_TBL_SEQ;--#!
drop sequence PHON_VAL_SEQ;--#!
drop sequence ENT_ADDR_CMP_VAL_SEQ;--#!

drop index EN_ADDR_CMP_VAL_VAL_IDX;--#!

CREATE SEQUENCE ENT_ADDR_CMP_SEQ;--#!
CREATE SEQUENCE ENT_NAME_CMP_SEQ;--#!

alter table ent_addr_cmp_tbl add seq_id bigint; --#!
alter table ent_name_cmp_tbl add seq_id bigint; --#!
CREATE TRIGGER TG_ENT_NAME_CMP_TBL_SEQ FOR ENT_NAME_CMP_TBL ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.SEQ_ID = NEXT VALUE FOR ENT_NAME_CMP_SEQ;
END;--#!
CREATE TRIGGER TG_ENT_ADDR_CMP_TBL_SEQ FOR ENT_ADDR_CMP_TBL ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.SEQ_ID = NEXT VALUE FOR ENT_ADDR_CMP_SEQ;
END;--#!
CREATE INDEX ENT_NAME_CMP_VAL_IDX ON ENT_NAME_CMP_TBL(VAL); --#!
CREATE INDEX ENT_ADDR_CMP_VAL_IDX ON ENT_ADDR_CMP_TBL(VAL); --#!
DROP TABLE ENT_ADDR_CMP_VAL_TBL;--#!
DROP TABLE PHON_VAL_TBL;--#!
SELECT REG_PATCH('20211110-01') FROM RDB$DATABASE; --#!
