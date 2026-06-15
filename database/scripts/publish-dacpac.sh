#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 5 ]; then
  echo "Usage: publish-dacpac.sh <dacpac_path> <server_fqdn> <database_name> <login> <password>" >&2
  exit 1
fi

dacpac_path="$1"
server_fqdn="$2"
database_name="$3"
login="$4"
password="$5"

if [ ! -f "$dacpac_path" ]; then
  echo "DACPAC not found: $dacpac_path" >&2
  exit 1
fi

echo "Publishing $database_name to $server_fqdn from $dacpac_path"
sqlpackage /Action:Publish \
  /SourceFile:"$dacpac_path" \
  /TargetServerName:"$server_fqdn" \
  /TargetDatabaseName:"$database_name" \
  /TargetUser:"$login" \
  /TargetPassword:"$password" \
  /BlockOnPossibleDataLoss:True
