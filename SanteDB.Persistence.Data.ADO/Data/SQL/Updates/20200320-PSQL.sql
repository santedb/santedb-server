/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20200320-01" name="Update:20200320-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add removal of duplicate versions</summary>
 *	<remarks>This allows devices to be tracked to facilities and users</remarks>
 *	<isInstalled>select ck_patch('20200320-01')</isInstalled>
 * </feature>
 */

BEGIN TRANSACTION ;

CREATE OR REPLACE FUNCTION prune_dup_act_vrsn() 
RETURNS VOID AS
$$
DECLARE 
	CNT INTEGER;
BEGIN
	LOOP
		RAISE INFO 'SCANNING FOR DUPLICATE ACT VERSIONS';
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

CREATE OR REPLACE FUNCTION prune_dup_ent_vrsn() 
RETURNS VOID AS
$$
DECLARE 
	CNT INTEGER;
BEGIN
	LOOP
		RAISE INFO 'SCANNING FOR DUPLICATE ENTITY VERSIONS';
		SELECT COUNT(*) INTO CNT 
		FROM
			(SELECT ent_id, count(ent_vrsn_id) 
			FROM ent_VRSN_TBL 
			WHERE OBSLT_UTC IS NULL
			GROUP BY ent_id
			HAVING COUNT(ent_vrsn_id) > 1) CNT_CHECK;

		EXIT WHEN CNT = 0;

		RAISE INFO 'REMOVING % DUPLICATE VERSIONS', CNT;
		UPDATE ent_vrsn_tbl 
		SET 
			obslt_utc = CURRENT_TIMESTAMP, 
			obslt_usr_id = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8' 
		WHERE (ent_id, ent_vrsn_id) IN 
		(
			SELECT ent_id, first(ent_vrsn_id) FROM 
			(SELECT ent_id, ent_vrsn_id 
			FROM ent_VRSN_TBL 
			WHERE OBSLT_UTC IS NULL
			ORDER BY vrsn_seq_id ASC) inner_order
			GROUP BY ent_id
			HAVING COUNT(ent_vrsn_id) > 1
		);
		
	END LOOP;
END
$$ LANGUAGE PLPGSQL;

SELECT PRUNE_DUP_ACT_VRSN();
SELECT PRUNE_DUP_ENT_VRSN();
CREATE UNIQUE INDEX act_vrsn_v_uq_idx ON act_vrsn_tbl(act_id) WHERE (obslt_utc IS NULL);
CREATE UNIQUE INDEX ent_vrsn_v_uq_idx ON ent_vrsn_tbl(ent_id) WHERE (obslt_utc IS NULL);

SELECT REG_PATCH('20200320-01');

COMMIT;
