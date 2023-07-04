﻿/** 
 * <feature scope="SanteDB.Persistence.Data.ADO" id="20220112-01" name="Update:20220112-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Adds and initializes fulltext searching to the SanteDB entity tables (may take up to 30 minutes to initialize)</summary>
 *	<isInstalled>select ck_patch('20220112-01')</isInstalled>
 * </feature>
 */
CREATE TABLE IF NOT EXISTS ft_ent_systbl 
(
	ent_id UUID NOT NULL DEFAULT uuid_generate_v1(),
	cls_cd_id UUID NOT NULL,
	terms tsvector,
	CONSTRAINT pk_ft_ent_systbl PRIMARY KEY (ent_id),
	CONSTRAINT fk_ft_ent_ent_id FOREIGN KEY (ent_id) REFERENCES ent_tbl(ent_id),
	CONSTRAINT fk_ft_cls_cd_id FOREIGN KEY (cls_cd_id) REFERENCES cd_tbl(cd_id)
);

ALTER TABLE psn_lng_tbl ALTER lng_cs TYPE VARCHAR(5);
CREATE INDEX ft_ent_ftidx ON ft_ent_systbl USING GIN (terms);

CREATE OR REPLACE FUNCTION rfrsh_fti() 
RETURNS void
AS
$$
BEGIN
	CREATE TEMPORARY TABLE ft_ent_tmptbl AS 
	SELECT ent_id, cls_cd_id, vector FROM
		ent_tbl 
		INNER JOIN
		( 
			SELECT ent_id, SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', tel_val)), 'D') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', id_val)), 'A') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', phon_val_tbl.val)), 'B') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', ent_addr_cmp_val_tbl.val)), 'C') AS vector
			FROM 
				ent_tbl 
				LEFT JOIN ent_tel_tbl USING (ent_id)
				LEFT JOIN ent_name_tbl USING (ent_id)
				LEFT JOIN ent_name_cmp_tbl USING (name_id)
				LEFT JOIN phon_val_tbl USING (val_seq_id)
				LEFT JOIN ent_addr_tbl USING (ent_id)
				LEFT JOIN ent_addr_cmp_tbl USING (addr_id)
				LEFT JOIN ent_addr_cmp_val_tbl ON (ent_addr_cmp_val_tbl.val_seq_id = ent_addr_cmp_tbl.VAL_SEQ_ID)
				LEFT JOIN ent_id_tbl USING (ent_id)
			WHERE 
				tel_val IS NOT NULL AND ent_tel_tbl.OBSLT_VRSN_SEQ_ID IS NULL OR 
				id_val IS NOT NULL  AND ent_id_tbl.OBSLT_VRSN_SEQ_ID IS NULL  OR 
				phon_val_tbl.VAL IS NOT NULL  AND ent_name_tbl.OBSLT_VRSN_SEQ_ID IS NULL  OR 
				ent_addr_cmp_val_tbl.VAL  IS NOT NULL AND ent_addr_tbl.OBSLT_VRSN_SEQ_ID IS NULL 
			GROUP BY ent_id
		) vectors USING (ent_id);
	TRUNCATE TABLE ft_ent_systbl;
	INSERT INTO ft_ent_systbl SELECT * FROM ft_ent_tmptbl ;
	
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION reindex_fti_ent(ent_id_in IN UUID) 
RETURNS void
AS 
$$
BEGIN 
	UPDATE FT_ENT_SYSTBL 
	SET terms = vector
	FROM 
		(SELECT SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', tel_val)), 'D') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', id_val)), 'A') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', phon_val_tbl.val)), 'B') ||
				SETWEIGHT(TO_TSVECTOR(STRING_AGG(' ', ent_addr_cmp_val_tbl.val)), 'C') AS vector
			FROM 
				ent_tbl 
				LEFT JOIN ent_tel_tbl USING (ent_id)
				LEFT JOIN ent_name_tbl USING (ent_id)
				LEFT JOIN ent_name_cmp_tbl USING (name_id)
				LEFT JOIN phon_val_tbl USING (val_seq_id)
				LEFT JOIN ent_addr_tbl USING (ent_id)
				LEFT JOIN ent_addr_cmp_tbl USING (addr_id)
				LEFT JOIN ent_addr_cmp_val_tbl ON (ent_addr_cmp_val_tbl.val_seq_id = ent_addr_cmp_tbl.VAL_SEQ_ID)
				LEFT JOIN ent_id_tbl USING (ent_id)
			WHERE 
				ent_id = ent_id_in AND (
				tel_val IS NOT NULL AND ent_tel_tbl.OBSLT_VRSN_SEQ_ID IS NULL OR 
				id_val IS NOT NULL  AND ent_id_tbl.OBSLT_VRSN_SEQ_ID IS NULL  OR 
				phon_val_tbl.VAL IS NOT NULL  AND ent_name_tbl.OBSLT_VRSN_SEQ_ID IS NULL  OR 
				ent_addr_cmp_val_tbl.VAL  IS NOT NULL AND ent_addr_tbl.OBSLT_VRSN_SEQ_ID IS NULL)) I 
	WHERE ent_id = ent_id_in;

END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION fti_tsquery(search_term_in in text)
RETURNS tsquery 
IMMUTABLE
AS 
$$
BEGIN
	RETURN websearch_to_tsquery(search_term_in);
END;
$$ LANGUAGE plpgsql;

SELECT rfrsh_fti();

CREATE INDEX psn_dob_idx ON psn_tbl (dob);
CREATE INDEX psn_gndr_idx ON psn_tbl (gndr_cd_id);
DROP INDEX phon_val_phon_val_idx;
DROP INDEX phon_val_val_btr_idx;
CREATE INDEX phon_val_val_soundex_idx ON phon_val_tbl (SOUNDEX(val));
DROP INDEX en_addr_cmp_val_val_idx;
CREATE INDEX ent_addr_cmp_val_gin_idx ON ent_addr_cmp_val_tbl USING GIN (val gin_trgm_ops);
SELECT REG_PATCH('20220112-01');