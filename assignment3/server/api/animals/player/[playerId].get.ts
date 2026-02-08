import { getPool } from '../../../utils/db'

export default defineEventHandler(async (event) => {
  const playerId = getRouterParam(event, 'playerId')
  if (!playerId) throw createError({ statusCode: 400, statusMessage: 'playerId required' })

  const pool = await getPool()
  const req = pool.request()
  req.input('playerId', playerId)

  const result = await req.query(`
    SELECT
      Id as id,
      OwnerPlayerId as ownerPlayerId,
      OwnerName as ownerName,
      Name as name,
      Kind as kind,
      Attack as attack,
      Defense as defense,
      Affection as affection,
      Level as level,
      HpMax as hpMax,
      HpCurrent as hpCurrent,
      CreatedAt as createdAt,
      TemplateId as templateId
    FROM PlayerAnimals
    WHERE OwnerPlayerId = @playerId
    ORDER BY CreatedAt DESC
  `)

  return result.recordset ?? []
})
