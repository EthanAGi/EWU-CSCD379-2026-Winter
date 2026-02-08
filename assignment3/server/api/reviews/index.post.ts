import { getPool } from '../../utils/db'

type ReviewBody = {
  playerName: string
  body: string
  rating: number
}

export default defineEventHandler(async (event) => {
  const body = await readBody<ReviewBody>(event)

  const playerName = (body?.playerName ?? '').trim()
  const text = (body?.body ?? '').trim()
  const rating = Number(body?.rating)

  if (!playerName) throw createError({ statusCode: 400, statusMessage: 'playerName required' })
  if (text.length < 5) throw createError({ statusCode: 400, statusMessage: 'body too short' })
  if (!(rating >= 1 && rating <= 5)) throw createError({ statusCode: 400, statusMessage: 'rating must be 1-5' })

  const pool = await getPool()
  const req = pool.request()
  req.input('playerName', playerName)
  req.input('body', text)
  req.input('rating', rating)

  const inserted = await req.query(`
    INSERT INTO Reviews (PlayerName, Body, Rating, CreatedAtUtc)
    OUTPUT
      inserted.Id as id,
      inserted.PlayerName as playerName,
      inserted.Body as body,
      inserted.Rating as rating,
      inserted.CreatedAtUtc as createdAtUtc
    VALUES (@playerName, @body, @rating, SYSUTCDATETIME())
  `)

  return inserted.recordset?.[0]
})
