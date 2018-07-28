﻿CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- DATAMART SCHEMA TABLE
CREATE TABLE ADHOC_SCH (
	UUID UUID NOT NULL DEFAULT uuid_generate_v4(),
	NAME VARCHAR(64) NOT NULL, 
	CONSTRAINT PK_ADHOC PRIMARY KEY (UUID)
);

CREATE UNIQUE INDEX ADHOC_SCH_NAME_IDX ON ADHOC_SCH(NAME);

-- ADHOC WAREHOUSE PROPERTIES
CREATE TABLE ADHOC_SCH_PROPS (
	UUID UUID NOT NULL DEFAULT uuid_generate_v4(),
	CONT_UUID UUID, -- THE CONTAINER OF THE CURRENT PROPERTY (FOR NESTED PROPERTIES)
	SCH_UUID UUID NOT NULL, -- THE SCHEMA TO WHICH THE PROPERTY BELONGS
	NAME VARCHAR(64) NOT NULL, -- THE NAME OF THE PROPERTY
	TYPE INT NOT NULL, -- THE TYPE 
	ATTR INT NOT NULL, -- ATTRIBUTES
	CONSTRAINT PK_ADHOC_SCH_PROPS PRIMARY KEY (UUID),
	CONSTRAINT FK_ADHOC_SCH_CONT FOREIGN KEY (CONT_UUID) REFERENCES ADHOC_SCH_PROPS(UUID),
	CONSTRAINT FK_ADHOC_SCH_SCH_ID FOREIGN KEY (SCH_UUID) REFERENCES ADHOC_SCH(UUID)
);

CREATE INDEX ADHOC_SCH_PROP_CONT_ID_IDX ON ADHOC_SCH_PROPS(CONT_UUID);
CREATE INDEX ADHOC_SCH_PROP_SCH_ID_IDX ON ADHOC_SCH_PROPS(SCH_UUID);

-- THE DATAMART TABLE -> ALLOWS US TO STORE AD-HOC DATA
CREATE TABLE ADHOC_MART (
	UUID UUID NOT NULL DEFAULT uuid_generate_v4(),
	NAME VARCHAR(64) NOT NULL,
	CRT_UTC TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	SCH_UUID UUID NOT NULL,
	CONSTRAINT PK_ADHOC_MART PRIMARY KEY (UUID),
	CONSTRAINT FK_ADHOC_MART_SCH_UUID FOREIGN KEY (SCH_UUID) REFERENCES ADHOC_SCH(UUID)
);

CREATE UNIQUE INDEX ADHOC_MART_NAME_IDX ON ADHOC_MART(NAME);

-- ADHOC QUERY
CREATE TABLE ADHOC_QUERY (
	UUID UUID NOT NULL DEFAULT uuid_generate_v4(),
	SCH_UUID UUID NOT NULL, -- UUID OF THE SCHEMA
	NAME VARCHAR(64) NOT NULL, -- NAME OF THE QUERY
	CONSTRAINT PK_ADHOC_QUERY PRIMARY KEY (UUID),
	CONSTRAINT FK_ADHOC_QUERY_SCH_UUID FOREIGN KEY (SCH_UUID) REFERENCES ADHOC_SCH(UUID)
);	
