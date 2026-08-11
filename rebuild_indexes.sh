#!/bin/bash
# rebuild_indexes.sh - Rebuild all indexes
#before the the running script run this command first  export DB_PASS="your_password"
set -e

DB_NAME="${DB_NAME:-instrument_reference_data}"
DB_USER="${DB_USER:-root}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
export MYSQL_PWD="${DB_PASS:-}"

echo "Starting index rebuild (optimization) for all tables in $DB_NAME..."

# mysqlcheck --optimize defragments tables and rebuilds indexes
mysqlcheck -u "$DB_USER" -h "$DB_HOST" -P "$DB_PORT" --optimize "$DB_NAME"

echo "All indexes rebuilt successfully!"