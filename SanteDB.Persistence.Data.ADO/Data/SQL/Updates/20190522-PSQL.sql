/** 
 * <update id="20190522-01" applyRange="1.0.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Add relationship "replaces" between all entities of the same class</summary>
 *	<remarks>Any entity is technically allowed to replace itself :)</remarks>
 *	<check>select ck_patch('20190522-01')</check>
 * </update>
 */

BEGIN TRANSACTION ;

ALTER TABLE ENT_EXT_TBL ALTER EXT_DISP TYPE TEXT;
ALTER TABLE ACT_EXT_TBL ALTER EXT_DISP TYPE TEXT;

-- GRANT SYSTEM LOGIN AS A SERVICE
INSERT INTO sec_rol_pol_assoc_tbl (pol_id, rol_id, pol_act) VALUES ('e15b96ab-646c-4c00-9a58-ea09eee67d7c', 'c3ae21d2-fc23-4133-ba42-b0e0a3b817d7', 2);
DROP INDEX SEC_DEV_SCRT_IDX ;--#!
SELECT REG_PATCH('20190522-01');
COMMIT;
