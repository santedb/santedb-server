/** 
 * <update id="20181110-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Switches the database to use integer private keys instead of uuid public keys</summary>
 *	<remarks>This patch updates the foreign key relationship of codes, entities and acts
 *  to carry integer and uuid based keys. Integer keys are used internally for joining tables. 
 *  please note that this patch requires the latest (1.9.0.0) version of the ORM Lite and ADO 
 *  data providers.</remarks>
 *	<isInstalled>select ck_patch('20181110-01')</isInstalled>
 * </update>
 */

 BEGIN TRANSACTION;

 CREATE TABLE IF NOT EXISTS ENT_POL_ASSOC_TBL (
	SEC_POL_INST_ID UUID NOT NULL DEFAULT uuid_generate_v1(),
	ENT_ID UUID NOT NULL, -- THE ACT TO WHICH THE POLICY APPLIES
	EFFT_VRSN_SEQ_ID NUMERIC(20) NOT NULL, -- THE VERSION OF THE ACT WHERE THE POLICY ASSOCIATION DID BECOME ACTIVE
	OBSLT_VRSN_SEQ_ID NUMERIC(20), -- THE VERSION OF THE ACT WHERE THE POLICY ASSOCIATION IS OBSOLETE,
	POL_ID UUID NOT NULL, -- THE IDENTIFIER OF THE POLICY WHICH IS ATTACHED TO THE ACT
	CONSTRAINT PK_ENT_POL_ASSOC_TBL PRIMARY KEY(SEC_POL_INST_ID),
	CONSTRAINT FK_ENT_POL_ENT_ID FOREIGN KEY (ENT_ID) REFERENCES ENT_TBL(ENT_ID),
	CONSTRAINT FK_ENT_POL_EFFT_VRSN_SEQ_ID FOREIGN KEY (EFFT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL(VRSN_SEQ_ID),
	CONSTRAINT FK_ENT_POL_OBSLT_VRSN_SEQ_ID FOREIGN KEY (OBSLT_VRSN_SEQ_ID) REFERENCES ENT_VRSN_TBL(VRSN_SEQ_ID),
	CONSTRAINT FK_ENT_POL_POL_ID FOREIGN KEY (POL_ID) REFERENCES SEC_POL_TBL(POL_ID)
);


 -- creation of code sequence
alter table cd_tbl rename cd_id to uuid;
create sequence cd_id_seq start with 1 increment by 1;
alter table cd_tbl add cd_id integer default nextval('cd_id_seq');

-- code versions
alter table cd_vrsn_tbl rename cd_id to cd_uuid;
alter table cd_vrsn_tbl add cd_id integer;
update cd_vrsn_tbl set cd_id = (select cd_id from cd_tbl where cd_tbl.uuid = cd_uuid);
alter table cd_vrsn_tbl alter cd_id set not null;
alter table cd_vrsn_tbl drop cd_uuid cascade;

-- code set
create sequence cd_set_id_seq start with 1 increment by 1;
alter table cd_set_tbl rename set_id to uuid;
alter table cd_set_tbl add set_id integer not null default nextval('cd_set_id_seq');

-- code set member
alter table cd_set_mem_assoc_tbl rename cd_id to cd_uuid;
alter table cd_set_mem_assoc_tbl add cd_id integer;
update cd_set_mem_assoc_tbl set cd_id = (select cd_id from cd_tbl where uuid = cd_uuid);
alter table cd_set_mem_assoc_tbl drop cd_uuid cascade;
alter table cd_set_mem_assoc_tbl alter cd_id set not null;
alter table cd_set_mem_assoc_tbl rename set_id to set_uuid;
alter table cd_set_mem_assoc_tbl add set_id integer;
update cd_set_mem_assoc_tbl set set_id = (select set_id from cd_set_tbl where uuid = set_uuid);
alter table cd_set_mem_assoc_tbl drop set_uuid;
alter table cd_set_mem_assoc_tbl alter set_id set not null;

-- code set primary key
alter table cd_set_tbl drop constraint pk_cd_set_tbl;
alter table cd_set_tbl add constraint pk_cd_set_tbl primary key (set_id);
alter table cd_set_mem_assoc_tbl add constraint fk_cd_set_mem_set_id foreign key (set_id) references cd_set_tbl(set_id);

-- assert class code based on integer key
CREATE OR REPLACE FUNCTION ASSRT_CD_CLS(
	CD_ID_IN IN INTEGER,
	CLS_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS
$$
BEGIN
	RETURN (SELECT COUNT(*) FROM CD_VRSN_TBL INNER JOIN CD_CLS_TBL USING (CLS_ID) WHERE CD_ID = CD_ID_IN AND (CD_CLS_TBL.MNEMONIC = CLS_MNEMONIC_IN OR
	-- CAN ALSO BE A NuLL REASON
	 CD_CLS_TBL.CLS_ID = '05ac7b93-1b1e-47dd-87dd-e56e353ecb94')) > 0;
END;
$$ LANGUAGE PLPGSQL;

-- assert code is set member
CREATE OR REPLACE FUNCTION IS_CD_SET_MEM(
	CD_ID_IN IN INTEGER,
	SET_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS 
$$
BEGIN
	RETURN (SELECT COUNT(*) FROM CD_SET_MEM_ASSOC_TBL INNER JOIN CD_SET_TBL USING(SET_ID) WHERE CD_ID = CD_ID_IN AND (MNEMONIC = SET_MNEMONIC_IN OR MNEMONIC = 'NullReason')) > 0;
END;
$$ LANGUAGE PLPGSQL;

-- check for null flavor
CREATE OR REPLACE FUNCTION ck_is_cd_set_mem ( cd_id_in IN INTEGER, set_mnemonic_in IN VARCHAR, allow_null_in IN BOOLEAN ) RETURNS BOOLEAN AS $$
DECLARE
	CD_UUID UUID;
 BEGIN 
	IF IS_CD_SET_MEM(cd_id_in, set_mnemonic_in) OR allow_null_in AND IS_CD_SET_MEM(cd_id_in, 'NullReason') THEN
		RETURN TRUE;
	ELSE 
		cd_uuid := (SELECT uuid FROM cd_tbl WHERE cd_id = cd_id_in);
		RAISE EXCEPTION 'Codification Error: Concept % is not in set %', cd_uuid, set_mnemonic_in
			USING ERRCODE = 'O9002';
	END IF;
 END;
 $$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cd_xref (cd_uuid_in in UUID) RETURNS INT AS $$
BEGIN
	RETURN (SELECT cd_id FROM cd_tbl WHERE uuid = cd_uuid_in);
END;
$$ LANGUAGE plpgsql;

-- alter substance administration key 
alter table sub_adm_tbl drop constraint fk_sub_adm_rte_cd_id;
alter table sub_adm_tbl drop constraint fk_sub_adm_dos_unt_cd_id;
alter table sub_adm_tbl rename rte_cd_id to rte_cd_uuid;
alter table sub_adm_tbl rename dos_unt_cd_id to dos_unt_cd_uuid;
alter table sub_adm_tbl rename ste_cd_id to ste_cd_uuid;
alter table sub_adm_tbl add rte_cd_id integer;
alter table sub_adm_tbl add dos_unt_cd_id integer;
alter table sub_adm_tbl add ste_cd_id integer;
alter table sub_adm_tbl drop constraint ck_sub_adm_rte_cd;
alter table sub_adm_tbl drop constraint ck_sub_adm_dos_unt_cd;
update sub_adm_tbl set rte_cd_id = (select cd_id from cd_tbl where uuid = rte_cd_uuid), dos_unt_cd_id = (select cd_id from cd_tbl where uuid = dos_unt_cd_uuid), ste_cd_id = cd_xref(ste_cd_uuid);
alter table sub_adm_tbl alter dos_unt_cd_id set not null;
alter table sub_adm_tbl alter rte_cd_id set not null;
alter table sub_adm_tbl drop rte_cd_uuid cascade;
alter table sub_adm_tbl drop ste_cd_uuid cascade;
alter table sub_adm_tbl drop dos_unt_cd_uuid cascade;
alter table sub_adm_tbl alter rte_cd_id set default cd_xref('8ba48d37-6c86-4a54-9a2f-e3a1dad2e6a2');
alter table sub_adm_tbl add constraint ck_sub_adm_rte_cd CHECK (assrt_cd_cls(rte_cd_id, 'Route'));
alter table sub_adm_tbl add constraint ck_sub_adm_dos_unt_cd CHECK (assrt_cd_cls(dos_unt_cd_id, 'UnitOfMeasure'));

-- act class
create sequence act_id_seq start with 1 increment by 1;
alter table act_tbl rename act_id to uuid;
alter table act_tbl rename cls_cd_id to cls_cd_uuid;
alter table act_tbl rename mod_cd_id to mod_cd_uuid;
alter table act_tbl add cls_cd_id integer;
alter table act_tbl add mod_cd_id integer;
alter table act_tbl add act_id integer not null default nextval('act_id_seq');
alter table act_tbl drop constraint ck_act_cls_cd;
alter table act_tbl drop constraint ck_act_mod_cd;
update act_tbl set cls_cd_id = cd_xref(cls_cd_uuid), mod_cd_id = cd_xref(mod_cd_uuid);
alter table act_tbl alter mod_cd_id set not null;
alter table act_tbl alter cls_cd_id set not null;
alter table act_tbl drop mod_cd_uuid cascade;
alter table act_tbl drop cls_cd_uuid cascade;
alter table act_tbl add constraint ck_act_cls_cd check (ck_is_cd_set_mem(cls_cd_id, 'ActClass', false));
alter table act_tbl add constraint ck_act_mod_cd check (ck_is_cd_set_mem(mod_cd_id, 'ActMood', false));

-- act relationship table
alter table act_rel_tbl rename rel_typ_cd_id to rel_typ_cd_uuid;
alter table act_rel_tbl rename src_act_id to src_act_uuid;
alter table act_rel_tbl rename trg_act_id to trg_act_uuid;
alter table act_rel_tbl add rel_typ_cd_id integer;
alter table act_rel_tbl add src_act_id integer;
alter table act_rel_tbl add trg_act_id integer;
drop index act_rel_unq_enf;
alter table act_rel_tbl drop constraint ck_act_rel_rel_typ_cd;
update act_rel_tbl set rel_typ_cd_id = cd_xref(rel_typ_cd_uuid), src_act_id = (select act_id from act_tbl where uuid = src_act_uuid), trg_act_id = (select act_id from act_tbl where uuid = trg_act_uuid);
alter table act_rel_tbl alter rel_typ_cd_id set not null;
alter table act_rel_tbl alter src_act_id set not null;
alter table act_rel_tbl alter trg_act_id set not null;
alter table act_rel_tbl drop src_act_uuid;
alter table act_rel_tbl drop trg_act_uuid;
alter table act_rel_tbl drop rel_typ_cd_uuid;
alter table act_rel_tbl add constraint ck_act_rel_rel_typ_cd check (ck_is_cd_set_mem(rel_typ_cd_id, 'ActRelationshipType', true));

-- act version table
alter table act_vrsn_tbl rename act_id to act_uuid;
alter table act_vrsn_tbl add act_id integer;
alter table act_vrsn_tbl rename sts_cd_id to sts_cd_uuid;
alter table act_vrsn_tbl rename typ_cd_id to typ_cd_uuid;
alter table act_vrsn_tbl rename rsn_cd_id to rsn_cd_uuid;
alter table act_vrsn_tbl add sts_cd_id integer;
alter table act_vrsn_tbl add rsn_cd_id integer;
alter table act_vrsn_tbl add typ_cd_id integer;
alter table act_vrsn_tbl drop constraint ck_act_vrsn_rsn_cd;
alter table act_vrsn_tbl drop constraint ck_act_vrsn_sts_cd;
update act_vrsn_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid), sts_cd_id = cd_xref(sts_cd_uuid), typ_cd_id = cd_xref(typ_cd_uuid), rsn_cd_id = cd_xref(rsn_cd_uuid);
alter table act_vrsn_tbl alter sts_cd_id set not null;
alter table act_vrsn_tbl alter act_id set not null;
alter table act_vrsn_tbl drop act_uuid;
alter table act_vrsn_tbl drop sts_cd_uuid;
alter table act_vrsn_tbl drop rsn_cd_uuid;
alter table act_vrsn_tbl drop typ_cd_uuid;
alter table act_vrsn_tbl add constraint ck_act_vrsn_rsn_cd check (rsn_cd_id is null or ck_is_cd_set_mem(rsn_cd_id, 'ActReasion', true));
alter table act_vrsn_tbl add constraint ck_act_vrsn_sts_cd check (ck_is_cd_set_mem(sts_cd_id, 'ActStatus', true));

-- alter act extension table 
alter table act_ext_tbl rename act_id to act_uuid;
alter table act_ext_tbl add act_id integer;
update act_ext_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid);
alter table act_ext_tbl alter act_id set not null;
alter table act_ext_tbl drop act_uuid;

-- alter act id
alter table act_id_tbl rename act_id to act_uuid;
alter table act_id_tbl add act_id integer;
update act_id_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid);
alter table act_id_tbl alter act_id set not null;
alter table act_id_tbl drop act_uuid;

-- alter act_note
alter table act_note_tbl rename act_id to act_uuid;
alter table act_note_tbl add act_id integer;
update act_note_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid);
alter table act_note_tbl alter act_id set not null;
alter table act_note_tbl drop act_uuid;

-- alter act policy
alter table act_pol_assoc_tbl rename act_id to act_uuid;
alter table act_pol_assoc_tbl add act_id integer;
update act_pol_assoc_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid);
alter table act_pol_assoc_tbl alter act_id set not null;
alter table act_pol_assoc_tbl drop act_uuid;

-- alter act protocol
alter table act_proto_assoc_tbl rename act_id to act_uuid;
alter table act_proto_assoc_tbl add act_id integer;
update act_proto_assoc_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid);
alter table act_proto_assoc_tbl alter act_id set not null;
alter table act_proto_assoc_tbl drop act_uuid;

-- alter tag 
alter table act_tag_tbl rename act_id to act_uuid;
alter table act_tag_tbl add act_id integer;
update act_tag_tbl set act_id = (select act_id from act_tbl where act_uuid = uuid);
alter table act_tag_tbl alter act_id set not null;
alter table act_tag_tbl drop act_uuid;

-- entity sequence identifiers
create sequence ent_id_seq start with 1 increment by 1;
alter table ent_tbl rename ent_id to uuid;
alter table ent_tbl rename cls_cd_id to cls_cd_uuid;
alter table ent_tbl rename dtr_cd_id to dtr_cd_uuid;
alter table ent_tbl add ent_id integer not null default nextval('ent_id_seq');
alter table ent_tbl add cls_cd_id integer;
alter table ent_tbl add dtr_cd_id integer;
alter table ent_tbl drop constraint ck_ent_cls_cd;
alter table ent_tbl drop constraint fk_ent_dtr_cd;
update ent_tbl set cls_cd_id = cd_xref(cls_cd_uuid), dtr_cd_id = cd_xref(dtr_cd_uuid);
alter table ent_tbl alter cls_cd_id set not null;
alter table ent_tbl alter dtr_cd_id set not null;
alter table ent_tbl drop cls_cd_uuid cascade;
alter table ent_tbl drop dtr_cd_uuid;
alter table ent_tbl add constraint ck_ent_cls_cd check (ck_is_cd_set_mem(cls_cd_id, 'EntityClass', false));
alter table ent_tbl add constraint ck_ent_dtr_cd check (ck_is_cd_set_mem(dtr_cd_id, 'EntityDeterminer', false));

-- reset the version seq
alter table ent_vrsn_tbl rename ent_id to ent_uuid;
alter table ent_vrsn_tbl rename typ_cd_id to typ_cd_uuid;
alter table ent_vrsn_tbl rename sts_cd_id to sts_cd_uuid;
alter table ent_Vrsn_tbl add typ_cd_id integer;
alter table ent_vrsn_tbl add sts_cd_id integer;
alter table ent_vrsn_tbl add ent_id integer;
alter table ent_vrsn_tbl drop constraint ck_ent_vrsn_sts_cd;
alter table ent_vrsn_tbl rename crt_act_id to crt_act_uuid;
alter table ent_vrsn_tbl add crt_act_id integer;
update ent_vrsn_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), sts_cd_id = cd_xref(sts_cd_uuid), typ_cd_id = cd_xref(typ_cd_uuid), crt_act_id = (select act_id from act_tbl where uuid = crt_act_uuid);
alter table ent_vrsn_tbl alter ent_id set not null;
alter table ent_vrsn_tbl alter sts_cd_id set not null;
alter table ent_vrsn_tbl drop crt_act_uuid;
alter table ent_vrsn_tbl drop ent_uuid;
alter table ent_vrsn_tbl drop sts_cd_uuid;
alter table ent_vrsn_tbl drop typ_cd_uuid;
alter table ent_vrsn_tbl add constraint ck_ent_vrsn_sts_cd check (ck_is_cd_set_mem(sts_cd_id, 'EntityStatus', false));

-- correct ent address 
create sequence ent_addr_id_seq start with 1 increment by 1;
alter table ent_addr_tbl rename addr_id to addr_uuid;
alter table ent_addr_tbl add addr_id integer not null default nextval('ent_addr_id_seq');
alter table ent_addr_tbl rename ent_id to ent_uuid;
alter table ent_addr_tbl rename use_cd_id to use_cd_uuid;
alter table ent_addr_tbl add ent_id integer;
alter table ent_addr_tbl add use_cd_id integer;
alter table ent_addr_tbl drop constraint ck_ent_addr_use_cd;
update ent_addr_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), use_cd_id = cd_xref(use_cd_uuid);
alter table ent_addr_tbl alter ent_id set not null;
alter table ent_addr_tbl alter use_cd_id set not null;
alter table ent_addr_tbl drop ent_uuid cascade;
alter table ent_addr_tbl drop use_cd_uuid;
alter table ent_addr_tbl add constraint ck_ent_addr_use_cd check (ck_is_cd_set_mem(use_cd_id, 'AddressUse', false));

-- correct ent address component
create sequence ent_addr_cmp_id_seq start with 1 increment by 1;
alter table ent_addr_cmp_tbl rename addr_id to addr_uuid;
alter table ent_addr_cmp_tbl add addr_id integer;
alter table ent_addr_cmp_tbl rename typ_cd_id to typ_cd_uuid;
alter table ent_addr_cmp_tbl add typ_cd_id integer;
alter table ent_addr_cmp_tbl drop constraint ck_ent_addr_cmp_typ_cd;
update ent_addr_cmp_tbl set addr_id = (select addr_id from ent_addr_tbl where ent_addr_cmp_tbl.addr_uuid = ent_addr_tbl.addr_uuid), typ_cd_id = cd_xref(typ_cd_uuid);
alter table ent_addr_cmp_tbl alter addr_id set not null;
alter table ent_addr_cmp_tbl drop cmp_id;
alter table ent_addr_cmp_tbl add cmp_id integer not null default nextval('ent_addr_cmp_id_seq');
alter table ent_addr_cmp_tbl drop addr_uuid;
alter table ent_addr_cmp_tbl drop typ_cd_uuid;
alter table ent_addr_cmp_tbl add constraint ck_ent_addr_cmp_typ_cd check (typ_cd_id is null or ck_is_cd_set_mem(typ_cd_id, 'AddressComponentType', false));
alter table ent_addr_cmp_tbl rename val_seq_id to val_id;

-- rekey address component
alter table ent_addr_tbl drop constraint pk_ent_addr_tbl;
alter table ent_addr_tbl add constraint pk_ent_addr_tbl primary key (addr_id);
alter table ent_addr_cmp_tbl add constraint fk_ent_addr_cmp_addr_id foreign key (addr_id) references ent_addr_tbl(addr_id);

-- rekey values
alter table ent_addr_cmp_val_tbl drop val_id;
alter table ent_addr_cmp_val_tbl rename val_seq_id to val_id;
alter table ent_addr_cmp_val_tbl add constraint pk_ent_addr_cmp_val_tbl primary key (val_id);
-- alter ent extension table 
alter table ent_ext_tbl rename ent_id to ent_uuid;
alter table ent_ext_tbl add ent_id integer;
update ent_ext_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid);
alter table ent_ext_tbl alter ent_id set not null;
alter table ent_ext_tbl drop ent_uuid;

-- alter ent id
alter table ent_id_tbl rename ent_id to ent_uuid;
alter table ent_id_tbl add ent_id integer;
update ent_id_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid);
alter table ent_id_tbl alter ent_id set not null;
alter table ent_id_tbl drop ent_uuid cascade;

-- alter ent_note
alter table ent_note_tbl rename ent_id to ent_uuid;
alter table ent_note_tbl add ent_id integer;
update ent_note_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid);
alter table ent_note_tbl alter ent_id set not null;
alter table ent_note_tbl drop ent_uuid;

-- alter ent policy
alter table ent_pol_assoc_tbl rename ent_id to ent_uuid;
alter table ent_pol_assoc_tbl add ent_id integer;
update ent_pol_assoc_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid);
alter table ent_pol_assoc_tbl alter ent_id set not null;
alter table ent_pol_assoc_tbl drop ent_uuid;

-- alter tag 
alter table ent_tag_tbl rename ent_id to ent_uuid;
alter table ent_tag_tbl add ent_id integer;
update ent_tag_tbl set ent_id = (select ent_id from ent_tbl where ent_uuid = uuid);
alter table ent_tag_tbl alter ent_id set not null;
alter table ent_tag_tbl drop ent_uuid;

-- correct ent address 
create sequence ent_name_id_seq start with 1 increment by 1;
alter table ent_name_tbl rename name_id to name_uuid;
alter table ent_name_tbl add name_id integer not null default nextval('ent_name_id_seq');
alter table ent_name_tbl rename ent_id to ent_uuid;
alter table ent_name_tbl rename use_cd_id to use_cd_uuid;
alter table ent_name_tbl add ent_id integer;
alter table ent_name_tbl add use_cd_id integer;
alter table ent_name_tbl drop constraint ck_ent_name_use_cd;
update ent_name_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), use_cd_id = cd_xref(use_cd_uuid);
alter table ent_name_tbl alter ent_id set not null;
alter table ent_name_tbl alter use_cd_id set not null;
alter table ent_name_tbl drop ent_uuid cascade;
alter table ent_name_tbl drop use_cd_uuid;
alter table ent_name_tbl add constraint ck_ent_name_use_cd check (ck_is_cd_set_mem(use_cd_id, 'NameUse', false));

-- correct ent address component
create sequence ent_name_cmp_id_seq start with 1 increment by 1;
alter table ent_name_cmp_tbl rename name_id to name_uuid;
alter table ent_name_cmp_tbl add name_id integer;
alter table ent_name_cmp_tbl rename typ_cd_id to typ_cd_uuid;
alter table ent_name_cmp_tbl add typ_cd_id integer;
alter table ent_name_cmp_tbl drop constraint ck_ent_name_cmp_typ_cd;
update ent_name_cmp_tbl set name_id = (select name_id from ent_name_tbl where ent_name_cmp_tbl.name_uuid = ent_name_tbl.name_uuid), typ_cd_id = cd_xref(typ_cd_uuid);
alter table ent_name_cmp_tbl alter name_id set not null;
alter table ent_name_cmp_tbl drop cmp_id;
alter table ent_name_cmp_tbl add cmp_id integer not null default nextval('ent_name_cmp_id_seq');
alter table ent_name_cmp_tbl drop name_uuid;
alter table ent_name_cmp_tbl drop typ_cd_uuid;
alter table ent_name_cmp_tbl add constraint ck_ent_name_cmp_typ_cd check (typ_cd_id is null or ck_is_cd_set_mem(typ_cd_id, 'NameComponentType', false));
alter table ent_name_cmp_tbl rename val_seq_id to val_id;

-- rekey address component
alter table ent_name_tbl drop constraint pk_ent_name_tbl;
alter table ent_name_tbl add constraint pk_ent_name_tbl primary key (name_id);
alter table ent_name_cmp_tbl add constraint fk_ent_name_cmp_name_id foreign key (name_id) references ent_name_tbl(name_id);

-- rekey values
alter table phon_val_tbl drop val_id;
alter table phon_val_tbl rename val_seq_id to val_id;
alter table phon_val_tbl add constraint pk_phon_val_tbl primary key (val_id);

-- stop validating relationships
drop trigger ent_rel_tbl_vrfy on ent_rel_tbl;

-- correct entity relationship table
alter table ent_rel_tbl rename src_ent_id to src_ent_uuid;
alter table ent_rel_tbl rename trg_ent_id to trg_ent_uuid;
alter table ent_rel_tbl rename rel_typ_cd_id to rel_typ_cd_uuid;
alter table ent_rel_tbl add src_ent_id integer;
alter table ent_rel_tbl add trg_ent_id integer;
alter table ent_rel_tbl add rel_typ_cd_id integer;
alter table ent_rel_tbl drop constraint ck_ent_rel_rel_type_cd;
update ent_rel_tbl set src_ent_id = (select ent_id from ent_tbl where uuid = src_ent_uuid), trg_ent_id = (select ent_id from ent_tbl where uuid = trg_ent_uuid), rel_typ_cd_id = cd_xref(rel_typ_cd_uuid);
alter table ent_rel_tbl alter src_ent_id set not null;
alter table ent_rel_tbl alter trg_ent_id set not null;
alter table ent_rel_tbl alter rel_typ_cd_id set not null;
alter table ent_rel_tbl drop rel_typ_cd_uuid;
alter table ent_rel_tbl drop src_ent_uuid;
alter table ent_rel_tbl drop trg_ent_uuid;
alter table ent_rel_tbl add constraint ck_ent_rel_rel_typ_cd check (ck_is_cd_set_mem(rel_typ_cd_id, 'EntityRelationshipType', false));


-- correct verification table
alter table ent_rel_vrfy_cdtbl rename rel_typ_cd_id to rel_typ_cd_uuid;
alter table ent_rel_vrfy_cdtbl rename src_cls_cd_id to src_cls_cd_uuid;
alter table ent_rel_vrfy_cdtbl rename trg_cls_cd_id to trg_cls_cd_uuid;
alter table ent_rel_vrfy_cdtbl add rel_typ_cd_id integer;
alter table ent_rel_vrfy_cdtbl add src_cls_cd_id integer;
alter table ent_rel_vrfy_cdtbl add trg_cls_cd_id integer;
update ent_rel_vrfy_cdtbl set rel_typ_cd_id = cd_xref(rel_typ_cd_uuid), src_cls_cd_id = cd_xref(src_cls_cd_uuid), trg_cls_cd_id = cd_xref(trg_cls_cd_uuid);
alter table ent_rel_vrfy_cdtbl alter rel_typ_cd_id set not null;
alter table ent_rel_vrfy_cdtbl alter src_cls_cd_id set not null;
alter table ent_rel_vrfy_cdtbl alter trg_cls_cd_id set not null;
alter table ent_rel_vrfy_cdtbl drop rel_typ_cd_uuid;
alter table ent_rel_vrfy_cdtbl drop src_cls_cd_uuid;
alter table ent_rel_vrfy_cdtbl drop trg_cls_cd_uuid;
alter table ent_rel_vrfy_cdtbl add constraint ck_ent_vrfy_rel_typ_cd check (ck_is_cd_set_mem(rel_typ_cd_id, 'EntityRelationshipType', false));
alter table ent_rel_vrfy_cdtbl add constraint ck_ent_vrfy_src_cls_cd check (ck_is_cd_set_mem(src_cls_cd_id, 'EntityClass', false));
alter table ent_rel_vrfy_cdtbl add constraint ck_ent_vrfy_trg_cls_cd check (ck_is_cd_set_mem(trg_cls_cd_id, 'EntityClass', false));

-- TRIGGER - ENSURE THAT ANY VALUE INSERTED INTO THE ENT_REL_TBL HAS THE PROPER PARENT
CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl () RETURNS TRIGGER AS $$
DECLARE 
	err_ref varchar(128)[];
BEGIN
	IF NOT EXISTS (
		SELECT * 
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

-- TRIGGER
CREATE TRIGGER ent_rel_tbl_vrfy BEFORE INSERT OR UPDATE ON ent_rel_tbl
	FOR EACH ROW EXECUTE PROCEDURE trg_vrfy_ent_rel_tbl();


-- correct authorship
alter table ent_note_tbl rename auth_ent_id to auth_ent_uuid;
alter table act_note_tbl rename auth_ent_id to auth_ent_uuid;
alter table ent_note_tbl add auth_ent_id integer;
alter table act_note_tbl add auth_ent_id integer;
update ent_note_tbl set auth_ent_id = (select ent_id from ent_tbl where uuid = auth_ent_uuid);
update act_note_tbl set auth_ent_id = (select ent_id from ent_tbl where uuid = auth_ent_uuid);
alter table ent_note_tbl alter auth_ent_id set not null;
alter table act_note_tbl alter auth_ent_id set not null;
alter table act_note_tbl drop auth_ent_uuid;
alter table ent_note_tbl drop auth_ent_uuid;

-- entity telecom table
alter table ent_tel_tbl rename use_cd_id to use_cd_uuid;
alter table ent_tel_tbl rename typ_cd_id to typ_cd_uuid;
alter table ent_tel_tbl rename ent_id to ent_uuid;
alter table ent_tel_tbl add ent_id integer;
alter table ent_tel_tbl add use_cd_id integer;
alter table ent_tel_tbl add typ_cd_id integer;
alter table ent_tel_tbl drop constraint ck_ent_tel_typ_cd;
alter table ent_tel_tbl drop constraint ck_ent_tel_use_cd;
update ent_tel_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), use_cd_id = cd_xref(use_cd_uuid), typ_cd_id = cd_xref(typ_cd_uuid);
alter table ent_tel_tbl alter ent_id set not null;
alter table ent_tel_tbl alter use_cd_id set not null;
alter table ent_tel_tbl drop use_cd_uuid cascade;
alter table ent_tel_tbl drop typ_cd_uuid;
alter table ent_tel_tbl drop ent_uuid;
alter table ent_tel_tbl add constraint ck_ent_tel_typ_cd check (typ_cd_id is null or ck_is_cd_set_mem(typ_cd_id, 'TelecomAddressType',true));
alter table ent_tel_tbl add constraint ck_ent_tel_use_cd check (ck_is_cd_set_mem(use_cd_id, 'TelecomAddressUse', false));

-- ASSRERT ENTITY IS A PARTICULAR CLASS
CREATE OR REPLACE FUNCTION IS_ENT_CLS(
	ENT_ID_IN IN INTEGER,
	CLS_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS 
$$
BEGIN
	RETURN (SELECT COUNT(*) FROM ENT_TBL INNER JOIN CD_CUR_VRSN ON (ENT_TBL.CLS_CD_ID = CD_CUR_VRSN.CD_ID) WHERE ENT_ID = ENT_ID_IN AND CD_CUR_VRSN.MNEMONIC = CLS_MNEMONIC_IN) > 0;
END
$$ LANGUAGE PLPGSQL;

-- place service
alter table plc_svc_tbl rename ent_id to ent_uuid;
alter table plc_svc_tbl rename svc_cd_id to svc_cd_uuid;
alter table plc_svc_tbl add ent_id integer;
alter table plc_svc_tbl add svc_cd_id integer;
alter table plc_svc_tbl drop constraint ck_plc_svc_cd;
alter table plc_svc_tbl drop constraint ck_plc_svc_ent;
update plc_svc_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), svc_cd_id = cd_xref(svc_cd_uuid);
alter table plc_svc_tbl alter ent_id set not null;
alter table plc_svc_tbl alter svc_cd_id set not null;
alter table plc_svc_tbl drop ent_uuid;
alter table plc_svc_tbl drop svc_cd_uuid;
alter table plc_svc_tbl add constraint ck_plc_svc_cd check (ck_is_cd_set_mem(svc_cd_id, 'ServiceCode', false));
alter table plc_svc_tbl add constraint ck_plc_svc_ent_cls check (is_ent_cls(ent_id, 'ServiceDeliveryLocation'));

-- correct participation
alter table act_ptcpt_tbl rename act_id to act_uuid;
alter table act_ptcpt_tbl rename ent_id to ent_uuid;
alter table act_ptcpt_tbl rename rol_cd_id to rol_cd_uuid;
alter table act_ptcpt_tbl add act_id integer;
alter table act_ptcpt_tbl add ent_id integer;
alter table act_ptcpt_tbl add rol_cd_id integer;
alter table act_ptcpt_tbl drop constraint ck_act_ptcpt_rol_cd;
update act_ptcpt_tbl set act_id = (select act_id from act_tbl where uuid = act_uuid), ent_id = (select ent_id from ent_tbl where uuid = ent_uuid), rol_cd_id = cd_xref(rol_cd_uuid);
alter table act_ptcpt_tbl alter act_id set not null;
alter table act_ptcpt_tbl alter ent_id set not null;
alter table act_ptcpt_tbl alter rol_cd_id set not null;
alter table act_ptcpt_tbl drop act_uuid;
alter table act_ptcpt_tbl drop ent_uuid;
alter table act_ptcpt_tbl drop rol_cd_uuid;
alter table act_ptcpt_tbl add constraint ck_act_ptcpt_rol_cd check (ck_is_cd_set_mem(rol_cd_id, 'ActParticipationType', true));

-- person language tables
alter table psn_lng_tbl rename ent_id to ent_uuid;
alter table psn_lng_tbl add ent_id integer;
update psn_lng_tbl set ent_id = (select ent_id from ent_tbl where uuid = ent_uuid);
alter table psn_lng_tbl alter ent_id set not null;
alter table psn_lng_tbl drop ent_uuid;

-- correct ent reference keys
alter table ent_tbl drop constraint pk_ent_tbl;
alter table ent_tbl add constraint pk_ent_tbl primary key (ent_id);
create unique index ent_uuid_idx on ent_tbl(uuid);
alter table ent_tel_tbl add constraint fk_ent_tel_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_vrsn_tbl add constraint fk_ent_vrsn_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_addr_tbl add constraint fk_ent_addr_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_note_tbl add constraint fk_ent_note_tbl_auth_id foreign key (auth_ent_id) references ent_tbl(ent_id);
alter table act_note_tbl add constraint fk_ent_note_tbl_auth_id foreign key (auth_ent_id) references ent_tbl(ent_id);
alter table ent_id_tbl add constraint fk_ent_id_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_pol_assoc_tbl add constraint fk_ent_pol_assoc_act foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_tag_tbl add constraint fk_ent_tag_act foreign key (ent_id) references ent_tbl(ent_id);
alter table plc_svc_tbl add constraint fk_plc_svc_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table act_ptcpt_tbl add constraint fk_act_ptcpt_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table psn_lng_tbl add constraint fk_psn_lng_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_name_tbl add constraint fk_ent_name_ent_id foreign key (ent_id) references ent_tbl(ent_id);
alter table ent_pol_assoc_tbl add constraint fk_ent_pol_assoc_ent_id foreign key (ent_id) references ent_tbl(ent_id);

-- correct act reference keys
alter table act_tbl drop constraint pk_act;
alter table act_tbl add constraint pk_act_tbl primary key (act_id);
create unique index act_uuid_idx on act_tbl(uuid);
alter table ent_vrsn_tbl add constraint fk_ent_vrsn_act_crt_id foreign key (crt_act_id) references act_tbl(act_id);
alter table act_ext_tbl add constraint fk_act_ext_act_id foreign key (act_id) references act_tbl(act_id);
alter table act_note_tbl add constraint fk_act_note_act foreign key (act_id) references act_tbl(act_id);
alter table act_proto_assoc_tbl add constraint fk_act_proto_assoc_act foreign key (act_id) references act_tbl(act_id);
alter table act_id_tbl add constraint fk_act_id_act_id foreign key (act_id) references act_tbl(act_id);
alter table act_pol_assoc_tbl add constraint fk_act_pol_assoc_act foreign key (act_id) references act_tbl(act_id);
alter table act_tag_tbl add constraint fk_act_tag_act foreign key (act_id) references act_tbl(act_id);
alter table act_vrsn_tbl add constraint fk_act_vrsn_act_id foreign key (act_id) references act_tbl(act_id);
alter table act_ptcpt_tbl add constraint fk_act_ptcpt_act_id foreign key (act_id) references act_tbl(act_id);

-- fix procedures
alter table proc_tbl rename mth_cd_id to mth_cd_uuid;
alter table proc_tbl rename apr_ste_cd_id to apr_ste_cd_uuid;
alter table proc_tbl rename trg_ste_cd_id to trg_ste_cd_uuid;
alter table proc_tbl add mth_cd_id integer;
alter table proc_tbl add apr_ste_cd_id integer;
alter table proc_tbl add trg_ste_cd_id integer;
alter table proc_tbl drop constraint ck_proc_apr_ste_cd_id;
alter table proc_tbl drop constraint ck_proc_mth_cd_id;
alter table proc_tbl drop constraint ck_proc_trg_ste_cd_id;
update proc_tbl set mth_cd_id = cd_xref(mth_cd_uuid), apr_ste_cd_id = cd_xref(apr_ste_cd_uuid), trg_ste_cd_id = cd_xref(trg_ste_cd_uuid);
alter table proc_tbl drop trg_ste_cd_uuid;
alter table proc_tbl drop mth_cd_uuid;
alter table proc_tbl drop apr_ste_cd_uuid;
alter table proc_tbl add constraint ck_proc_apr_ste_cd_id check (apr_ste_cd_id is null or ck_is_cd_set_mem(apr_ste_cd_id, 'BodySiteOrSystemCode', true));
alter table proc_tbl add constraint ck_proc_mth_cd_id check (mth_cd_id is null or ck_is_cd_set_mem(mth_cd_id, 'ProcedureTechniqueCode', true));
alter table proc_tbl add constraint ck_proc_trg_ste_cd_id check (trg_ste_cd_id is null or ck_is_cd_set_mem(trg_ste_cd_id, 'BodySiteOrSystemCode', true));


-- fix discharge 
alter table pat_enc_tbl rename dsch_dsp_cd_id to dsch_dsp_cd_uuid;
alter table pat_enc_tbl add dsch_dsp_cd_id integer;
update pat_enc_tbl set dsch_dsp_cd_id = cd_xref(dsch_dsp_cd_uuid);
alter table pat_enc_tbl drop dsch_dsp_cd_uuid;

-- fix material form code
alter table mat_tbl rename frm_cd_id to frm_cd_uuid;
alter table mat_tbl rename qty_cd_id to qty_cd_uuid;
alter table mat_tbl add frm_cd_id integer;
alter table mat_tbl add qty_cd_id integer;
alter table mat_tbl drop constraint ck_mat_frm_cd;
alter table mat_tbl drop constraint ck_mat_qty_cd;
update mat_tbl set frm_cd_id = cd_xref(frm_cd_uuid), qty_cd_id = cd_xref(qty_cd_uuid);
alter table mat_tbl drop frm_cd_uuid;
alter table mat_tbl drop qty_cd_uuid;
alter table mat_tbl add constraint ck_mat_frm_cd_id check (frm_cd_id is null or assrt_cd_cls(frm_cd_id, 'Form'));
alter table mat_tbl add constraint ck_mat_qty_cd_id check (qty_cd_id is null or assrt_cd_cls(qty_cd_id, 'UnitOfMeasure'));

-- fix gender code
alter table pat_tbl rename gndr_cd_id to gndr_cd_uuid;
alter table pat_tbl rename mrtl_sts_cd_id to mrtl_sts_cd_uuid;
alter table pat_tbl rename edu_lvl_cd_id to edu_lvl_cd_uuid;
alter table pat_tbl rename lvn_arg_cd_id to lvn_arg_cd_uuid;
alter table pat_tbl rename rlgn_cd_id to rlgn_cd_uuid;
alter table pat_tbl rename eth_grp_cd_id to eth_grp_cd_uuid;
alter table pat_tbl add gndr_cd_id integer;
alter table pat_tbl add mrtl_sts_cd_id integer;
alter table pat_tbl add edu_lvl_cd_id integer;
alter table pat_tbl add lvn_arg_cd_id integer;
alter table pat_tbl add rlgn_cd_id integer;
alter table pat_tbl add eth_grp_cd_id integer;
alter table pat_tbl drop constraint ck_pat_edu_lvl_cd ;
alter table pat_tbl drop constraint ck_pat_eth_grp_cd ;
alter table pat_tbl drop constraint ck_pat_gndr_cd ;
alter table pat_tbl drop constraint ck_pat_lvn_arg_cd ;
alter table pat_tbl drop constraint ck_pat_mrtl_sts_cd ;
alter table pat_tbl drop constraint ck_pat_rlgn_cd ;
alter table pat_tbl drop constraint pat_tbl_dcsd_prec_check ;
update pat_tbl set
	gndr_cd_id = cd_xref(gndr_cd_uuid),
	mrtl_sts_cd_id = cd_xref(mrtl_sts_cd_uuid),
	edu_lvl_cd_id = cd_xref(edu_lvl_cd_uuid),
	lvn_arg_cd_id = cd_xref(lvn_arg_cd_uuid),
	rlgn_cd_id = cd_xref(rlgn_cd_uuid),
	eth_grp_cd_id = cd_xref(eth_grp_cd_uuid);
alter table pat_tbl drop gndr_cd_uuid;
alter table pat_tbl drop mrtl_sts_cd_uuid;
alter table pat_tbl drop edu_lvl_cd_uuid;
alter table pat_tbl drop lvn_arg_cd_uuid;
alter table pat_tbl drop rlgn_cd_uuid;
alter table pat_tbl drop eth_grp_cd_uuid;
alter table pat_tbl add constraint ck_pat_edu_lvl_cd check (edu_lvl_cd_id is null or is_cd_set_mem(edu_lvl_cd_id, 'EducationLevel'));
alter table pat_tbl add constraint ck_pat_eth_grp_cd check (eth_grp_cd_id is null or is_cd_set_mem(eth_grp_cd_id, 'Ethnicity'));
alter table pat_tbl add constraint ck_pat_gndr_cd check (ck_is_cd_set_mem(gndr_cd_id, 'AdministrativeGenderCode', true));
alter table pat_tbl add constraint ck_pat_lvn_arg_cd check (lvn_arg_cd_id is null or is_cd_set_mem(lvn_arg_cd_id, 'LivingArrangement'));
alter table pat_tbl add constraint ck_pat_mrtl_sts_cd check (mrtl_sts_cd_id is null or is_cd_set_mem(mrtl_sts_cd_id, 'MaritalStatus'));
alter table pat_tbl add constraint ck_pat_rlgn_cd check (rlgn_cd_id is null or is_cd_set_mem(rlgn_cd_id, 'Reltion'));
alter table pat_tbl add constraint pat_tbl_dcsd_prec_check check (dcsd_prec IN ('Y', 'M', 'D'));

-- provider 
alter table pvdr_tbl rename spec_cd_id to spec_cd_uuid;
alter table pvdr_tbl add spec_cd_id integer;
update pvdr_tbl set spec_cd_id = cd_xref(spec_cd_uuid);
alter table pvdr_tbl drop spec_cd_uuid;

-- organizations
alter table org_tbl rename ind_cd_id to ind_cd_uuid;
alter table org_tbl add ind_cd_id integer;
alter table org_tbl drop constraint ck_org_ind_cd;
update org_tbl set ind_cd_id = cd_xref(ind_cd_uuid);
alter table org_tbl drop ind_cd_uuid;
alter table org_tbl add constraint ck_org_ind_cd check (ind_cd_id is null or ck_is_cd_set_mem(ind_cd_id, 'IndustryCode', true));

-- coded obs value
alter table cd_obs_tbl rename val_cd_id to val_cd_uuid;
alter table cd_obs_tbl add val_cd_id integer;
update cd_obs_tbl set val_cd_id = cd_xref(val_cd_uuid);
alter table cd_obs_tbl drop val_cd_uuid;

-- quantity value
alter table qty_obs_tbl rename uom_cd_id to uom_cd_uuid;
alter table qty_obs_tbl add uom_cd_id integer;
alter table qty_obs_tbl drop constraint ck_qty_obs_uom_cd;
update qty_obs_tbl set uom_cd_id = cd_xref(uom_cd_uuid);
alter table qty_obs_tbl alter uom_cd_id set not null;
alter table qty_obs_tbl drop uom_cd_uuid;
alter table qty_obs_tbl add constraint ck_qty_obs_uom_cd check (assrt_cd_cls(uom_cd_id, 'UnitOfMeasure'));

-- interpretation
alter table obs_tbl rename int_cd_id to int_cd_uuid;
alter table obs_tbl add int_cd_id integer;
alter table obs_tbl drop constraint ck_obs_int_cd;
update obs_tbl set int_cd_id = cd_xref(int_cd_uuid);
alter table obs_tbl drop int_cd_uuid;
alter table obs_tbl add constraint ck_obs_int_cd check (int_cd_id is null or ck_is_cd_set_mem(int_cd_id, 'ActInterpretation', true));

-- Identity type codes
alter table id_typ_tbl rename typ_cd_id to typ_cd_uuid;
alter table id_typ_tbl rename ent_scp_cd_id to ent_scp_cd_uuid;
alter table id_typ_tbl add typ_cd_id integer;
alter table id_typ_tbl add ent_scp_cd_id integer;
update id_typ_tbl set typ_cd_id = cd_xref(typ_cd_uuid), ent_scp_cd_id = cd_xref(ent_scp_cd_uuid);
alter table id_typ_tbl alter typ_cd_id set not null;
alter table id_typ_tbl alter ent_scp_cd_id set not null;
alter table id_typ_tbl drop typ_cd_uuid;
alter table id_typ_tbl drop ent_scp_cd_uuid;

-- fix assigning authority scope
alter table asgn_aut_scp_tbl rename cd_id to cd_uuid;
alter table asgn_aut_scp_tbl add cd_id integer;
update asgn_aut_scp_tbl set cd_id = cd_xref(cd_uuid);
alter table asgn_aut_scp_tbl alter cd_id set not null;
alter table asgn_aut_scp_tbl drop cd_uuid;

-- fix source/target relationship
alter table cd_rel_assoc_tbl rename src_cd_id to src_cd_uuid;
alter table cd_rel_assoc_tbl rename trg_cd_id to trg_cd_uuid;
alter table cd_rel_assoc_tbl add src_cd_id integer;
alter table cd_rel_assoc_tbl add trg_cd_id integer;
update cd_rel_assoc_tbl set src_cd_id = cd_xref(src_cd_uuid), trg_cd_id = cd_xref(trg_cd_uuid);
alter table cd_rel_assoc_tbl alter src_cd_id set not null;
alter table cd_rel_assoc_tbl alter trg_cd_id set not null;
alter table cd_rel_assoc_tbl drop src_cd_uuid;
alter table cd_rel_assoc_tbl drop trg_cd_uuid;

-- code name
alter table cd_name_tbl rename cd_id to cd_uuid;
alter table cd_name_tbl add cd_id integer;
update cd_name_tbl set cd_id = cd_xref(cd_uuid);
alter table cd_name_tbl alter cd_id set not null;
alter table cd_name_tbl drop cd_uuid;

-- reference terms
create sequence ref_term_id_seq start with 1 increment by 1;
alter table ref_term_tbl rename ref_term_id to uuid;
alter table ref_term_tbl add ref_term_id integer not null default nextval('ref_term_id_seq');
alter table ref_term_name_tbl rename ref_term_id to ref_term_uuid;
alter table ref_term_name_tbl add ref_term_id integer;
update ref_term_name_tbl set ref_term_id = (select ref_term_id from ref_term_tbl where uuid = ref_term_uuid);
alter table ref_term_name_tbl alter ref_term_id set not null;
alter table ref_term_name_tbl drop ref_term_uuid;

-- code ref term assoc
alter table cd_ref_term_assoc_tbl rename cd_id to cd_uuid;
alter table cd_ref_term_assoc_tbl add cd_id integer;
alter table cd_ref_term_assoc_tbl rename ref_term_id to ref_term_uuid;
alter table cd_ref_term_assoc_Tbl add ref_term_id integer;
update cd_ref_term_assoc_tbl set cd_id = cd_xref(cd_uuid), ref_term_id = (select ref_term_id from ref_term_tbl where uuid = ref_term_uuid);
alter table cd_ref_term_assoc_tbl alter cd_id set not null;
alter table cd_ref_term_assoc_tbl alter ref_term_id set not null;
alter table cd_ref_term_assoc_tbl drop cd_uuid;
alter table cd_ref_term_assoc_tbl drop ref_term_uuid;

-- fix ref term
alter table ref_term_tbl drop constraint pk_ref_term_tbl;
alter table ref_term_tbl add constraint pk_ref_term_tbl primary key (ref_term_id);
alter table ref_term_name_tbl add constraint fk_ref_term_name_ref_term foreign key (ref_term_id) references ref_term_tbl (ref_term_id);
alter table cd_ref_term_assoc_tbl add constraint fk_ref_term_assoc_ref_term_id foreign key (ref_term_id) references ref_term_tbl (ref_term_id);

-- code identifier
alter table cd_vrsn_tbl rename sts_cd_id to sts_cd_uuid;
alter table cd_vrsn_tbl add sts_cd_id integer;
alter table cd_vrsn_tbl drop constraint ck_cd_vrsn_sts_cd_id;
update cd_vrsn_tbl set sts_cd_id = cd_xref(sts_cd_uuid);
alter table cd_vrsn_tbl alter sts_cd_id set not null;
alter table cd_vrsn_tbl drop sts_cd_uuid;
alter table cd_vrsn_tbl add constraint ck_cd_vrsn_sts_cd_id check (ck_is_cd_set_mem(sts_cd_id, 'ConceptStatus', false));

-- fix keys on 
alter table cd_tbl drop constraint pk_cd_tbl;
alter table cd_tbl add constraint pk_cd_tbl primary key (cd_id);
alter table cd_vrsn_tbl add constraint fk_cd_vrsn_id foreign key (cd_id) references cd_tbl(cd_id);
create unique index cd_uuid_idx on cd_tbl(uuid);

-- fix foreign keys to use integers
alter table cd_set_mem_assoc_tbl add constraint fk_cd_set_mem_cd_id foreign key (cd_id) references cd_tbl(cd_id);
alter table ent_tbl add constraint fk_ent_cls_cd_id foreign key (cls_cd_id) references cd_tbl(cd_id);
alter table ent_tbl add constraint fk_ent_dtr_cd_id foreign key (dtr_cd_id) references cd_tbl(cd_id);
alter table sub_adm_tbl add constraint fk_sub_adm_rte_cd_id foreign key (rte_cd_id) references cd_tbl(cd_id);
alter table sub_adm_tbl add constraint fk_sub_adm_dos_unt_cd_id foreign key (dos_unt_cd_id) references cd_tbl(cd_id);
alter table cd_Ref_term_assoc_tbl add constraint fk_ref_term_assoc_cd_id foreign key (cd_id) references cd_tbl(cd_id);
alter table act_tbl add constraint fk_act_cls_cd_id foreign key (cls_cd_id) references cd_tbl(cd_id);
alter table act_tbl add constraint fk_act_mod_cd_id foreign key (mod_cd_id) references cd_tbl(cd_id);
alter table act_rel_tbl add constraint fk_act_rel_src_act_id foreign key (src_act_id) references act_tbl(act_id);
alter table act_rel_tbl add constraint fk_act_rel_trg_act_id foreign key (trg_act_id) references act_tbl(act_id);
alter table act_rel_tbl add constraint fk_act_rel_typ_cd_id foreign key (rel_typ_cd_id) references cd_tbl(cd_id);
alter table act_vrsn_tbl add constraint fk_act_vrsn_rsn_cd_id foreign key (rsn_cd_id) references cd_tbl(cd_id);
alter table act_vrsn_tbl add constraint fk_act_vrsn_sts_cd_id foreign key (sts_cd_id) references cd_tbl(cd_id);
alter table act_vrsn_tbl add constraint fk_act_vrsn_typ_cd_id foreign key (typ_cd_id) references cd_tbl(cd_id);
alter table ent_vrsn_tbl add constraint fk_ent_vrsn_sts_cd_id foreign key (sts_cd_id) references cd_tbl(cd_id);
alter table ent_vrsn_tbl add constraint fk_ent_vrsn_typ_cd_id foreign key (typ_cd_id) references cd_tbl(cd_id);
alter table act_ptcpt_tbl add constraint fk_act_ptcpt_rol_cd foreign key (rol_cd_id) references cd_tbl(cd_id);
alter table plc_svc_tbl add constraint fk_plc_svc_cd_id foreign key (svc_cd_id) references cd_tbl(cd_id);
alter table ent_tel_tbl add constraint fk_ent_tel_use_cd foreign key (use_cd_id) references cd_tbl(cd_id);
alter table ent_tel_tbl add constraint fk_ent_tel_typ_cd foreign key (typ_cd_id) references cd_tbl(cd_id);
alter table ent_addr_tbl add constraint fk_ent_addr_use_cd foreign key (use_cd_id) references cd_tbl(cd_id);
alter table ent_rel_vrfy_cdtbl add constraint fk_ent_vrfy_rel_typ_cd foreign key (rel_typ_cd_id) references cd_tbl(cd_id);
alter table ent_rel_vrfy_cdtbl add constraint fk_ent_vrfy_src_cls_cd foreign key (src_cls_cd_id) references cd_tbl(cd_id);
alter table cd_vrsn_tbl add constraint fk_cd_vrsn_sts_cd_id foreign key (sts_cd_id) references cd_tbl(cd_id);
alter table ent_rel_vrfy_cdtbl add constraint fk_ent_vrfy_trg_cls_cd foreign key (trg_cls_cd_id) references cd_tbl(cd_id);
alter table proc_tbl add constraint fk_proc_apr_ste_cd_id foreign key (apr_ste_cd_id) references cd_tbl(cd_id);
alter table proc_tbl add constraint fk_proc_trg_ste_cd_id foreign key (trg_ste_cd_id) references cd_tbl(cd_id);
alter table proc_tbl add constraint fk_proc_mth_cd_id foreign key (mth_cd_id) references cd_tbl(cd_id);
alter table pat_enc_tbl add constraint fk_pat_enc_dsch_dsp_cd_id foreign key (dsch_dsp_cd_id) references cd_tbl(cd_id);
alter table ent_rel_tbl add constraint fk_ent_rel_rel_typ_cd foreign key (rel_typ_cd_id) references cd_tbl(cd_id);
alter table ent_addr_cmp_tbl add constraint fk_ent_addr_cmp_typ_cd_id foreign key (typ_cd_id) references cd_tbl(cd_id);
alter table mat_tbl add constraint fk_mat_frm_cd_id foreign key (frm_cd_id) references cd_tbl(cd_id);
alter table mat_tbl add constraint fk_mat_qty_cd_id foreign key (qty_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_edu_lvl_cd foreign key (edu_lvl_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_eth_grp_cd foreign key (eth_grp_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_gndr_cd foreign key (gndr_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_lvn_arg_cd foreign key (lvn_arg_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_mrtl_sts_cd foreign key (mrtl_sts_cd_id) references cd_tbl(cd_id);
alter table pat_tbl add constraint fk_pat_rlgn_cd foreign key (rlgn_cd_id) references cd_tbl(cd_id);
alter table pvdr_tbl add constraint fk_pvdr_spec_cd_id foreign key (spec_cd_id) references cd_tbl(cd_id);
alter table org_tbl add constraint fk_org_ind_cd_id foreign key (ind_cd_id) references cd_tbl(cd_id);
alter table cd_obs_tbl add constraint fk_cd_obs_val_cd_id foreign key (val_cd_id) references cd_tbl(cd_id);
alter table qty_obs_tbl add constraint fk_qty_obs_uom_cd_id foreign key (uom_cd_id) references cd_tbl(cd_id);
alter table obs_tbl add constraint fk_obs_int_cd_id foreign key (int_cd_id) references cd_tbl(cd_id);
alter table id_typ_tbl add constraint fk_id_typ_typ_cd_id foreign key (typ_cd_id) references cd_tbl(cd_id);
alter table id_typ_tbl add constraint fk_id_typ_ent_scp_cd_id foreign key (ent_scp_cd_id) references cd_tbl(cd_id);
alter table asgn_aut_scp_tbl add constraint fk_asgn_aut_scp_cd_id foreign key (cd_id) references cd_tbl(cd_id);
alter table cd_rel_assoc_tbl add constraint fk_cd_rel_src_cd_id foreign key (src_cd_id) references cd_tbl(cd_id);
alter table cd_rel_assoc_tbl add constraint fk_cd_rel_trg_cd_id foreign key (trg_cd_id) references cd_tbl(cd_id);
alter table cd_name_tbl add constraint fk_cd_name_cd_id foreign key (cd_id) references cd_tbl(cd_id);

create unique index act_rel_unq_enf on act_rel_tbl (src_act_id, trg_act_id, rel_typ_cd_id) where obslt_vrsn_seq_id is null;
create unique index act_ptcpt_unq_enf on act_ptcpt_tbl (act_id, ent_id, rol_cd_id) where obslt_vrsn_seq_id is null;
create index act_rel_typ_cd_id_idx on act_rel_tbl (rel_typ_cd_id);

-- person table
alter table psn_tbl add vrsn_seq_id integer;
update psn_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = psn_tbl.ent_vrsn_id);
alter table psn_tbl alter vrsn_seq_id set not null;
alter table psn_tbl add constraint fk_psn_ent_vrsn_seq_id foreign key (vrsn_seq_id) references ent_vrsn_tbl (vrsn_seq_id);
create unique index psn_vrsn_seq_idx on psn_tbl(vrsn_seq_id);

-- patient table
alter table pat_tbl add vrsn_seq_id integer;
update pat_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = pat_tbl.ent_vrsn_id);
alter table pat_tbl alter vrsn_seq_id set not null;
alter table pat_tbl add constraint fk_pat_psn_vrsn_seq_id foreign key (vrsn_seq_id) references psn_tbl (vrsn_seq_id);

-- application entity table
alter table app_ent_tbl add vrsn_seq_id integer;
update app_ent_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = app_ent_tbl.ent_vrsn_id);
alter table app_ent_tbl alter vrsn_seq_id set not null;
alter table app_ent_tbl add constraint fk_app_ent_ent_vrsn_seq_id foreign key (vrsn_seq_id) references ent_vrsn_tbl (vrsn_seq_id);

-- device entity table
alter table dev_ent_tbl add vrsn_seq_id integer;
update dev_ent_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = dev_ent_tbl.ent_vrsn_id);
alter table dev_ent_tbl alter vrsn_seq_id set not null;
alter table dev_ent_tbl add constraint fk_dev_ent_ent_vrsn_seq_id foreign key (vrsn_seq_id) references ent_vrsn_tbl (vrsn_seq_id);

-- user entity table
alter table usr_ent_tbl add vrsn_seq_id integer;
update usr_ent_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = usr_ent_tbl.ent_vrsn_id);
alter table usr_ent_tbl alter vrsn_seq_id set not null;
alter table usr_ent_tbl add constraint fk_dev_ent_ent_vrsn_seq_id foreign key (vrsn_seq_id) references psn_tbl (vrsn_seq_id);

-- place table
alter table plc_tbl add vrsn_seq_id integer;
update plc_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = plc_tbl.ent_vrsn_id);
alter table plc_tbl alter vrsn_seq_id set not null;
alter table plc_tbl add constraint fk_plc_psn_vrsn_seq_id foreign key (vrsn_seq_id) references ent_vrsn_tbl (vrsn_seq_id);

-- material table
alter table mat_tbl add vrsn_seq_id integer;
update mat_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = mat_tbl.ent_vrsn_id);
alter table mat_tbl alter vrsn_seq_id set not null;
alter table mat_tbl add constraint fk_mat_psn_vrsn_seq_id foreign key (vrsn_seq_id) references ent_vrsn_tbl (vrsn_seq_id);
create unique index mat_vrsn_seq_idx on mat_tbl(vrsn_seq_id);

-- m.material table
alter table mmat_tbl add vrsn_seq_id integer;
update mmat_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = mmat_tbl.ent_vrsn_id);
alter table mmat_tbl alter vrsn_seq_id set not null;
alter table mmat_tbl add constraint fk_mmat_psn_vrsn_seq_id foreign key (vrsn_seq_id) references mat_tbl (vrsn_seq_id);

-- provider table
alter table pvdr_tbl add vrsn_seq_id integer;
update pvdr_tbl set vrsn_seq_id = (select vrsn_seq_id from ent_vrsn_tbl where ent_vrsn_tbl.ent_vrsn_id = pvdr_tbl.ent_vrsn_id);
alter table pvdr_tbl alter vrsn_seq_id set not null;
alter table pvdr_tbl add constraint fk_prov_psn_vrsn_seq_id foreign key (vrsn_seq_id) references psn_tbl (vrsn_seq_id);

-- alter obs table
alter table act_vrsn_tbl rename act_vrsn_id to vrsn_uuid;

alter table obs_tbl add vrsn_seq_id integer;
update obs_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table obs_tbl alter vrsn_seq_id set not null;
alter table obs_tbl add constraint fk_obs_vrsn_seq_id foreign key (vrsn_seq_id) references act_vrsn_tbl(vrsn_seq_id);
create unique index obs_vrsn_seq_id_idx on obs_tbl(vrsn_seq_id);

alter table cd_obs_tbl add vrsn_seq_id integer;
update cd_obs_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table cd_obs_tbl alter vrsn_seq_id set not null;
alter table cd_obs_tbl add constraint fk_cd_obs_vrsn_seq_id foreign key (vrsn_seq_id) references obs_tbl(vrsn_seq_id);

alter table txt_obs_tbl add vrsn_seq_id integer;
update txt_obs_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_Vrsn_id = vrsn_uuid);
alter table txt_obs_tbl alter vrsn_seq_id set not null;
alter table txt_obs_tbl add constraint fk_txt_obs_vrsn_seq_id foreign key (vrsn_seq_id) references obs_tbl(vrsn_seq_id);

alter table qty_obs_tbl add vrsn_seq_id integer;
update qty_obs_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table qty_obs_tbl alter vrsn_seq_id set not null;
alter table qty_obs_tbl add constraint fk_qty_obs_vrsn_seq_id foreign key (vrsn_seq_id) references obs_tbl(vrsn_seq_id);

alter table cntrl_act_tbl add vrsn_seq_id integer;
update cntrl_act_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table cntrl_act_tbl alter vrsn_seq_id set not null;
alter table cntrl_act_tbl add constraint fk_cntrl_act_vrsn_seq_id foreign key (vrsn_seq_id) references act_vrsn_tbl(vrsn_seq_id);

alter table pat_enc_tbl add vrsn_seq_id integer;
update pat_enc_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table pat_enc_tbl alter vrsn_seq_id set not null;
alter table pat_enc_tbl add constraint fk_pat_enc_vrsn_seq_id foreign key (vrsn_seq_id) references act_vrsn_tbl(vrsn_seq_id);

alter table sub_adm_tbl add vrsn_seq_id integer;
update sub_adm_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table sub_adm_tbl alter vrsn_seq_id set not null;
alter table sub_adm_tbl add constraint fk_sub_adm_vrsn_seq_id foreign key (vrsn_seq_id) references act_vrsn_tbl(vrsn_seq_id);

alter table proc_tbl add vrsn_seq_id integer;
update proc_tbl set vrsn_seq_id = (select vrsn_seq_id from act_vrsn_tbl where act_vrsn_id = vrsn_uuid);
alter table proc_tbl alter vrsn_seq_id set not null;
alter table proc_tbl add constraint fk_proc_vrsn_seq_id foreign key (vrsn_seq_id) references act_vrsn_tbl(vrsn_seq_id);

alter table sub_adm_tbl drop act_Vrsn_id;
alter table pat_enc_tbl drop act_Vrsn_id;
alter table cntrl_act_tbl drop act_vrsn_id;
alter table qty_obs_tbl drop act_vrsn_id;
alter table txt_obs_tbl drop act_vrsn_id;
alter table cd_obs_tbl drop act_Vrsn_id;
alter table obs_tbl drop act_vrsn_id;
alter table proc_tbl drop act_Vrsn_id;
alter table sub_adm_tbl add constraint pk_sub_adm_tbl primary key (vrsn_seq_id);
alter table pat_enc_tbl add constraint pk_pat_enc_tbl primary key (vrsn_seq_id);
alter table cntrl_act_tbl add constraint pk_cntrl_act_tbl  primary key (vrsn_seq_id);
alter table qty_obs_tbl add constraint pk_qty_obs_tbl primary key (vrsn_seq_id);
alter table txt_obs_tbl add constraint pk_txt_obs_tbl primary key (vrsn_seq_id);
alter table cd_obs_tbl add constraint pk_obs_tbl primary key (vrsn_seq_id);
alter table proc_tbl add constraint pk_proc_tbl primary key (vrsn_seq_id);

create index ent_rel_src_seq_id_idx on ent_rel_tbl(src_ent_id);
create index ent_rel_trg_seq_id_idx on ent_rel_tbl(trg_ent_id);
create unique index ent_rel_unq_enf on ent_rel_tbl(src_ent_id, trg_ent_id, rel_typ_cd_id) where obslt_vrsn_seq_id is null;

-- derived tables
alter table pat_tbl drop constraint fk_pat_ent_vrsn_id;
alter table pat_tbl drop constraint pk_pat_tbl;
alter table pat_tbl drop ent_vrsn_id cascade;
alter table pat_tbl add constraint pk_pat_tbl primary key (vrsn_seq_id);
alter table mmat_tbl drop ent_vrsn_id cascade;
alter table mmat_tbl add constraint pk_mmat_tbl primary key (vrsn_seq_id);
alter table mat_tbl drop ent_vrsn_id cascade;
alter table mat_tbl add constraint pk_mat_tbl primary key (vrsn_seq_id);
alter table plc_tbl drop ent_vrsn_id cascade;
alter table plc_tbl add constraint pk_plc_tbl primary key (vrsn_seq_id);
alter table pvdr_tbl drop constraint fk_pvdr_ent_vrsn_id;
alter table pvdr_tbl drop constraint pk_pvdr_tbl;
alter table pvdr_tbl drop ent_vrsn_id cascade;
alter table pvdr_tbl add constraint pk_pvdr_tbl primary key (vrsn_seq_id);
alter table usr_ent_tbl drop constraint fk_usr_ent_vrsn_id;
alter table usr_ent_tbl drop constraint pk_usr_ent_tbl;
alter table usr_ent_tbl drop ent_vrsn_id cascade;
alter table usr_ent_tbl add constraint pk_usr_ent_tbl primary key (vrsn_seq_id);
alter table psn_tbl drop constraint fk_psn_ent_vrsn_id;
alter table psn_tbl drop constraint pk_psn_tbl;
alter table psn_tbl drop ent_vrsn_id cascade;
alter table psn_tbl add constraint pk_psn_tbl primary key (vrsn_seq_id);
alter table dev_ent_tbl drop ent_vrsn_id cascade;
alter table dev_ent_tbl add constraint pk_dev_ent_tbl primary key (vrsn_seq_id);
alter table app_ent_tbl drop ent_vrsn_id cascade;
alter table app_ent_tbl add constraint pk_app_ent_tbl primary key (vrsn_seq_id);
create unique index ent_vrsn_ent_id_idx on ent_vrsn_tbl (ent_id) where (obslt_utc is null);
alter table ent_vrsn_tbl rename ent_vrsn_id to vrsn_uuid;

alter table ent_pol_assoc_tbl alter efft_vrsn_seq_id type integer;
alter table ent_pol_assoc_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_rel_tbl alter efft_vrsn_seq_id type integer;
alter table ent_rel_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_addr_tbl alter efft_vrsn_seq_id type integer;
alter table ent_addr_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_ext_tbl alter efft_vrsn_seq_id type integer;
alter table ent_ext_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_id_tbl alter efft_vrsn_seq_id type integer;
alter table ent_id_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_name_tbl alter efft_vrsn_seq_id type integer;
alter table ent_name_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_note_tbl alter efft_vrsn_seq_id type integer;
alter table ent_note_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_rel_tbl alter efft_vrsn_seq_id type integer;
alter table ent_rel_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_tel_tbl alter efft_vrsn_seq_id type integer;
alter table ent_tel_tbl alter obslt_vrsn_seq_id type integer;
alter table plc_svc_tbl alter efft_vrsn_seq_id type integer;
alter table plc_svc_tbl alter obslt_vrsn_seq_id type integer;
alter table psn_lng_tbl alter efft_vrsn_seq_id type integer;
alter table psn_lng_tbl alter obslt_vrsn_seq_id type integer;
alter table ent_vrsn_tbl alter vrsn_seq_id type integer;

alter table act_id_tbl alter efft_vrsn_seq_id type integer;
alter table act_id_tbl alter obslt_vrsn_seq_id type integer;
alter table act_ext_tbl alter efft_vrsn_seq_id type integer;
alter table act_ext_tbl alter obslt_vrsn_seq_id type integer;
alter table act_rel_tbl alter efft_vrsn_seq_id type integer;
alter table act_rel_tbl alter obslt_vrsn_seq_id type integer;
alter table act_ptcpt_tbl alter efft_vrsn_seq_id type integer;
alter table act_ptcpt_tbl alter obslt_vrsn_seq_id type integer;
alter table act_pol_assoc_tbl alter efft_vrsn_seq_id type integer;
alter table act_pol_assoc_tbl alter obslt_vrsn_seq_id type integer;
alter table act_note_tbl alter efft_vrsn_seq_id type integer;
alter table act_note_tbl alter obslt_vrsn_seq_id type integer;

alter table act_vrsn_tbl alter vrsn_seq_id type integer;

create index act_ext_act_id on act_ext_tbl(act_id);
create index act_id_act_id_idx on act_id_tbl(act_id);
create index act_note_act_id_idx on act_note_tbl(act_id);
create index act_pol_assoc_act_id on act_pol_assoc_tbl(act_id);
create index act_proto_assoc_act_id on act_proto_assoc_tbl(act_id);
create index act_ptcpt_act_id_idx on act_ptcpt_tbl(act_id);
create index act_ptcpt_ent_id_idx on act_ptcpt_tbl(ent_id);
create index act_ptcpt_rol_typ_id_idx on act_ptcpt_tbl(rol_cd_id);
create index act_rel_src_id_idx on act_rel_tbl(src_act_id);
create index act_rel_trg_id_idx on act_rel_tbl(trg_act_id);
create index act_tag_act_id_idx on act_tag_tbl(act_id);
create unique index act_vrsn_act_id_idx on act_vrsn_tbl(act_id) where (obslt_utc is null);
create index act_vrsn_typ_id_idx on act_vrsn_tbl(typ_cd_id);

alter table asgn_aut_scp_tbl add constraint pk_asgn_aut_scp_tbl primary key (aut_id, cd_id);
create index cd_name_cd_id_idx on cd_name_tbl(cd_id);
create index cd_Ref_term_assoc_cd_id_idx on cd_ref_term_assoc_tbl(cd_id);
create index cd_rel_assoc_src_id_idx on cd_rel_assoc_tbl(src_cd_id);
create index cd_rel_assoc_trg_id_idx on cd_rel_assoc_tbl(trg_cd_id);
create index cd_set_mem_assoc_cd_id_idx on cd_set_mem_assoc_tbl(cd_id);
create index cd_set_mem_assoc_set_id_idx on cd_set_mem_assoc_tbl(set_id);
create unique index cd_vrsn_cd_id_idx on cd_vrsn_tbl(cd_id) where (obslt_utc is null);
create index ent_addr_cmp_typ_cd_idx on ent_addr_cmp_tbl(typ_cd_id);
create index ent_addr_cmp_addr_id_idx on ent_addr_cmp_tbl(addr_id);
create index ent_addr_ent_id_idx on ent_addr_tbl(ent_id);
create index ent_ext_ent_id_idx on ent_ext_tbl(ent_id);
create index ent_id_ent_id_idx on ent_id_tbl(ent_id);
create index ent_name_cmp_name_id_idx on ent_name_cmp_tbl(name_id);
create index ent_name_ent_id_idx on ent_name_tbl(ent_id);
create index ent_note_ent_id_idx on ent_note_tbl(ent_id);
create index ent_pol_assoc_ent_id_idx on ent_pol_assoc_tbl(ent_id);
create index ent_rel_src_ent_id_idx on ent_rel_tbl(src_ent_id);
create index ent_rel_trg_ent_id_idx on ent_rel_tbl(trg_ent_id);
create index ent_rel_rel_typ_id_idx on ent_rel_tbl(rel_typ_cd_id);
create index ent_tag_ent_id_idx on ent_tag_tbl(ent_id);
create index ent_cls_cd_id_idx on ent_tbl(cls_cd_id);
create index ent_tel_ent_id_idx on ent_tel_tbl(ent_id);
create index pat_gndr_cs_id_idx on pat_tbl(gndr_cd_id);
create index plc_svc_ent_id_idx on plc_svc_tbl(ent_id);
create index psn_lng_ent_id_idx on psn_lng_tbl(ent_id);
create unique index ent_addr_uuid_idx on ent_addr_tbl(addr_uuid);
create unique index ent_name_uuid_idx on ent_name_tbl(name_uuid);
alter table ent_addr_cmp_tbl alter val_id type integer;
alter table ent_addr_cmp_val_tbl alter val_id type integer;
alter table ent_name_cmp_tbl drop cmp_seq;
alter table ent_name_cmp_tbl alter val_id type integer;
alter table phon_val_tbl alter val_id type integer;
alter table cd_vrsn_tbl rename cd_vrsn_id to vrsn_uuid;
alter table act_ptcpt_tbl alter ptcpt_seq_id type integer;
alter table act_proto_assoc_tbl add constraint pk_act_proto_assoc_tbl primary key (proto_id, act_id);

ALTER TABLE SEC_APP_TBL ADD LOCKED TIMESTAMPTZ; -- LOCKOUT PERIOD
ALTER TABLE SEC_APP_TBL ADD FAIL_AUTH INTEGER; -- FAILED AUTHENTICATION ATTEMPTS
ALTER TABLE SEC_APP_TBL ADD LAST_AUTH_UTC TIMESTAMPTZ; -- THE LAST AUTHETNICATION TIME
ALTER TABLE SEC_APP_TBL ADD UPD_UTC TIMESTAMP; -- THE CREATION TIME OF THE APP
ALTER TABLE SEC_APP_TBL ADD UPD_PROV_ID UUID; -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
ALTER TABLE SEC_APP_TBL ADD CONSTRAINT FK_SEC_APP_UPD_PROV_ID FOREIGN KEY (UPD_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID);

ALTER TABLE SEC_DEV_TBL ADD LOCKED TIMESTAMPTZ; -- LOCKOUT PERIOD
ALTER TABLE SEC_DEV_TBL ADD FAIL_AUTH INTEGER; -- FAILED AUTHENTICATION ATTEMPTS
ALTER TABLE SEC_DEV_TBL ADD LAST_AUTH_UTC TIMESTAMPTZ; -- THE LAST AUTHETNICATION TIME
ALTER TABLE SEC_DEV_TBL ADD UPD_UTC TIMESTAMP; -- THE CREATION TIME OF THE APP
ALTER TABLE SEC_DEV_TBL ADD UPD_PROV_ID UUID; -- THE USER WHICH IS RESPONSIBLE FOR THE CREATION OF THE APP
ALTER TABLE SEC_DEV_TBL ADD CONSTRAINT FK_SEC_DEV_UPD_PROV_ID FOREIGN KEY (UPD_PROV_ID) REFERENCES SEC_PROV_TBL(PROV_ID);


DROP FUNCTION AUTH_APP (TEXT, TEXT);

-- AUTHENTICATE AN APPICATION
-- AUTHENTICATE AN APPICATION
CREATE OR REPLACE FUNCTION AUTH_APP (
	APP_PUB_ID_IN IN TEXT,
	APP_SCRT_IN IN TEXT,
	MAX_FAIL_AUTH_IN IN INTEGER
) RETURNS SETOF SEC_APP_TBL AS 
$$ 
DECLARE 
	APP_TPL SEC_APP_TBL;
BEGIN
	SELECT INTO APP_TPL * FROM SEC_APP_TBL WHERE APP_PUB_ID = APP_PUB_ID_IN LIMIT 1;
	IF (APP_TPL.LOCKED > CURRENT_TIMESTAMP) THEN
		APP_TPL.LOCKED = COALESCE(APP_TPL.LOCKED, CURRENT_TIMESTAMP) + ((APP_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
		UPDATE SEC_APP_TBL SET FAIL_AUTH = SEC_APP_TBL.FAIL_AUTH + 1, LOCKED = APP_TPL.LOCKED
			WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
		APP_TPL.APP_PUB_ID := ('ERR:AUTH_LCK:' || ((APP_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT));
		APP_TPL.APP_ID = UUID_NIL();
		APP_TPL.APP_SCRT = NULL;
		RETURN QUERY SELECT APP_TPL.*;
	ELSE
		-- LOCKOUT ACCOUNTS
		IF (APP_TPL.APP_SCRT = APP_SCRT_IN) THEN
			UPDATE SEC_APP_TBL SET 
				FAIL_AUTH = 0,
				LAST_AUTH_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8',
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			RETURN QUERY SELECT APP_TPL.*;
		ELSIF(APP_TPL.FAIL_AUTH > MAX_FAIL_AUTH_IN) THEN 
			APP_TPL.LOCKED = COALESCE(APP_TPL.LOCKED, CURRENT_TIMESTAMP) + ((APP_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
			UPDATE SEC_APP_TBL SET FAIL_AUTH = COALESCE(SEC_APP_TBL.FAIL_AUTH, 0) + 1, LOCKED = APP_TPL.LOCKED
				WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			APP_TPL.APP_PUB_ID := ('AUTH_LCK:' || ((APP_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
			APP_TPL.APP_ID := UUID_NIL();
			APP_TPL.APP_SCRT := NULL;
			RETURN QUERY SELECT APP_TPL.*;
		ELSE
			UPDATE SEC_APP_TBL SET FAIL_AUTH = COALESCE(SEC_APP_TBL.FAIL_AUTH, 0) + 1 WHERE SEC_APP_TBL.APP_PUB_ID = APP_PUB_ID_IN;
			APP_TPL.APP_PUB_ID := ('AUTH_INV:' || APP_PUB_ID_IN)::VARCHAR;
			APP_TPL.APP_ID := UUID_NIL();
			APP_TPL.APP_SCRT := NULL;			
			RETURN QUERY SELECT APP_TPL.*;
		END IF;
	END IF;

END
$$ LANGUAGE PLPGSQL;

-- AUTHENTICATE A DEVICE
DROP FUNCTION AUTH_DEV (TEXT, TEXT);
CREATE OR REPLACE FUNCTION AUTH_DEV (
	DEV_PUB_ID_IN IN TEXT,
	DEV_SCRT_IN IN TEXT,
	MAX_FAIL_AUTH_IN IN INTEGER
) RETURNS SETOF SEC_DEV_TBL AS 
$$ 
DECLARE 
	DEV_TPL SEC_DEV_TBL;
BEGIN
	SELECT INTO DEV_TPL * FROM SEC_DEV_TBL WHERE DEV_PUB_ID = DEV_PUB_ID_IN LIMIT 1;
	IF (DEV_TPL.LOCKED > CURRENT_TIMESTAMP) THEN
		DEV_TPL.LOCKED = COALESCE(DEV_TPL.LOCKED, CURRENT_TIMESTAMP) + ((DEV_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
		UPDATE SEC_DEV_TBL SET FAIL_AUTH = SEC_DEV_TBL.FAIL_AUTH + 1, LOCKED = DEV_TPL.LOCKED
			WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
		DEV_TPL.DEV_PUB_ID := ('ERR:AUTH_LCK:' || ((DEV_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT));
		DEV_TPL.DEV_ID = UUID_NIL();
		DEV_TPL.DEV_SCRT = NULL;
		RETURN QUERY SELECT DEV_TPL.*;
	ELSE
		-- LOCKOUT ACCOUNTS
		IF (DEV_TPL.DEV_SCRT = DEV_SCRT_IN) THEN
			UPDATE SEC_DEV_TBL SET 
				FAIL_AUTH = 0,
				LAST_AUTH_UTC = CURRENT_TIMESTAMP,
				UPD_PROV_ID = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8',
				UPD_UTC = CURRENT_TIMESTAMP
			WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			RETURN QUERY SELECT DEV_TPL.*;
		ELSIF(DEV_TPL.FAIL_AUTH > MAX_FAIL_AUTH_IN) THEN 
			DEV_TPL.LOCKED = COALESCE(DEV_TPL.LOCKED, CURRENT_TIMESTAMP) + ((DEV_TPL.FAIL_AUTH - MAX_FAIL_AUTH_IN) ^ 1.5 * '30 SECONDS'::INTERVAL);
			UPDATE SEC_DEV_TBL SET FAIL_AUTH = COALESCE(SEC_DEV_TBL.FAIL_AUTH, 0) + 1, LOCKED = DEV_TPL.LOCKED
				WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			DEV_TPL.DEV_PUB_ID := ('AUTH_LCK:' || ((DEV_TPL.LOCKED - CURRENT_TIMESTAMP)::TEXT))::VARCHAR;
			DEV_TPL.DEV_ID := UUID_NIL();
			DEV_TPL.DEV_SCRT := NULL;
			RETURN QUERY SELECT DEV_TPL.*;
		ELSE
			UPDATE SEC_DEV_TBL SET FAIL_AUTH = COALESCE(SEC_DEV_TBL.FAIL_AUTH, 0) + 1 WHERE SEC_DEV_TBL.DEV_PUB_ID = DEV_PUB_ID_IN;
			DEV_TPL.DEV_PUB_ID := ('AUTH_INV:' || DEV_PUB_ID_IN)::VARCHAR;
			DEV_TPL.DEV_ID := UUID_NIL();
			DEV_TPL.DEV_SCRT := NULL;			
			RETURN QUERY SELECT DEV_TPL.*;
		END IF;
	END IF;

END
$$ LANGUAGE PLPGSQL;



 -- GET THE SCHEMA VERSION
CREATE OR REPLACE FUNCTION GET_SCH_VRSN() RETURNS VARCHAR(10) AS
$$
BEGIN
	RETURN '1.9.0.0';
END;
$$ LANGUAGE plpgsql;

SELECT REG_PATCH('20181110-01');

 COMMIT;

