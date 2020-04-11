/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200320-01" name="Update:20200320-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add removal of duplicate versions</summary>
 *	<remarks>This allows devices to be tracked to facilities and users</remarks>
 *	<isInstalled>select ck_patch('20200320-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

CREATE OR REPLACE FUNCTION prune_dup_vrsn() 
RETURNS VOID AS
$$
DECLARE 
	CNT INTEGER;
BEGIN
	LOOP
		RAISE INFO 'SCANNING FOR DUPLICATE VERSIONS';
		SELECT COUNT(*) INTO CNT 
		FROM
			(SELECT act_id, count(act_vrsn_id) 
			FROM ACT_VRSN_TBL 
			WHERE OBSLT_UTC IS NULL
			GROUP BY act_id
			HAVING COUNT(act_vrsn_id) > 1) CNT_CHECK;

		EXIT WHEN CNT = 0;

		RAISE INFO 'REMOVING % DUPLICATE VERSIONS', CNT;
		UPDATE act_vrsn_tbl 
		SET 
			obslt_utc = CURRENT_TIMESTAMP, 
			obslt_usr_id = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8' 
		WHERE (act_id, act_vrsn_id) IN 
		(
			SELECT act_id, first(act_vrsn_id) FROM 
			(SELECT act_id, act_vrsn_id 
			FROM ACT_VRSN_TBL 
			WHERE OBSLT_UTC IS NULL
			ORDER BY vrsn_seq_id ASC) inner_order
			GROUP BY act_id
			HAVING COUNT(act_vrsn_id) > 1
		);
		
	END LOOP;
END
$$ LANGUAGE PLPGSQL;

SELECT PRUNE_DUP_VRSN();

SELECT REG_PATCH('20200320-01');

COMMIT;
