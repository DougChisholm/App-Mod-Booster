#!/usr/bin/env python3
"""
run-sql-dbrole.py
Grants the user-assigned managed identity the required SQL database roles
(db_datareader, db_datawriter, EXECUTE) by running script.sql.

The MANAGED-IDENTITY-NAME placeholder in script.sql is replaced with the
actual managed identity name before execution.

Works cross-platform (Linux / macOS / Windows WSL).
"""
import os
import pyodbc
import struct
from azure.identity import AzureCliCredential

# Database connection settings - updated by deploy.sh
SERVER          = "example.database.windows.net"
DATABASE        = "Northwind"
SQL_SCRIPT_FILE = "script.sql"


def get_access_token():
    """Get Azure AD access token using Azure CLI credentials"""
    credential = AzureCliCredential()
    token = credential.get_token("https://database.windows.net/.default")
    return token.token


def execute_sql_script(server, database, script_file):
    """Execute SQL script using Azure AD token authentication"""

    print("Getting Azure AD access token...")
    access_token = get_access_token()

    token_bytes  = access_token.encode("utf-16-le")
    token_struct = struct.pack(f"<I{len(token_bytes)}s", len(token_bytes), token_bytes)

    connection_string = (
        f"Driver={{ODBC Driver 18 for SQL Server}};"
        f"Server={server};"
        f"Database={database};"
        f"Encrypt=yes;"
        f"TrustServerCertificate=no;"
    )

    SQL_COPT_SS_ACCESS_TOKEN = 1256

    print(f"Connecting to {server}/{database}...")
    conn = pyodbc.connect(connection_string, attrs_before={SQL_COPT_SS_ACCESS_TOKEN: token_struct})

    try:
        print(f"Reading SQL script from {script_file}...")
        with open(script_file, 'r') as f:
            sql_script = f.read()

        statements    = []
        current_batch = []

        for line in sql_script.split('\n'):
            stripped = line.strip()
            if stripped.upper() == 'GO':
                if current_batch:
                    statements.append('\n'.join(current_batch))
                    current_batch = []
            elif stripped:
                current_batch.append(line)

        if current_batch:
            statements.append('\n'.join(current_batch))

        cursor = conn.cursor()
        for i, statement in enumerate(statements, 1):
            if statement.strip():
                print(f"Executing statement {i}/{len(statements)}...")
                try:
                    cursor.execute(statement)
                    conn.commit()
                    print(f"  ✓ Statement {i} executed successfully")
                except Exception as e:
                    print(f"  ✗ Error executing statement {i}: {e}")
                    raise

        print("\n✓ All SQL statements executed successfully!")

    finally:
        conn.close()


if __name__ == "__main__":
    # The MANAGED_IDENTITY_NAME env var is set by deploy.sh before calling this script.
    managed_identity_name = os.environ.get("MANAGED_IDENTITY_NAME", "")
    if not managed_identity_name:
        print("✗ MANAGED_IDENTITY_NAME environment variable is not set.")
        exit(1)

    # Replace placeholder - cross-platform using Python (works on Mac, Linux, Windows)
    with open(SQL_SCRIPT_FILE, 'r') as f:
        content = f.read()

    content = content.replace("MANAGED-IDENTITY-NAME", managed_identity_name)

    with open(SQL_SCRIPT_FILE, 'w') as f:
        f.write(content)

    print(f"Replaced MANAGED-IDENTITY-NAME with '{managed_identity_name}' in {SQL_SCRIPT_FILE}")

    try:
        execute_sql_script(SERVER, DATABASE, SQL_SCRIPT_FILE)
    except Exception as e:
        print(f"\n✗ Error: {e}")
        exit(1)
