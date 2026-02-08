import { getPool } from '../../utils/db'

export default defineEventHandler(async () => {
  const pool = await getPool()

  const result = await pool.request().query(`
    SELECT Id as id, Kind as kind, Attack as attack, Defense as defense,
           Affection as affection, Level as level, HpMax as hpMax
    FROM AnimalTemplates
    ORDER BY Id ASC
  `)

  return result.recordset ?? []
})
