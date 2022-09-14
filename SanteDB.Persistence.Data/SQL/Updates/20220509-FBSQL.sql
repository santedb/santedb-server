/** 
 * <feature scope="SanteDB.Persistence.Data" id="20220509-01" name="Update:20220509-01" applyRange="1.1.0.0-1.2.0.0"  invariantName="FirebirdSQL">
 *	<summary>Update: Adds validation rules for participation and act relationships</summary>
 *	<isInstalled>select ck_patch('20220509-01') from RDB$DATABASE</isInstalled>
 * </feature>
 */

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('A0174216-6439-4351-9483-A241A48029B7'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Admitter]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('A0174216-6439-4351-9483-A241A48029B7'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Admitter]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('6CBF29AD-AC51-48C9-885A-CFE3026ECF6E'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Attender]=>HealthcareProvider', 3);--#!


INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('1B2DBF82-A503-4CF4-9ECB-A8E111B4674E'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Authenticator]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('1B2DBF82-A503-4CF4-9ECB-A8E111B4674E'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Authenticator]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('1B2DBF82-A503-4CF4-9ECB-A8E111B4674E'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Authenticator]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('1B2DBF82-A503-4CF4-9ECB-A8E111B4674E'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Authenticator]=>Patient', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F0CB3FAF-435D-4704-9217-B884F757BC14'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[AuthorOrOriginator]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F0CB3FAF-435D-4704-9217-B884F757BC14'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[AuthorOrOriginator]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F0CB3FAF-435D-4704-9217-B884F757BC14'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[AuthorOrOriginator]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F0CB3FAF-435D-4704-9217-B884F757BC14'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[AuthorOrOriginator]=>Patient', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F0CB3FAF-435D-4704-9217-B884F757BC14'), null, char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), '*=[AuthorOrOriginator]=>Device', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('479896B0-35D5-4842-8109-5FDBEE14E8A4'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Baby]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('479896B0-35D5-4842-8109-5FDBEE14E8A4'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Baby]=>Patient', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('28C744DF-D889-4A44-BC1A-2E9E9D64AF13'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Beneficiary]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('28C744DF-D889-4A44-BC1A-2E9E9D64AF13'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Beneficiary]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('28C744DF-D889-4A44-BC1A-2E9E9D64AF13'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Beneficiary]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('28C744DF-D889-4A44-BC1A-2E9E9D64AF13'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Beneficiary]=>Patient', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('7F81B83E-0D78-4685-8BA4-224EB315CE54'), null, char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), '*=[CausitiveAgent]=>Material', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('7F81B83E-0D78-4685-8BA4-224EB315CE54'), null, char_to_uuid('fafec286-89d5-420b-9085-054aca9d1eef'), '*=[CausitiveAgent]=>ManufacturedMaterial', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('7F81B83E-0D78-4685-8BA4-224EB315CE54'), null, char_to_uuid('61fcbf42-b5e0-4fb5-9392-108a5c6dbec7'), '*=[CausitiveAgent]=>Animal', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('7F81B83E-0D78-4685-8BA4-224EB315CE54'), null, char_to_uuid('e5a09cc2-5ae5-40c2-8e32-687dba06715d'), '*=[CausitiveAgent]=>Food', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('7F81B83E-0D78-4685-8BA4-224EB315CE54'), null, char_to_uuid('2e9fa332-9391-48c6-9fc8-920a750b25d3'), '*=[CausitiveAgent]=>ChemicalSubstance', 3);--#!
	

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('4B5471D4-E3FE-45F7-85A2-AE2B4F224757'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[CoverageTarget]=>Patient', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('649D6D69-139C-4006-AE45-AFF4649D6079'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Custodian]=>Organization', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('A2594E6E-E8FE-4C68-82A5-D3A46DBEC87D'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Discharger]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('A2594E6E-E8FE-4C68-82A5-D3A46DBEC87D'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Discharger]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('A2594E6E-E8FE-4C68-82A5-D3A46DBEC87D'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Discharger]=>Patient (self-discharged)', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('693F08FA-625A-40D2-B928-6856099C0349'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Distributor]=>Organization', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('BE1235EE-710A-4732-88FD-6E895DE7C56D'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Donor]=>Organization', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('AC05185B-5A80-47A8-B924-060DEB6D0EB2'), null, char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), '*=[Donor]=>ServiceDeliveryLocation', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5A6A6766-8E1D-4D36-AE50-9B7D82D8A182'), null, char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), '*=[Exposure]=>Material', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5A6A6766-8E1D-4D36-AE50-9B7D82D8A182'), null, char_to_uuid('fafec286-89d5-420b-9085-054aca9d1eef'), '*=[Exposure]=>ManufacturedMaterial', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5A6A6766-8E1D-4D36-AE50-9B7D82D8A182'), null, char_to_uuid('61fcbf42-b5e0-4fb5-9392-108a5c6dbec7'), '*=[Exposure]=>Animal', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5A6A6766-8E1D-4D36-AE50-9B7D82D8A182'), null, char_to_uuid('e5a09cc2-5ae5-40c2-8e32-687dba06715d'), '*=[Exposure]=>Food', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5A6A6766-8E1D-4D36-AE50-9B7D82D8A182'), null, char_to_uuid('2e9fa332-9391-48c6-9fc8-920a750b25d3'), '*=[Exposure]=>ChemicalSubstance', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('EA60A5A9-E971-4F0D-BB5D-DC7A0C74A2C9'), null, char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), '*=[ExposureAgent]=>Material', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('EA60A5A9-E971-4F0D-BB5D-DC7A0C74A2C9'), null, char_to_uuid('e5a09cc2-5ae5-40c2-8e32-687dba06715d'), '*=[ExposureAgent]=>Food', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('EA60A5A9-E971-4F0D-BB5D-DC7A0C74A2C9'), null, char_to_uuid('2e9fa332-9391-48c6-9fc8-920a750b25d3'), '*=[ExposureAgent]=>ChemicalSubstance', 3);--#!



INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('39604248-7812-4B60-BC54-8CC1FFFB1DE6'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Informant]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('39604248-7812-4B60-BC54-8CC1FFFB1DE6'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Informant]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('39604248-7812-4B60-BC54-8CC1FFFB1DE6'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[Informant]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('39604248-7812-4B60-BC54-8CC1FFFB1DE6'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Informant]=>Patient', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0716A333-CD46-439D-BFD6-BF788F3885FA'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[LegalAuthenticator]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0716A333-CD46-439D-BFD6-BF788F3885FA'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[LegalAuthenticator]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0716A333-CD46-439D-BFD6-BF788F3885FA'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[LegalAuthenticator]=>Patient', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), '*=[Location]=>ServiceDeliveryLocation', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), '*=[Location]=>Place', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), '*=[Location]=>State', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), '*=[Location]=>County', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('acafe0f2-e209-43bb-8633-3665fd7c90ba'), '*=[Location]=>PrecinctOrBorough', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('61848557-D78D-40E5-954F-0B9C97307A04'), null, char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), '*=[Location]=>CityOrTown', 3);--#!


INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('6792DB6C-FD5C-4AB8-96F5-ACE5665BDCB9'), null, char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), '*=[NonReusableDevice]=>Device', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('76990D3D-3F27-4B39-836B-BA87EEBA3328'), null, char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), '*=[ReusableDevice]=>Device', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('1373FF04-A6EF-420A-B1D0-4A07465FE8E8'), null, char_to_uuid('1373ff04-a6ef-420a-b1d0-4a07465fe8e8'), '*=[Device]=>Device', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('C704A23D-86EF-4E11-9050-F8AA10919FF2'), null, null, '*=[Participation]=>*', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('FA5E70A4-A46E-4665-8A20-94D4D7B86FC8'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Performer]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('FA5E70A4-A46E-4665-8A20-94D4D7B86FC8'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Performer]=>HealthcareProvider', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('79F6136C-1465-45E8-917E-E7832BC8E3B2'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[PrimaryPerformer]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('79F6136C-1465-45E8-917E-E7832BC8E3B2'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[PrimaryPerformer]=>HealthcareProvider', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('99E77288-CB09-4050-A8CF-385513F32F0A'), char_to_uuid('932a3c7e-ad77-450a-8a1f-030fc2855450'), char_to_uuid('d39073be-0f8f-440e-b8c8-7034cc138a95'), 'SubstanceAdministration=[Product]=>Material', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('3F92DBEE-A65E-434F-98CE-841FEEB02E3F'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[RecordTarget]=>Patient', 3);--#!
	

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('353F9255-765E-4336-8007-1D61AB09AAD6'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[ReferredBy]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('353F9255-765E-4336-8007-1D61AB09AAD6'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[ReferredBy]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('353F9255-765E-4336-8007-1D61AB09AAD6'), null, char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), '*=[ReferredBy]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('353F9255-765E-4336-8007-1D61AB09AAD6'), null, char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), '*=[ReferredBy]=>ServiceDeliveryLocation', 3);--#!



INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5D175F21-1963-4589-A400-B5EF5F64842C'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Transfer=[Origin]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5D175F21-1963-4589-A400-B5EF5F64842C'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Transfer=[Origin]=>ServiceDeliveryLocation', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('727B3624-EA62-46BB-A68B-B9E49E302ECA'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Transfer=[Destination]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('727B3624-EA62-46BB-A68B-B9E49E302ECA'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Transfer=[Destination]=>ServiceDeliveryLocation', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5D175F21-1963-4589-A400-B5EF5F64842C'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Supply=[Origin]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5D175F21-1963-4589-A400-B5EF5F64842C'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Supply=[Origin]=>ServiceDeliveryLocation', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('727B3624-EA62-46BB-A68B-B9E49E302ECA'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('7c08bd55-4d42-49cd-92f8-6388d6c4183f'), 'Supply=[Destination]=>Organization', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('727B3624-EA62-46BB-A68B-B9E49E302ECA'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), 'Supply=[Destination]=>ServiceDeliveryLocation', 3);--#!


INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('53C694B8-27D8-43DD-95A4-BB318431D17C'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Receiver]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('53C694B8-27D8-43DD-95A4-BB318431D17C'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Receiver]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('53C694B8-27D8-43DD-95A4-BB318431D17C'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Receiver]=>Patient', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('BCE17B21-05B2-4F02-BF7A-C6D3561AA948'), null, char_to_uuid('8ba5e5c9-693b-49d4-973c-d7010f3a23ee'), '*=[Specimen]=>LivingSubject', 3);--#!

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('DE3F7527-E3C9-45EF-8574-00CA4495F767'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Transcriber]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('DE3F7527-E3C9-45EF-8574-00CA4495F767'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Transcriber]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('DE3F7527-E3C9-45EF-8574-00CA4495F767'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Transcriber]=>Patient', 3);--#!
	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F9DC5787-DD4D-42C6-A082-AC7D11956FDA'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Verifier]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F9DC5787-DD4D-42C6-A082-AC7D11956FDA'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Verifier]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('F9DC5787-DD4D-42C6-A082-AC7D11956FDA'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Verifier]=>Patient', 3);--#!
	

INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0B82357F-5AE0-4543-AB8E-A33E9B315BAB'), null, char_to_uuid('9de2a846-ddf2-4ebc-902e-84508c5089ea'), '*=[Witness]=>Person', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0B82357F-5AE0-4543-AB8E-A33E9B315BAB'), null, char_to_uuid('6b04fed8-c164-469c-910b-f824c2bda4f0'), '*=[Witness]=>HealthcareProvider', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('0B82357F-5AE0-4543-AB8E-A33E9B315BAB'), null, char_to_uuid('bacd9c6f-3fa9-481e-9636-37457962804d'), '*=[Witness]=>Patient', 3);--#!
		
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('ff34dfa7-c6d3-4f8b-bc9f-14bcdc13ba6c'), '*=[Via]=>ServiceDeliveryLocation', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('8cf4b0b0-84e5-4122-85fe-6afa8240c218'), '*=[Via]=>State', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('21ab7873-8ef3-4d78-9c19-4582b3c40631'), '*=[Via]=>Place', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('d9489d56-ddac-4596-b5c6-8f41d73d8dc5'), '*=[Via]=>County', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('acafe0f2-e209-43bb-8633-3665fd7c90ba'), '*=[Via]=>PrecinctOrBorough', 3);--#!
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls)
	VALUES (gen_uuid(), char_to_uuid('5B0FAC74-5AC6-44E6-99A4-6813C0E2F4A9'), null, char_to_uuid('79dd4f75-68e8-4722-a7f5-8bc2e08f5cd6'), '*=[Via]=>CityOrTown', 3);--#!

	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	SELECT gen_uuid(), char_to_uuid('DC3DF205-18EF-4854-AC00-68C295C9C744'), cd_id, cd_id, mnemonic || '=[Appends]=>' || mnemonic, 2
	FROM 
		cd_vrsn_tbl 
		INNER JOIN cd_set_mem_assoc_tbl USING (cd_id)
	WHERE 
		set_id = char_to_uuid('62C5FDE0-A3AA-45DF-94E9-242F4451644A')
		AND obslt_utc IS NULL;--#!


	
-- ANY FULFILLS ANY OF SAME CLASS
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	SELECT gen_uuid(), char_to_uuid('646542BC-72E4-488B-BBF4-865D452E62EC'), cd_id, cd_id, mnemonic || '=[Fulfills]=>' || mnemonic, 2
	FROM 
		cd_vrsn_tbl 
		INNER JOIN cd_set_mem_assoc_tbl USING (cd_id)
	WHERE 
		set_id = char_to_uuid('62C5FDE0-A3AA-45DF-94E9-242F4451644A')
		AND obslt_utc IS NULL;--#!

-- ANY REFERS ANY ACT
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('8FCE259A-B859-4AE3-8160-0221F6AB1650'), NULL, NULL, '*=[RefersTo]=>*', 2);--#!
-- ANY STARTS AFTER ANY ACT
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('C66D7CA9-C6C2-46B1-9276-AD76BAF04B07'), NULL, NULL, '*=[StartsAfterStartIf]=>*', 2);--#!


-- ENCOUNTER EL ENCOUNTER ACT
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('EBF9AC10-B5C9-407A-91A4-360BFB7E0FB9'), char_to_uuid('54b52119-1709-4098-8911-5df6d6c84140'), char_to_uuid('54b52119-1709-4098-8911-5df6d6c84140'), 'Encounter=[EpisodeLink]=>Encounter', 2);--#!

-- SUPPLY DEPARTURE SUPPLY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('28C81CDC-CA56-4C92-B691-094E89630642'), CHAR_TO_UUID('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), 'SUPPLY=[DEPARTURE]=>SUPPLY', 2);--#!

-- TRANSFER DEPARTURE TRANSFER
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('28C81CDC-CA56-4C92-B691-094E89630642'), CHAR_TO_UUID('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), 'TRANSPORT=[DEPARTURE]=>TRANSPORT', 2);--#!

--TRANSFER ARRIVAL TRANSFER	
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('26FE590C-3684-4574-9359-057FDD06BA61'), CHAR_TO_UUID('61677f76-dc05-466d-91de-47efc8e7a3e6'), char_to_uuid('61677f76-dc05-466d-91de-47efc8e7a3e6'), 'TRANSPORT=[ARRIVAL]=>TRANSPORT', 2);--#!

-- SUPPLY ARRIVAL SUPPLY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('26FE590C-3684-4574-9359-057FDD06BA61'), CHAR_TO_UUID('a064984f-9847-4480-8bea-dddf64b3c77c'), char_to_uuid('a064984f-9847-4480-8bea-dddf64b3c77c'), 'SUPPLY=[ARRIVAL]=>SUPPLY', 2);--#!

-- ENCOUNTER HAS COMPONENT ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('78B9540F-438B-4B6F-8D83-AAF4979DBC64'), char_to_uuid('54b52119-1709-4098-8911-5df6d6c84140'), NULL, 'Encounter=[HasComponent]=>*', 2);--#!

-- ENCOUNTER HAS SUBJECT ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('9871C3BC-B57A-479D-A031-7B56CB06FA84'), char_to_uuid('54b52119-1709-4098-8911-5df6d6c84140'), NULL, 'Encounter=[HasSubject]=>*', 2);--#!
		
-- ANY HAS REASON ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('55DA61A2-7B86-47F3-9B0B-BA47DC99C950'), NULL, NULL, '*=[HasReason]=>*', 2);--#!


-- OBSERVATION HAS COMPONENT OBSERVATION
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('78B9540F-438B-4B6F-8D83-AAF4979DBC64'), char_to_uuid('28d022c6-8a8b-47c4-9e6a-2bc67308739e'), char_to_uuid('28d022c6-8a8b-47c4-9e6a-2bc67308739e'), 'Observation=[HasComponent]=>Observation', 2);--#!

-- OBSERVATION HAS REFERENCE VALUE OBSERVATION
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('99488A1D-6D97-4013-8C91-DED6AD3B8E89'), char_to_uuid('28d022c6-8a8b-47c4-9e6a-2bc67308739e'), char_to_uuid('28d022c6-8a8b-47c4-9e6a-2bc67308739e'), 'Observation=[HasReferenceValues]=>Observation', 2);--#!

-- BATTERY HAS COMPONENT ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('78B9540F-438B-4B6F-8D83-AAF4979DBC64'), char_to_uuid('676de278-64aa-44f2-9b69-60d61fc1f5f5'), NULL, 'Battery=[HasComponent]=>*', 2);--#!

-- CONDITION HAS MANIFESTATION OF OBSERVATION
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('22918D17-D3DC-4135-A003-4C1C52E57E75'), char_to_uuid('1987c53c-7ab8-4461-9ebc-0d428744a8c0'), char_to_uuid('28d022c6-8a8b-47c4-9e6a-2bc67308739e'), 'Condition=[HasManifestation]=>Observation', 2);--#!

-- ANY IS CAUSE OF CONDITION
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('57D81685-E399-4ABD-8744-96454188A9FA'), NULL, char_to_uuid('1987c53c-7ab8-4461-9ebc-0d428744a8c0'), '*=[IsCauseOf]=>Condition', 2);--#!
	
-- CONDITION HAS SUPPORT OF ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('3209E3F1-2258-4B63-8182-2C888DA66CF0'), char_to_uuid('1987c53c-7ab8-4461-9ebc-0d428744a8c0'), NULL, 'Condition=[HasSupport]=>*', 2);--#!

-- CONDITION HAS SUPPORT OF ANY
INSERT INTO rel_vrfy_systbl (rel_vrfy_id, rel_typ_cd_id, src_cls_cd_id, trg_cls_cd_id, err_desc, rel_cls) 
	VALUES(gen_uuid(), char_to_uuid('9871C3BC-B57A-479D-A031-7B56CB06FA84'), char_to_uuid('1987c53c-7ab8-4461-9ebc-0d428744a8c0'), NULL, 'Condition=[HasSubject]=>*', 2);--#!

alter table sub_adm_tbl alter rte_cd_id set default x'61D8F65C747E4A99982FA42AC5437473';--#!
alter table sub_adm_tbl alter DOS_UNT_CD_ID set default x'61D8F65C747E4A99982FA42AC5437473';--#!
alter table sub_adm_tbl drop constraint CK_SUB_ADM_RTE_CD; --#!
alter table sub_adm_tbl drop constraint CK_SUB_ADM_DOS_UNT_CD; --#!
alter table sub_adm_tbl add constraint CK_SUB_ADM_RTE_CD CHECK (ASSRT_CD_CLS(RTE_CD_ID, 'Route'));--#!
alter table sub_adm_tbl add constraint CK_SUB_ADM_DOS_UNT_CD CHECK (ASSRT_CD_CLS(DOS_UNT_CD_ID, 'UnitOfMeasure'));--#!
SELECT REG_PATCH('20220509-01') FROM RDB$DATABASE; --#!

