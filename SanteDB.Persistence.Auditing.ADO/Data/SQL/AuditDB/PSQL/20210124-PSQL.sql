/** 
 * <feature scope="SanteDB.Persistence.Audit.ADO" id="20210124-00" name="Initialize:20210124-01" invariantName="npgsql">
 *	<summary>Upgrades audit schema to optimize for large datasets</summary>
 *	<remarks>This script installs the necessary core schema files for SanteDB audit</remarks>
 *  <isInstalled mustSucceed="true">SELECT to_regclass('public.aud_meta_val_cdtbl') IS NOT NULL;</isInstalled>
 * </feature>
 */
create sequence aud_meta_seq start with 1 increment by 1;
create sequence aud_act_seq start with 1 increment by 1;
create sequence aud_act_assoc_seq start with 1 increment by 1;
create sequence aud_obj_seq start with 1 increment by 1;
create sequence aud_meta_val_seq start with 1 increment by 1;

create table aud_meta_val_cdtbl (
val_id bigint not null default nextval('aud_meta_val_seq'),
val varchar(256) not null,
constraint pk_aud_meta_val_cdtbl primary key (val_id)
);

alter table aud_meta_tbl add column id_new bigint not null default nextval('aud_meta_seq');
alter table aud_meta_tbl drop constraint pk_aud_meta_tbl;
alter table aud_meta_tbl add constraint pk_aud_meta_tbl primary key (id_new);
alter table aud_meta_tbl drop column id;
alter table aud_meta_tbl rename id_new to id;
insert into aud_meta_val_cdtbl (val) select distinct val from aud_meta_tbl;
alter table aud_meta_tbl add column val_id bigint;
update aud_meta_tbl set val_id = (select val_id from aud_meta_val_cdtbl where aud_meta_val_cdtbl.val = aud_meta_tbl.val limit 1);
alter table aud_meta_tbl alter column val_id set not null;
alter table aud_meta_tbl add constraint fk_val_val_cdtbl foreign key (val_id) references aud_meta_val_cdtbl(val_id);
alter table aud_meta_tbl drop column val;

alter table aud_act_tbl add column id_new bigint not null default nextval('aud_act_seq');
alter table aud_act_assoc_tbl add column act_id_new bigint;
alter table aud_act_assoc_tbl drop constraint fk_aud_act_assoc_act_id;
alter table aud_act_tbl drop constraint pk_aud_act_tbl;
alter table aud_act_tbl add constraint pk_aud_act_tbl primary key (id_new);
update aud_act_assoc_tbl set act_id_new = (select id_new from aud_act_tbl where aud_act_tbl.id = aud_act_assoc_tbl.act_id );
alter table aud_act_assoc_tbl add constraint fk_aud_act_assoc_act_id foreign key (act_id_new) references aud_act_tbl(id_new);
alter table aud_act_assoc_tbl drop column act_id;
alter table aud_act_assoc_tbl rename column act_id_new to act_id;
alter table aud_act_tbl drop column id;
alter table aud_act_tbl rename column id_new to id;

alter table aud_act_assoc_tbl add column id_new bigint not null default nextval('aud_act_assoc_seq');
alter table aud_act_assoc_tbl drop constraint pk_aud_act_assoc_tbl;
alter table aud_act_assoc_tbl add constraint pk_aud_act_Associ_tbl primary key (id_new);
alter table aud_act_assoc_tbl drop column id;
alter table aud_act_assoc_tbl rename column id_new to id;

alter table aud_obj_tbl add column id_new bigint not null default nextval('aud_obj_seq');
alter table aud_obj_tbl drop constraint pk_aud_obj_tbl;
alter table aud_obj_tbl add constraint pk_aud_obj_tbl primary key (id_new);
alter table aud_obj_tbl drop column id;
alter table aud_obj_tbl rename column id_new to id;
alter table aud_obj_tbl add cst_id_typ uuid;
alter table aud_obj_tbl add constraint fk_aud_obj_cst_id foreign key (cst_id_typ) references aud_cd_tbl(id);

CREATE SEQUENCE aud_obj_dat_seq START WITH 1 INCREMENT BY 1;

 -- TABLE FOR STORAGE OF AUDIT OBJECTS
 CREATE TABLE aud_obj_dat_tbl (
	id	bigint not null default nextval('aud_obj_dat_seq'),
	obj_id bigint NOT NULL, -- OBJECT TO WHICH THE DATA BELONGS
	key VARCHAR(256), -- THE KEY OBJECT IDENTIFIER
	val BYTEA, -- ADDITIONAL NAME DATA ASSIGNED TO THE OBJECT
	CONSTRAINT pk_aud_obj_dat_tbl PRIMARY KEY (id),
	CONSTRAINT fk_aud_obj_dat_obj_tbl FOREIGN KEY (obj_id) REFERENCES aud_obj_tbl(id)
);

CREATE INDEX aud_obj_obj_dat_id_idx ON aud_obj_dat_tbl(obj_id);
