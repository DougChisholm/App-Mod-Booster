#!/usr/bin/env python3
"""
run-sql-stored-procs.py
Deploys stored-procedures.sql to Azure SQL Database using
Azure Active Directory (CLI) authentication.
Works cross-platform (Linux / macOS / Windows WSL).
"""
import pyodbc
import struct
from azure.identity import AzureCliCredential

# Database connection settings - updated by deploy.sh
SERVER          = "example.database.windows.net"
DATABASE        = "Northwind"
SQL_SCRIPT_FILE = "stored-procedures.sql"


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

        print("\n✓ All stored procedures deployed successfully!")

    finally:
        conn.close()


if __name__ == "__main__":
    try:
        execute_sql_script(SERVER, DATABASE, SQL_SCRIPT_FILE)
    except Exception as e:
        print(f"\n✗ Error: {e}")
        exit(1)
