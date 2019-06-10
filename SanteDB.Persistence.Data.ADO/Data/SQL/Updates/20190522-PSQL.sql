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

SELECT REG_PATCH('20190522-01');
COMMIT;
