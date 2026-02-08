import { getPool } from '../../utils/db'

export default defineEventHandler(async (event) => {
  const q = getQuery(event)
  const take = Math.min(100, Math.max(1, Number(q.take ?? 50)))

  const pool = await getPool()
  const req = pool.request()
  req.input('take', take)

  const result = await req.query(`
    SELECT TOP (@take)
      Id as id,
      PlayerName as playerName,
      Body as body,
      Rating as rating,
      CreatedAtUtc as createdAtUtc
    FROM Reviews
    ORDER BY CreatedAtUtc DESC
  `)

  return result.recordset ?? []
})
