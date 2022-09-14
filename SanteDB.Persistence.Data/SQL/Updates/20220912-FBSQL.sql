﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220912-01" name="Update:20220912-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds functions for free-text searching</summary>
 *	<isInstalled>select ck_patch('20220912-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */

CREATE TABLE FT_ENT_SYSTBL
(
	ft_ent_id UUID NOT NULL,
	ent_id UUID NOT NULL,
	term VARCHAR(256) NOT NULL,
	CONSTRAINT pk_ft_ent_systbl PRIMARY KEY (ft_ent_id),
	CONSTRAINT fk_ft_ent_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl(ent_id)
);--#!
CREATE INDEX FT_ENT_TERM_IDX ON FT_ENT_SYSTBL(TERM);--#!
CREATE INDEX FT_ENT_ENT_IDX ON FT_ENT_SYSTBL(ENT_ID); --#!

-- ASSRERT ACT IS A PARTICULAR CLASS
CREATE OR ALTER PROCEDURE reindex_fti_ent(
	ENT_ID_IN UUID
) AS 
BEGIN
	DELETE FROM FT_ENT_SYSTBL WHERE ENT_ID = :ENT_ID_IN;
	INSERT INTO FT_ENT_SYSTBL 
	SELECT U, ENT_ID, LOWER(VAL) FROM (
		SELECT GEN_UUID() U, ENT_ID, VAL
		FROM 
			ENT_NAME_CMP_TBL 
			INNER JOIN ENT_NAME_TBL USING (NAME_ID)
		UNION 
		SELECT GEN_UUID() U, ENT_ID, VAL
		FROM 
			ENT_ADDR_CMP_TBL 
			INNER JOIN ENT_ADDR_TBL USING (ADDR_ID)
		UNION 
		SELECT GEN_UUID() U, ENT_ID, ID_VAL
			FROM ENT_ID_TBL
	) I WHERE ENT_ID = :ENT_ID_IN;				
END;
--#!
CREATE OR ALTER PROCEDURE rfrsh_fti() AS 
BEGIN
	DELETE FROM FT_ENT_SYSTBL;
	INSERT INTO FT_ENT_SYSTBL 
	SELECT U, ENT_ID, LOWER(VAL) FROM (
		SELECT GEN_UUID() U, ENT_ID, VAL
		FROM 
			ENT_NAME_CMP_TBL 
			INNER JOIN ENT_NAME_TBL USING (NAME_ID)
		UNION 
		SELECT GEN_UUID() U, ENT_ID, VAL
		FROM 
			ENT_ADDR_CMP_TBL 
			INNER JOIN ENT_ADDR_TBL USING (ADDR_ID)
		UNION 
		SELECT GEN_UUID() U, ENT_ID, ID_VAL
			FROM ENT_ID_TBL
	) 	;		
END;--#!
ALTER TABLE PATCH_DB_SYSTBL ADD INFO_NAME VARCHAR(256);--#!
SELECT REG_PATCH('20220912-01') FROM RDB$DATABASE; --#!

