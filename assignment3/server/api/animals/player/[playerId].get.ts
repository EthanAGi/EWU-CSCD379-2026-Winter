import { getPool } from '../../../utils/db'

export default defineEventHandler(async (event) => {
  const playerId = getRouterParam(event, 'playerId')

  console.log('----------------------------------------')
  console.log('[api] /animals/player hit')
  console.log('[api] playerId:', playerId)
  console.log('[api] node env:', process.env.NODE_ENV)
  console.log(
    '[api] has AZURE_SQL_CONNECTION_STRING:',
    Boolean(process.env.AZURE_SQL_CONNECTION_STRING)
  )
  console.log(
    '[api] has ConnectionStrings__DefaultConnection:',
    Boolean(process.env.ConnectionStrings__DefaultConnection)
  )

  if (!playerId) {
    console.error('[api] missing playerId')
    throw createError({
      statusCode: 400,
      statusMessage: 'playerId required',
    })
  }

  try {
    console.log('[api] calling getPool()…')
    const pool = await getPool()
    console.log('[api] SQL pool acquired')

    const req = pool.request()
    req.input('playerId', playerId)

    console.log('[api] running SQL query…')

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

    console.log(
      '[api] query complete, rows:',
      Array.isArray(result.recordset) ? result.recordset.length : 'none'
    )

    return result.recordset ?? []
  } catch (err: any) {
    console.error('[api] DB ERROR OCCURRED')
    console.error('[api] error name:', err?.name)
    console.error('[api] error message:', err?.message)
    console.error('[api] error code:', err?.code)
    console.error('[api] error stack:', err?.stack)

    throw createError({
      statusCode: 500,
      statusMessage: 'Database query failed',
    })
  } finally {
    console.log('[api] /animals/player handler finished')
    console.log('----------------------------------------')
  }
})
