#!/bin/bash
# stale_data_report.sh - Run stale_instruments_vw and report instruments not updated in 30+ days
#before the the running script run this command first  export DB_PASS="your_password"
set -e

DB_NAME="${DB_NAME:-instrument_reference_data}"
DB_USER="${DB_USER:-root}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
export MYSQL_PWD="${DB_PASS:-}"
OUTPUT_FILE="stale_instruments_report_$(date +%F).csv"

echo "Generating stale data report from 'stale_instruments_vw'..."

# -B runs in batch mode (tab separated), sed converts tabs to commas for CSV formatting
mysql -u "$DB_USER" -h "$DB_HOST" -P "$DB_PORT" -B -e "SELECT * FROM stale_instruments_vw;" "$DB_NAME" | sed 's/\t/,/g' > "$OUTPUT_FILE"

echo "Report generated successfully! Saved to $OUTPUT_FILE"