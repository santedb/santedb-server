/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220414-01" name="Update:20220414-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds HEAD column to the versioned tables</summary>
 *	<isInstalled>select ck_patch('20220414-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */
ALTER TABLE ent_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
ALTER TABLE cd_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!
ALTER TABLE act_vrsn_tbl ADD head BOOLEAN DEFAULT FALSE NOT NULL;--#!

UPDATE ent_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE cd_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!
UPDATE act_vrsn_tbl SET head = TRUE WHERE obslt_utc IS NULL;--#!

CREATE UNIQUE INDEX ent_vrsn_head_uq_idx ON ent_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN ENT_ID END);--#!
CREATE UNIQUE INDEX act_vrsn_head_uq_idx ON act_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN ACT_ID END);--#!
CREATE UNIQUE INDEX cd_vrsn_head_uq_idx ON cd_vrsn_tbl COMPUTED BY (CASE WHEN HEAD THEN CD_ID END);--#!

SELECT REG_PATCH('20220414-01') FROM RDB$DATABASE; --#!

