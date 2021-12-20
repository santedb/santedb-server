﻿/** 
 * <feature scope="SanteDB.Persistence.Data" id="20211218-01" name="Update:20211218-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="npgsql">
 *	<summary>Update: Adds container classes to the database</summary>
 *	<isInstalled>select ck_patch('20211218-01')</isInstalled>
 * </feature>
 */
 CREATE TABLE CONT_ENT_TBL (
	ENT_VRSN_ID UUID NOT NULL,
	CAP_QTY NUMERIC(12,4),
	DIA_QTY NUMERIC(12,4),
	HGT_QTY NUMERIC(12,4),
	CONSTRAINT PK_CONT_ENT_TBL PRIMARY KEY (ENT_VRSN_ID),
	CONSTRAINT FK_CONT_ENT_ENT_VRSN_TBL FOREIGN KEY (ENT_VRSN_ID) REFERENCES ENT_VRSN_TBL(ENT_VRSN_ID)
);
SELECT REG_PATCH('20211218-01');