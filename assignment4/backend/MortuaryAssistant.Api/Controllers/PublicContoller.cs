using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;
    public PublicController(AppDbContext db) => _db = db;

    // ✅ Public: shows DB data to anyone (including not logged-in users)
    // Returns ONLY: CaseNumber, Status (string), CreatedAt
    [AllowAnonymous]
    [HttpGet("cases")]
    public async Task<IActionResult> PublicCases()
    {
        var data = await _db.CaseFiles
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.CaseNumber,
                Status = c.Status.ToString(),
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    // ✅ Protected: full case file details (Admin or Mortician)
    // Route: /api/public/cases/{caseNumber}
    [Authorize(Roles = "Admin,Mortician")]
    [HttpGet("cases/{caseNumber}")]
    public async Task<IActionResult> CaseDetails(string caseNumber)
    {
        if (string.IsNullOrWhiteSpace(caseNumber))
            return BadRequest(new { message = "Case number is required." });

        // Load full graph:
        // - Decedent (1:1)
        // - Tasks (and their WorkflowStepTemplate + AssignedToUser if present)
        // - Notes
        // - EquipmentCheckouts (and their Equipment)
        var c = await _db.CaseFiles
            .AsNoTracking()
            .Include(x => x.Decedent)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.WorkflowStepTemplate)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.AssignedToUser)
            .Include(x => x.Notes)
            .Include(x => x.EquipmentCheckouts)
                .ThenInclude(ec => ec.Equipment)
            .FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);

        if (c is null)
            return NotFound(new { message = "Case not found." });

        // DTO with REAL properties from your models.
        // Avoids circular reference issues and keeps payload consistent.
        var dto = new
        {
            c.Id,
            c.CaseNumber,
            Status = c.Status.ToString(),
            c.CreatedAt,
            c.NextOfKinName,

            Decedent = c.Decedent is null ? null : new
            {
                c.Decedent.Id,
                c.Decedent.CaseFileId,
                c.Decedent.FirstName,
                c.Decedent.LastName,
                c.Decedent.DateOfBirth,
                c.Decedent.DateOfDeath,
                c.Decedent.PlaceOfDeath,
                c.Decedent.TagNumber,
                c.Decedent.StorageLocation
            },

            Tasks = c.Tasks
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.CaseFileId,

                    t.WorkflowStepTemplateId,
                    WorkflowStepTemplate = t.WorkflowStepTemplate == null ? null : new
                    {
                        t.WorkflowStepTemplate.Id,
                        t.WorkflowStepTemplate.Name,
                        t.WorkflowStepTemplate.Description,
                        t.WorkflowStepTemplate.SortOrder,
                        t.WorkflowStepTemplate.IsActive
                    },

                    Status = t.Status.ToString(),
                    t.Notes,

                    t.AssignedToUserId,
                    AssignedToUser = t.AssignedToUser is null ? null : new
                    {
                        t.AssignedToUser.Id,
                        t.AssignedToUser.Email,
                        t.AssignedToUser.DisplayName
                    },

                    t.CreatedAt,
                    t.StartedAt,
                    t.CompletedAt
                })
                .ToList(),

            Notes = c.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.CaseFileId,
                    n.Text,
                    n.CreatedByUserId,
                    n.CreatedAt
                })
                .ToList(),

            EquipmentCheckouts = c.EquipmentCheckouts
                .OrderByDescending(ec => ec.CheckedOutAt)
                .Select(ec => new
                {
                    ec.Id,
                    ec.EquipmentId,

                    Equipment = ec.Equipment == null ? null : new
                    {
                        ec.Equipment.Id,
                        ec.Equipment.Name,
                        ec.Equipment.SerialNumber,
                        Status = ec.Equipment.Status.ToString(),
                        ec.Equipment.Location,
                        ec.Equipment.IsActive
                    },

                    ec.CaseFileId,
                    ec.CheckedOutByUserId,
                    ec.CheckedOutAt,
                    ec.ReturnedAt,
                    ec.Notes
                })
                .ToList()
        };

        return Ok(dto);
    }
}