/** 
 * <feature scope="SanteDB.Persistence.Data" id="20190625-01" name="Update:20190625-01" applyRange="0.2.0.0-0.9.0.0" invariantName="FirebirdSQL">
 *	<summary>Update:Cumulative update for FirebirdSQL</summary>
 *	<remarks>Updates the FirebirdSQL database to latest SanteDB</remarks>
 *  <isInstalled>select ck_patch('20190625-01') from rdb$database</isInstalled>
 * </feature>
 */

 -- VERIFICATION
-- RULE 1. -> SERVICE DELIVERY LOCATIONS CAN ONLY HAVE SERVICE DELIVERY LOCATIONS FOR PARENTS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_sdl_parent_sdlOnly');--#!
-- RULE 2. -> NON-SERVICE DELIVERY LOCATIONS CAN ONLY HAVE NON-SERVICE DELIVERY LOCATIONS FOR PARENTS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'err_place_parent_placeOnly');--#!
-- RULE 3. -> STATES CAN ONLY HAVE COUNTRY AS PARENT
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), 'err_state_parent_countryOnly');--#!
-- RULE 4. -> COUNTY CAN ONLY HAVE STATE AS PARENT
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), 'err_county_parent_stateOnly');--#!
-- RULE 5. -> CITY CAN ONLY HAVE COUNTY OR STATE AS PARENT
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), 'err_city_parent_stateOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), 'err_city_parent_countyOnly');--#!
-- RULE 6. -> PLACES CAN HAVE ANY PLACE AS PARENT
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), 'err_place_parent_placeOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('BFCBB345-86DB-43BA-B47E-E7411276AC7C'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), 'err_place_parent_placeOnly');--#!
-- RULE 7. -> MATERIALS CAN ONLY HAVE MANUFACTURED MATERIALS AS INSTANCE
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('AC45A740-B0C7-4425-84D8-B3F8A41FEF9F'), char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), char_to_uuid('fafec286-89d5-420b-9085-054aca9d1eef'), 'err_material_instance_manufacturedMaterialOnly');--#!
-- RULE 8. -> ONLY ORGANIZATIONS CAN MANUFACTURE MATERIALS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('6780DF3B-AFBD-44A3-8627-CBB3DC2F02F6'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), char_to_uuid('fafec286-89d5-420b-9085-054aca9d1eef'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
-- RULE 9. -> ONLY SDLS CAN BE DEDICATED SERVICE DELIVERY LOCATIONS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('8ba5e5c9-693b-49d4-973c-d7010f3a23ee'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('e29fcfad-ec1d-4c60-a055-039a494248ae'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('61fcbf42-b5e0-4fb5-9392-108a5c6dbec7'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
-- RULE 10. -> ONLY SDLS CAN OWN STOCK
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('117da15c-0864-4f00-a987-9b9854cba44e'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), char_to_uuid('fafec286-89d5-420b-9085-054aca9d1eef'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
-- RULE 11. -> ONLY PERSONS OR PATIENTS CAN BE MOTHERS OR NEXT OF KIN
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc)
	SELECT cd_id, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), 'err_patient_nok_personOnly'
	FROM cd_set_mem_vw
	WHERE set_mnemonic = 'FamilyMember';
	--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455F1772-F580-47E8-86BD-B5CE25D351F9'), char_to_uuid('4d1a5c28-deb7-411e-b75f-d524f90dfa63'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
-- RULE 10. -> SDL IS VALID BETWEEN PATIENTS AND LOCATIONS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('41baf7aa-5ffd-4421-831f-42d4ab3de38a'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_organization_manufactures_manufacturedMaterialOnly');--#!
-- RULE 12. -> Materials may use other Materials
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('08fff7d9-bac7-417b-b026-c9bee52f4a37'), char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), 'err_materials_associate_materials');--#!
-- RULE 14: PROVIDERS CAN BE HEALTHCARE PROVIDERS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('77b7a04b-c065-4faf-8ec0-2cdad4ae372b'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), 'err_healthCareProviders_PersonsOnly');--#!
-- RULE 15: NOK IS BETWEEN PATIENTS AND PERSONS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('1ee4e74f-542d-4544-96f6-266a6247f274'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), 'err_nextOfKin_PersonsOnly');--#!
-- RULE 16: DEDICATED SDL AND STATE / COUNTY
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455f1772-f580-47e8-86bd-b5ce25d351f9'), char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_stateDedicatedSDL');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455f1772-f580-47e8-86bd-b5ce25d351f9'), char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_stateDedicatedSDL');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('455f1772-f580-47e8-86bd-b5ce25d351f9'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'err_stateDedicatedSDL');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) 
	SELECT char_to_uuid('d1578637-e1cb-415e-b319-4011da033813'), cd_id, cd_id, 'err_ReplaceOnlySameType' FROM cd_set_mem_vw WHERE set_mnemonic = 'EntityClass';--#!

-- RULE 17: ORGS ARE TERRITORIAL AUTHORITIES OF PLACES
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('C6B92576-1D62-4896-8799-6F931F8AB607'), char_to_uuid('7C08BD55-4D42-49CD-92F8-6388D6C4183F'), char_to_uuid('21AB7873-8EF3-4D78-9C19-4582B3C40631'), 'err_stateDedicatedSDL');--#!
-- RULE BIRTHPLACE CAN BE BETWEEN A PLACE OR ORGANIZATION AND PATIENT OR PERSON
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'Birthplace Person>Place');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Birthplace Person>Organization');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), 'Birthplace Patient>Place');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Birthplace Patient>Organization');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Birthplace Person>SDL');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('F3EF7E48-D8B7-4030-B431-AFF7E0E1CB76'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Birthplace Person>SDL');--#!
-- RULE CITIZENSHIP
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('35B13152-E43C-4BCB-8649-A9E83BEE33A2'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), 'Citizenship Person>COUNTRY');--#!
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc) VALUES (char_to_uuid('35B13152-E43C-4BCB-8649-A9E83BEE33A2'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('48b2ffb3-07db-47ba-ad73-fc8fb8502471'), 'Citizenship Patient>COUNTRY');--#!
--#!
-- X CAN BE A CHILD OF X
INSERT INTO ENT_REL_VRFY_CDTBL (src_cls_cd_id, rel_typ_cd_id, trg_cls_cd_id, err_desc) SELECT CD_ID, char_to_uuid('739457d0-835a-4a9c-811c-42b5e92ed1ca'), CD_ID, 'CHILD RECORD' FROM CD_SET_MEM_ASSOC_TBL WHERE SET_ID = char_to_uuid('4e6da567-0094-4f23-8555-11da499593af');
--#!
--#!
-- PSN or PAT CAN BE EMPLOYEE OF ORG
INSERT INTO ENT_REL_VRFY_CDTBL (trg_cls_cd_id, rel_typ_cd_id, src_cls_cd_id, err_desc) VALUES (char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), char_to_uuid('b43c9513-1c1c-4ed0-92db-55a904c122e6'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), 'Person=[Employee]=>Organization'); 
--#!
INSERT INTO ENT_REL_VRFY_CDTBL (trg_cls_cd_id, rel_typ_cd_id, src_cls_cd_id, err_desc) VALUES (char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), char_to_uuid('b43c9513-1c1c-4ed0-92db-55a904c122e6'), char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), 'Person=[Employee]=>Organization'); 
--#!

--#!
-- MISSING POLICY IDENTIFIERS
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (char_to_uuid('baa227aa-224d-4859-81b3-c1eb2750067f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.11', 'Access Audit Log', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (char_to_uuid('baa227aa-224d-4859-81b3-c1eb2750068f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.12', 'Administer Applets', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (char_to_uuid('baa227aa-224d-4859-81b3-c1eb2750069f'), '1.3.6.1.4.1.33349.3.1.5.9.2.0.13', 'Assign Policy', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!
INSERT INTO SEC_POL_TBL (POL_ID, OID, POL_NAME, CRT_PROV_ID) VALUES (char_to_uuid('baa227aa-224d-4859-81b3-c1eb275006af'), '1.3.6.1.4.1.33349.3.1.5.9.2.2.5', 'Elevate Clinical Data', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));
--#!

--#!
-- Index for performance
CREATE INDEX ACT_VRSN_CRT_UTC_IDX ON ACT_VRSN_TBL(CRT_UTC);
--#!
CREATE INDEX ENT_VRSN_CRT_UTC_IDX ON ENT_VRSN_TBL(CRT_UTC);
--#!

--#!
ALTER TABLE ENT_EXT_TBL ALTER EXT_DISP TYPE VARCHAR(4096);
--#!
ALTER TABLE ACT_EXT_TBL ALTER EXT_DISP TYPE VARCHAR(4096);
--#!
-- RULE -> PATIENTS CAN BE NEXT OF KIN TO OTHER PATIENTS
INSERT INTO ent_rel_vrfy_cdtbl (rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc)
	SELECT cd_id, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), 'err_patient_nok_personOnly'
	FROM cd_set_mem_vw
	WHERE set_mnemonic = 'FamilyMember'
	AND NOT EXISTS (SELECT 1 FROM ent_rel_vrfy_cdtbl WHERE src_cls_cd_id = char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d') AND trg_cls_cd_id = char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d') AND rel_typ_cd_id = cd_id);
--#!
UPDATE ENT_REL_VRFY_CDTBL 
	SET err_desc = (
		SELECT SRC.MNEMONIC || ' ==[' || TYP.MNEMONIC || ']==> ' || TRG.MNEMONIC 
		FROM 
			ENT_REL_VRFY_CDTBL VFY
			INNER JOIN CD_VRSN_TBL TYP ON (REL_TYP_CD_ID = TYP.CD_ID)
			INNER JOIN CD_VRSN_TBL SRC ON (SRC_CLS_CD_ID = SRC.CD_ID)
			INNER JOIN CD_VRSN_TBL TRG ON (TRG_CLS_CD_ID = TRG.CD_ID)
		WHERE 
			VFY.ENT_REL_VRFY_ID = ENT_REL_VRFY_CDTBL.ENT_REL_VRFY_ID
		FETCH FIRST 1 ROWS ONLY
	);
--#!
-- FIX CACT IN CLASS
INSERT INTO CD_TBL VALUES(char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F'),TRUE);--#!
INSERT INTO CD_VRSN_TBL (CD_VRSN_ID, CD_ID, STS_CD_ID, CRT_PROV_ID, MNEMONIC, CLS_ID) VALUES (gen_uuid(), char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F'), char_to_uuid('c8064cbd-fa06-4530-b430-1a52f1530c27'), char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'), 'ControlActEvent', char_to_uuid('17FD5254-8C25-4ABB-B246-083FBE9AFA15'));--#!
INSERT INTO REF_TERM_TBL(REF_TERM_ID, CS_ID, MNEMONIC, CRT_PROV_ID)  VALUES(char_to_uuid('524e38bb-0a7a-490d-9400-b25ae094099c'), char_to_uuid('BAB1D66A-1E98-4BFD-9B4A-7C4BE88F35D1'), 'CACT', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'));--#!
INSERT INTO CD_REF_TERM_ASSOC_TBL(CD_REF_TERM_ID, REF_TERM_ID, CD_ID, EFFT_VRSN_SEQ_ID, REL_TYP_ID) SELECT gen_uuid(), char_to_uuid('524e38bb-0a7a-490d-9400-b25ae094099c'), char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F'), VRSN_SEQ_ID, char_to_uuid('2c4dafc2-566a-41ae-9ebc-3097d7d22f4a') FROM CD_VRSN_TBL WHERE CD_ID = char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F') AND OBSLT_UTC IS NULL;--#!
INSERT INTO REF_TERM_NAME_TBL(REF_TERM_NAME_ID, REF_TERM_ID, LANG_CS, TERM_NAME, CRT_PROV_ID, PHON_ALG_ID) VALUES(gen_uuid(), char_to_uuid('524e38bb-0a7a-490d-9400-b25ae094099c'), 'en', 'Control Event', char_to_uuid('fadca076-3690-4a6e-af9e-f1cd68e8c7e8'), char_to_uuid('402CD339-D0E4-46CE-8FC2-12A4B0E17226'));--#!
INSERT INTO CD_NAME_TBL(NAME_ID, CD_ID, EFFT_VRSN_SEQ_ID, LANG_CS, VAL, PHON_ALG_ID) SELECT gen_uuid(), CD_ID, VRSN_SEQ_ID, 'en', 'Control Event', char_to_uuid('402CD339-D0E4-46CE-8FC2-12A4B0E17226') FROM CD_VRSN_TBL WHERE CD_ID = char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F') AND OBSLT_UTC IS NULL;--#!
INSERT INTO cd_set_mem_assoc_tbl (set_id, cd_id) VALUES (char_to_uuid('62c5fde0-a3aa-45df-94e9-242f4451644a'), char_to_uuid('B35488CE-B7CD-4DD4-B4DE-5F83DC55AF9F'));--#!

-- GRANT SYSTEM LOGIN AS A SERVICE
INSERT INTO sec_rol_pol_assoc_tbl (sec_pol_inst_id, pol_id, rol_id, pol_act) VALUES (gen_uuid(), char_to_uuid('e15b96ab-646c-4c00-9a58-ea09eee67d7c'), char_to_uuid('c3ae21d2-fc23-4133-ba42-b0e0a3b817d7'), 2);--#!
--#!
ALTER TABLE PSN_LNG_TBL ALTER COLUMN LNG_CS TYPE CHAR(5);--#!
DROP INDEX SEC_DEV_SCRT_IDX ;--#!
SELECT REG_PATCH('20190625-01') FROM RDB$DATABASE;--#!