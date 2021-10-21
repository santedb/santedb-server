/** 
 * <feature scope="SanteDB.Persistence.Audit.ADO" id="20210124-00" name="Initialize:20210124-01" invariantName="FirebirdSQL">
 *	<summary>Upgrades audit schema to optimize for large datasets</summary>
 *	<remarks>This script installs the necessary core schema files for SanteDB audit</remarks>
 *  <isInstalled mustSucceed="true">select true from rdb$database where exists (select 1 from rdb$relations where rdb$relation_name = 'AUD_META_VAL_CDTBL');</isInstalled>
 * </feature>
 */
create sequence aud_meta_seq start with 1 increment by 1;--#!
create sequence aud_act_seq start with 1 increment by 1;--#!
create sequence aud_act_assoc_seq start with 1 increment by 1;--#!
create sequence aud_obj_seq start with 1 increment by 1;--#!
create sequence aud_meta_val_seq start with 1 increment by 1;--#!
create table aud_meta_val_cdtbl (
val_id bigint not null,
val varchar(256) not null,
constraint pk_aud_meta_val_cdtbl primary key (val_id)
);
--#!
alter table aud_meta_tbl drop constraint pk_aud_meta_tbl;--#!
alter table aud_meta_tbl drop id;--#!
alter table aud_meta_tbl add id bigint not null;--#!
alter table aud_meta_tbl add constraint pk_aud_meta_tbl primary key (id);--#!
alter table aud_meta_tbl add val_id bigint not null;--#!
alter table aud_meta_tbl add constraint fk_val_val_cdtbl foreign key (val_id) references aud_meta_val_cdtbl(val_id);--#!
alter table aud_meta_tbl drop val;--#!

alter table aud_act_tbl drop constraint pk_aud_act_tbl;
alter table aud_act_tbl drop id;
alter table aud_act_tbl add id bigint not null;
alter table aud_act_tbl add constraint pk_aud_act_tbl primary key (id);
alter table aud_act_assoc_tbl drop constraint fk_aud_act_assoc_act_id;
alter table aud_act_assoc_tbl drop column act_id;
alter table aud_act_assoc_tbl add act_id bigint not null;
alter table aud_act_tbl add constraint pk_aud_act_tbl primary key (id);
alter table aud_act_assoc_tbl add constraint fk_aud_act_assoc_act_id foreign key (act_id) references aud_act_tbl(id);


alter table aud_act_assoc_tbl add column id_new bigint not null default nextval('aud_act_assoc_seq');
alter table aud_act_assoc_tbl drop constraint pk_aud_act_assoc_tbl;
alter table aud_act_assoc_tbl add constraint pk_aud_act_Associ_tbl primary key (id_new);
alter table aud_act_assoc_tbl drop column id;
alter table aud_act_assoc_tbl rename column id_new to id;

alter table aud_obj_tbl add column id_new bigint not null default nextval('aud_obj_seq');
alter table aud_obj_tbl drop constraint pk_aud_obj_tbl;
alter table aud_obj_tbl add constraint pk_aud_obj_tbl primary key (id_new);
alter table aud_obj_tbl drop column id;
alter table aud_obj_tbl rename column id_new to id;--#!
CREATE TRIGGER aud_meta_val_cdtbl_seq FOR aud_meta_val_cdtbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.val_id = NEXT VALUE FOR aud_meta_val_seq;
END;
--#!
CREATE TRIGGER aud_meta_tbl_seq FOR aud_meta_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR aud_meta_seq;
END;
--#!
CREATE TRIGGER aud_act_tbl_seq FOR aud_act_tbl ACTIVE BEFORE INSERT POSITION 0 AS BEGIN
	NEW.id = NEXT VALUE FOR aud_act_seq;
END;
--#!
alter table aud_obj_tbl add cst_id_typ uuid;--#!
alter table aud_obj_tbl add constraint fk_aud_obj_cst_id foreign key (cst_id_typ) references aud_cd_tbl(id);--#!