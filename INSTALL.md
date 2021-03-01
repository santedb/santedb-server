# Installation Notes

## Upgrading from SanteDB iCDR Server 2.0.xx to 2.1.0

SanteDB iCDR server 2.1.0 has undergone some code and binary clean up to 
correct issues with the FHIR interface, allowing for multiple matching 
configurations, and to refactor code to common libraries reducing 
overhead of maintaining the old PCL libraries.

To upgrade from 2.0.xx to 2.1.0 the following procedures should be taken:

1. Take a backup any configuration files edited in %INSTALL%\config (if any modifications were made)
2. Stop the SanteDB host process
3. Run the Windows Installer (or unzip the tarballs)
    + The Windows Installer confirm you want to remove the 2.0.xx files, click yes
    + If installing via tarball remove the files: ```SanteGuard.*.dll```, ```SanteMPI.*.dll``` and ```SanteDB.Core.dll```
4. Review either the ```santedb.npgsql.config.xml``` or ```santedb.fbsql.config.xml``` files
    + Any section definitions that have changed, modify them to match the 2.1.0 definition
    + Any service definitions that have changed assemblies or namespaces, modify to match the 2.1.0
    + You can reference [this commit](https://github.com/santedb/santedb-server/commit/84d595fbafcd339947d568486b5e0f8e5a69e269#diff-067fcba314c969cc18d047050f182a3d8a035f9cf469bc0568d6c1aec3a402e8) as a guide to changes
5. If you require SanteGuard or SanteMPI ensure that you unpack the appropriate 2.1.x packages to restore those plugins.

