#!/bin/bash
# db_reload.sh - Drop and reload from dump file
#before the the running script run this command first  export DB_PASS="your_password"
set -e

DB_NAME="${DB_NAME:-instrument_reference_data}"
DB_USER="${DB_USER:-root}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
export MYSQL_PWD="${DB_PASS:-}"
DUMP_FILE="schema_and_data_dump.sql"

if [ ! -f "$DUMP_FILE" ]; then
    echo "Error: Dump file '$DUMP_FILE' not found!"
    exit 1
fi

echo "Warning: This will drop and recreate the database '$DB_NAME'."
echo "Dropping and recreating database..."

# Connect to MySQL to drop and recreate the target database
mysql -u "$DB_USER" -h "$DB_HOST" -P "$DB_PORT" -e "DROP DATABASE IF EXISTS $DB_NAME; CREATE DATABASE $DB_NAME;"

echo "Reloading data from $DUMP_FILE..."
mysql -u "$DB_USER" -h "$DB_HOST" -P "$DB_PORT" "$DB_NAME" < "$DUMP_FILE"

echo "Database reload complete!"