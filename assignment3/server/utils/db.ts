import sql from 'mssql'

let pool: sql.ConnectionPool | null = null

export async function getPool() {
  if (pool) return pool

  const connStr = process.env.AZURE_SQL_CONNECTION_STRING
    || process.env.ConnectionStrings__DefaultConnection

  if (!connStr) {
    throw new Error('Missing SQL connection string')
  }

  pool = await sql.connect(connStr)
  return pool
}
