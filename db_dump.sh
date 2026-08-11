#!/bin/bash
# db_dump.sh - Full schema and data dump
#before the the running script run this command first  export DB_PASS="your_password"
set -e

DB_NAME="${DB_NAME:-instrument_reference_data}"
DB_USER="${DB_USER:-root}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
export MYSQL_PWD="${DB_PASS:-}"
OUTPUT_FILE="schema_and_data_dump.sql"

echo "Starting database dump for $DB_NAME..."

# mysqldump creates the full schema and data dump
mysqldump -u "$DB_USER" -h "$DB_HOST" -P "$DB_PORT" "$DB_NAME" > "$OUTPUT_FILE"

echo "Dump complete! Saved to $OUTPUT_FILE"