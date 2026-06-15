#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 6 ]; then
  echo "Usage: detect-schema-drift.sh <dacpac_path> <server_fqdn> <database_name> <login> <password> <report_dir> [report_label]" >&2
  exit 1
fi

dacpac_path="$1"
server_fqdn="$2"
database_name="$3"
login="$4"
password="$5"
report_dir="$6"
report_label="${7:-$database_name}"

if [ ! -f "$dacpac_path" ]; then
  echo "DACPAC not found: $dacpac_path" >&2
  exit 1
fi

mkdir -p "$report_dir"
report_file="$report_dir/deploy-report-${report_label}.xml"

echo "Checking schema drift for $database_name on $server_fqdn"
sqlpackage /Action:DeployReport \
  /SourceFile:"$dacpac_path" \
  /TargetServerName:"$server_fqdn" \
  /TargetDatabaseName:"$database_name" \
  /TargetUser:"$login" \
  /TargetPassword:"$password" \
  /OutputPath:"$report_file"

python3 - "$report_file" "$database_name" <<'PY'
import sys
import xml.etree.ElementTree as ET

report_path = sys.argv[1]
database_name = sys.argv[2]

tree = ET.parse(report_path)
root = tree.getroot()

operations = [element for element in root.iter() if element.tag.endswith("Operation")]
alerts = [element for element in root.iter() if element.tag.endswith("Alert")]

if not operations and not alerts:
    print(f"No schema drift detected for {database_name}")
    sys.exit(0)

print(f"Schema drift detected for {database_name}")
print(f"Report: {report_path}")
print(f"Pending operations: {len(operations)}")
print(f"Alerts: {len(alerts)}")

for operation in operations[:25]:
    name = operation.attrib.get("Name", "")
    item_type = operation.attrib.get("ItemType", "")
    item_name = operation.attrib.get("ItemName", operation.attrib.get("Value", ""))
    print(f"  - {name} {item_type} {item_name}".strip())

if len(operations) > 25:
    print(f"  ... and {len(operations) - 25} more operations")

for alert in alerts[:10]:
    alert_id = alert.attrib.get("Id", alert.attrib.get("Name", "Alert"))
    print(f"  - Alert: {alert_id}")

sys.exit(1)
PY
