/** 
 * <feature scope="SanteDB.Persistence.Data" id="20200320-01" name="Update:20200320-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Add removal of duplicate versions</summary>
 *	<remarks>This allows devices to be tracked to facilities and users</remarks>
 *	<isInstalled>select ck_patch('20200320-01')</isInstalled>
 * </feature>
 */
-- OPTIONAL
INSERT INTO CD_SET_MEM_ASSOC_TBL (set_id, cd_id) VALUES ('ba44f451-f979-4e11-9db4-aad8e2e88ce5', 'e537599d-8e57-4030-ba0a-62e96d708acb') ON CONFLICT DO NOTHING;
--#!

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
			obslt_prov_id = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8' 
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
			obslt_prov_id = 'fadca076-3690-4a6e-af9e-f1cd68e8c7e8' 
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

--#!
-- INFO: Pruning duplicate ACT versions
SELECT PRUNE_DUP_ACT_VRSN();
--#!
-- INFO: Pruning duplicate ENTITY versions
SELECT PRUNE_DUP_ENT_VRSN();
--#!
-- INFO: Re-Indexing
CREATE UNIQUE INDEX act_vrsn_v_uq_idx ON act_vrsn_tbl(act_id) WHERE (obslt_utc IS NULL);
CREATE UNIQUE INDEX ent_vrsn_v_uq_idx ON ent_vrsn_tbl(ent_id) WHERE (obslt_utc IS NULL);

--#!
SELECT REG_PATCH('20200320-01');
