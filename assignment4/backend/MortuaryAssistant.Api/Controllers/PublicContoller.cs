using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICaseService _cases;
    private readonly UserManager<ApplicationUser> _users;

    public PublicController(AppDbContext db, ICaseService cases, UserManager<ApplicationUser> users)
    {
        _db = db;
        _cases = cases;
        _users = users;
    }

    // ✅ Public: shows DB data to anyone (including not logged-in users)
    // Uses CaseService (ICaseService)
    [AllowAnonymous]
    [HttpGet("cases")]
    public async Task<IActionResult> PublicCases()
    {
        var sr = await _cases.GetAllCasesAsync();

        if (sr.Ok)
            return StatusCode(sr.StatusCode, sr.Value);

        return StatusCode(sr.StatusCode, new { message = sr.Error ?? "Request failed." });
    }

    // ✅ Protected: full case file details (Admin or Mortician)
    // Route: /api/public/cases/{caseNumber}
    //
    // NOTE:
    // Your current ICaseService only exposes GetAllCasesAsync(), so the "details"
    // endpoint still queries the DbContext directly for the rich graph.
    [Authorize(Roles = "Admin,Mortician")]
    [HttpGet("cases/{caseNumber}")]
    public async Task<IActionResult> CaseDetails(string caseNumber)
    {
        if (string.IsNullOrWhiteSpace(caseNumber))
            return BadRequest(new { message = "Case number is required." });

        var c = await _db.CaseFiles
            .AsNoTracking()
            .Include(x => x.Decedent)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.WorkflowStepTemplate)
            .Include(x => x.Notes)
            .Include(x => x.EquipmentCheckouts)
                .ThenInclude(ec => ec.Equipment)
            .FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);

        if (c is null)
            return NotFound(new { message = "Case not found." });

        // Resolve assigned mortician via Identity (no EF navigation required)
        var assignedMortician = string.IsNullOrWhiteSpace(c.AssignedMorticianUserId)
            ? null
            : await _users.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == c.AssignedMorticianUserId)
                .Select(u => new { u.Id, u.Email, u.DisplayName })
                .FirstOrDefaultAsync();

        // Resolve AssignedToUser for tasks in one query (avoids relying on navigation properties)
        var assignedTaskUserIds = c.Tasks
            .Select(t => t.AssignedToUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var taskUsers = assignedTaskUserIds.Count == 0
            ? new Dictionary<string, object>()
            : await _users.Users
                .IgnoreQueryFilters()
                .Where(u => assignedTaskUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => (object)new { x.Id, x.Email, x.DisplayName });

        var dto = new
        {
            c.Id,
            c.CaseNumber,
            Status = c.Status.ToString(),
            c.CreatedAt,
            c.NextOfKinName,

            // assignment at CASE level
            c.AssignedMorticianUserId,
            AssignedMortician = assignedMortician,

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
                    AssignedToUser = (!string.IsNullOrWhiteSpace(t.AssignedToUserId) &&
                                      taskUsers.TryGetValue(t.AssignedToUserId!, out var u))
                        ? u
                        : null,

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