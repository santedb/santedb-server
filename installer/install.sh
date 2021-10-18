#!/bin/bash

exit_on_error() {
 exit_code=$1
    last_command=${@:2}
    if [ $exit_code -ne 0 ]; then
        >&2 echo "\"${last_command}\" command failed with exit code ${exit_code}."
		echo "Installation failed - running as root?"; 
        exit $exit_code
    fi
}

declare -r INSTALL_ROOT='/opt/santesuite/santedb/server';
 
echo -e "\n
SanteDB iCDR Server Installation Script
Copyright (c) 2021 SanteSuite Inc. and the SanteSuite Contributors
Portions Copyright (C) 2019-2021 Fyfe Software Inc.
Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology

 Licensed under the Apache License, Version 2.0 (the "License"); you 
 may not use this file except in compliance with the License. You may 
 obtain a copy of the License at 
 
 http://www.apache.org/licenses/LICENSE-2.0 
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 License for the specific language governing permissions and limitations under 
 the License.
 
 "
 
 while ! [[ "$eula" =~ ^[ynYN]$ ]]
 do
 read -p "Do you accept the terms of the LICENSE? [y/n] : " eula
 done 
 
 if [[ "$eula" =~ ^[nN]$ ]] 
 then 
	echo "You must accept the terms of the license agreement";
	exit;
fi;

echo Installing at $INSTALL_ROOT
mkdir -p $INSTALL_ROOT || exit_on_error();

echo Copying Files
cp -rv * $INSTALL_ROOT || exit_on_error();

echo Installing MONO Framework
sudo apt install -y mono-complete || exit_on_error();

echo Installing Certificates 
mono $INSTALL_ROOT/SanteDB.exe --install-certs

echo Installing santedb.service
cat > /etc/systemd/system/santedb.service <<EOF
[Unit]
Description=SanteDB iCDR Server

[Service]
Type=Simple
RemainAfterExit=yes
ExecStart=/usr/bin/mono-service -d:$INSTALL_ROOT $INSTALL_ROOT/SanteDB.exe --console 
ExecStop=kill `cat /tmp/SanteDB.exe.lock`

[Install]
WantedBy=multi-user.target
EOF
 
echo -e "\n

SanteDB is now installed -

START SANTEDB: 
systemctl start santedb

STOP SANTEDB: 
systemctl stop santedb
"

 while ! [[ "$config" =~ ^[ynYN]$ ]]
 do
 read -p "Do you want to configure your SanteDB instance? [y/n] : " config
 done 
 
 if [[ "$config" =~ ^[yY]$ ]] 
 then 
	mono $INSTALL_ROOT/ConfigTool.exe
fi;