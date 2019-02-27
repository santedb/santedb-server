/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20181113-01" name="Update:20181113-01" applyRange="1.0.0.0-1.9.0.0"  invariantName="npgsql">
 *	<summary>Update: Optimize check constraints</summary>
 *	<remarks>Adds a more space efficient unique check on the act relationship, entity and participation tables</remarks>
 *	<isInstalled>select ck_patch('20181113-01')</isInstalled>
 * </feature>
 */

 BEGIN TRANSACTION;

create unique index act_ptcpt_unq_enf_sha1 on act_ptcpt_tbl(digest(act_id::text || ent_id::text || rol_cd_id::text, 'sha1'));
drop index act_ptcpt_unq_enf;
create unique index act_rel_unq_enf_sha1 on act_rel_tbl(digest(src_Act_id::text || trg_act_id::text || rel_typ_cd_id::text, 'sha1'));
drop index act_rel_unq_enf;
create unique index ent_rel_unq_enf_sha1 on ent_rel_tbl(digest(src_ent_id::text || trg_ent_id::text || rel_typ_cd_id::text, 'sha1'));
drop index ent_rel_unq_enf;
alter table alrt_tbl rename to mail_msg_tbl;
alter table mail_msg_tbl rename alrt_id to mail_msg_id;
alter table alrt_rcpt_to_tbl rename to mail_msg_rcpt_to_tbl;
alter table mail_msg_rcpt_to_tbl rename alrt_id to mail_msg_id;

SELECT REG_PATCH('20181113-01');

COMMIT;