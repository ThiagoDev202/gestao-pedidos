#!/usr/bin/env bash
set -euo pipefail

sqlcmd=/opt/mssql-tools18/bin/sqlcmd

for attempt in $(seq 1 30); do
  if "$sqlcmd" -C -S "$DB_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  echo "aguardando o SQL Server (tentativa $attempt)"
  sleep 2
done

for script in /scripts/0[1-3]-*.sql; do
  echo "executando $script"
  "$sqlcmd" -C -b -S "$DB_HOST" -U sa -P "$MSSQL_SA_PASSWORD" -i "$script"
done

echo "banco pronto"
