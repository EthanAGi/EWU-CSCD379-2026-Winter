import { getPool } from '../../utils/db'

type ClaimBody = {
  ownerPlayerId: string
  ownerName: string
  kind: string
  name: string
}

export default defineEventHandler(async (event) => {
  const body = await readBody<ClaimBody>(event)

  if (!body?.ownerPlayerId || !body?.ownerName || !body?.kind || !body?.name) {
    throw createError({ statusCode: 400, statusMessage: 'Missing fields' })
  }

  const pool = await getPool()

  // Find template for this kind
  const tReq = pool.request()
  tReq.input('kind', body.kind)

  const tpl = await tReq.query(`
    SELECT TOP 1 Id, Kind, Attack, Defense, Affection, Level, HpMax
    FROM AnimalTemplates
    WHERE Kind = @kind
  `)

  const t = tpl.recordset?.[0]
  if (!t) throw createError({ statusCode: 400, statusMessage: `Unknown kind: ${body.kind}` })

  // Insert PlayerAnimal
  const iReq = pool.request()
  iReq.input('ownerPlayerId', body.ownerPlayerId)
  iReq.input('ownerName', body.ownerName)
  iReq.input('name', body.name)
  iReq.input('kind', t.Kind)
  iReq.input('attack', t.Attack)
  iReq.input('defense', t.Defense)
  iReq.input('affection', t.Affection)
  iReq.input('level', t.Level)
  iReq.input('hpMax', t.HpMax)
  iReq.input('hpCurrent', t.HpMax)
  iReq.input('templateId', t.Id)

  const inserted = await iReq.query(`
    INSERT INTO PlayerAnimals
      (OwnerPlayerId, OwnerName, Name, Kind, Attack, Defense, Affection, Level, HpMax, HpCurrent, CreatedAt, TemplateId)
    OUTPUT
      inserted.Id as id,
      inserted.OwnerPlayerId as ownerPlayerId,
      inserted.OwnerName as ownerName,
      inserted.Name as name,
      inserted.Kind as kind,
      inserted.Attack as attack,
      inserted.Defense as defense,
      inserted.Affection as affection,
      inserted.Level as level,
      inserted.HpMax as hpMax,
      inserted.HpCurrent as hpCurrent,
      inserted.CreatedAt as createdAt,
      inserted.TemplateId as templateId
    VALUES
      (@ownerPlayerId, @ownerName, @name, @kind, @attack, @defense, @affection, @level, @hpMax, @hpCurrent, SYSUTCDATETIME(), @templateId)
  `)

  return inserted.recordset?.[0]
})
