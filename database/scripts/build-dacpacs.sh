#!/usr/bin/env bash
set -euo pipefail

database_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
solution="$database_root/VitalNexus.Database.sln"
output_dir="${1:-$database_root/artifacts/dacpacs}"
configuration="${CONFIGURATION:-Release}"

declare -a projects=(
  "VitalNexus.AccountBusiness.Database|Accounts|core"
  "VitalNexus.LabMarkersData.Database|LabMarkersData|core"
  "VitalNexus.PatientHealth.Database|PatientHealth|phi"
)

echo "Restoring $solution"
dotnet restore "$solution"

echo "Building $solution ($configuration)"
dotnet build "$solution" --configuration "$configuration" --no-restore

rm -rf "$output_dir"
mkdir -p "$output_dir"

manifest="$output_dir/dacpac-manifest.json"
generated_at="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
git_sha="${GITHUB_SHA:-local}"

echo "{" > "$manifest"
echo "  \"generatedAt\": \"$generated_at\"," >> "$manifest"
echo "  \"gitSha\": \"$git_sha\"," >> "$manifest"
echo "  \"configuration\": \"$configuration\"," >> "$manifest"
echo "  \"dacpacs\": [" >> "$manifest"

first_entry=1
for entry in "${projects[@]}"; do
  IFS='|' read -r project_name database_name server_role <<< "$entry"
  project_dir="$database_root/$project_name"
  dacpac=$(find "$project_dir/bin/$configuration" -name '*.dacpac' -print -quit)

  if [ -z "$dacpac" ]; then
    echo "Missing DACPAC for $project_name" >&2
    exit 1
  fi

  artifact_name="${project_name}.dacpac"
  cp "$dacpac" "$output_dir/$artifact_name"
  unzip -t "$output_dir/$artifact_name" >/dev/null

  if [ "$first_entry" -eq 0 ]; then
    echo "," >> "$manifest"
  fi
  first_entry=0

  printf '    {\n      "project": "%s",\n      "file": "%s",\n      "databaseName": "%s",\n      "serverRole": "%s"\n    }' \
    "$project_name" "$artifact_name" "$database_name" "$server_role" >> "$manifest"
  echo "Packaged $artifact_name from $dacpac"
done

echo "" >> "$manifest"
echo "  ]" >> "$manifest"
echo "}" >> "$manifest"

echo "DACPAC manifest written to $manifest"
