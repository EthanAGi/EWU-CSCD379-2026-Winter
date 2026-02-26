using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Constants;
using MortuaryAssistant.Api.Data;
using MortuaryAssistant.Api.Models;
using MortuaryAssistant.Api.Services;

namespace MortuaryAssistant.Api.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
public class CaseFilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICaseService _cases;

    public CaseFilesController(AppDbContext db, UserManager<ApplicationUser> users, ICaseService cases)
    {
        _db = db;
        _users = users;
        _cases = cases;
    }

    // ✅ List cases:
    // - scope=mine  -> mortician gets only assigned cases
    // - scope=all   -> admin gets all cases (delegates to CaseService)
    [HttpGet]
    public async Task<IActionResult> ListCases([FromQuery] string scope = "mine")
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // ✅ "all" is handled by the service (anything the service does, we leave to it)
        if (string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!User.IsInRole(Roles.Admin))
                return Forbid();

            var sr = await _cases.GetAllCasesAsync();
            return ToActionResult(sr);
        }

        // scope=mine (not provided by CaseService, so controller keeps the richer query)
        IQueryable<CaseFile> q = _db.CaseFiles.Include(x => x.Decedent);

        if (User.IsInRole(Roles.Mortician))
        {
            q = q.Where(x => x.AssignedMorticianUserId == user.Id);
        }
        else if (User.IsInRole(Roles.Admin))
        {
            // Admin "mine" -> all by default (your current behavior)
        }
        else
        {
            return Forbid();
        }

        // ✅ Resolve assigned mortician via left join on Users, ignoring any global filters on ApplicationUser
        var results = await (
            from c in q
            join m in _users.Users.IgnoreQueryFilters()
                on c.AssignedMorticianUserId equals m.Id into mortJoin
            from m in mortJoin.DefaultIfEmpty()
            orderby c.CreatedAt descending
            select new
            {
                caseNumber = c.CaseNumber,
                status = c.Status.ToString(),
                createdAt = c.CreatedAt,
                nextOfKinName = c.NextOfKinName,
                decedentName = c.Decedent == null
                    ? null
                    : (c.Decedent.FirstName + " " + c.Decedent.LastName).Trim(),
                assignedMortician = m == null ? null : new
                {
                    m.Id,
                    m.Email,
                    m.DisplayName
                }
            }
        ).ToListAsync();

        return Ok(results);
    }

    // ✅ Create case (Mortician or Admin)
    // ✅ Auto-assign:
    //    - If creator is Mortician -> assign creator
    //    - Else if creator is Admin and AssignedMorticianUserId provided -> assign that mortician (validated)
    // ✅ Optionally creates Decedent
    // ✅ Auto-creates tasks from active WorkflowStepTemplate rows
    [HttpPost]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CaseNumber))
            return BadRequest(new { message = "CaseNumber is required." });

        var creator = await _users.GetUserAsync(User);
        if (creator == null) return Unauthorized();

        var caseNumber = request.CaseNumber.Trim();

        var exists = await _db.CaseFiles.AnyAsync(x => x.CaseNumber == caseNumber);
        if (exists)
            return Conflict(new { message = "That case number already exists." });

        var entity = new CaseFile
        {
            CaseNumber = caseNumber,
            Status = CaseStatus.Intake,
            NextOfKinName = request.NextOfKinName?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };

        var creatorIsMortician =
            User.IsInRole(Roles.Mortician) || await _users.IsInRoleAsync(creator, Roles.Mortician);

        var creatorIsAdmin =
            User.IsInRole(Roles.Admin) || await _users.IsInRoleAsync(creator, Roles.Admin);

        if (creatorIsMortician)
        {
            entity.AssignedMorticianUserId = creator.Id;
        }
        else if (creatorIsAdmin)
        {
            var requestedId = request.AssignedMorticianUserId?.Trim();
            if (!string.IsNullOrWhiteSpace(requestedId))
            {
                var mort = await _users.FindByIdAsync(requestedId);
                if (mort == null)
                    return BadRequest(new { message = "AssignedMorticianUserId is invalid (user not found)." });

                if (!await _users.IsInRoleAsync(mort, Roles.Mortician))
                    return BadRequest(new { message = "AssignedMorticianUserId must belong to a Mortician user." });

                entity.AssignedMorticianUserId = mort.Id;
            }
        }

        if (request.Decedent != null &&
            (!string.IsNullOrWhiteSpace(request.Decedent.FirstName) ||
             !string.IsNullOrWhiteSpace(request.Decedent.LastName)))
        {
            entity.Decedent = new Decedent
            {
                FirstName = request.Decedent.FirstName?.Trim() ?? "",
                LastName = request.Decedent.LastName?.Trim() ?? "",
                DateOfBirth = request.Decedent.DateOfBirth,
                DateOfDeath = request.Decedent.DateOfDeath,
                PlaceOfDeath = string.IsNullOrWhiteSpace(request.Decedent.PlaceOfDeath) ? null : request.Decedent.PlaceOfDeath.Trim(),
                TagNumber = string.IsNullOrWhiteSpace(request.Decedent.TagNumber) ? null : request.Decedent.TagNumber.Trim(),
                StorageLocation = string.IsNullOrWhiteSpace(request.Decedent.StorageLocation) ? null : request.Decedent.StorageLocation.Trim()
            };
        }

        _db.CaseFiles.Add(entity);
        await _db.SaveChangesAsync();

        var templates = await _db.WorkflowStepTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        if (templates.Count > 0)
        {
            foreach (var t in templates)
            {
                _db.CaseTasks.Add(new CaseTask
                {
                    CaseFileId = entity.Id,
                    WorkflowStepTemplateId = t.Id,
                    Status = CaseTaskStatus.Todo,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        return Ok(new { caseNumber = entity.CaseNumber });
    }

    // 🔎 Get case with assigned mortician + decedent
    [HttpGet("{caseNumber}")]
    public async Task<IActionResult> GetCase(string caseNumber)
    {
        var c = await _db.CaseFiles
            .Include(x => x.Decedent)
            .FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);

        if (c == null)
            return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        var m = await _users.Users.IgnoreQueryFilters()
            .Where(u => u.Id == c.AssignedMorticianUserId)
            .Select(u => new { u.Id, u.Email, u.DisplayName })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            c.Id,
            c.CaseNumber,
            status = c.Status.ToString(),
            c.CreatedAt,
            nextOfKinName = c.NextOfKinName,
            assignedMortician = m,
            decedent = c.Decedent == null ? null : new
            {
                c.Decedent.Id,
                firstName = c.Decedent.FirstName,
                lastName = c.Decedent.LastName,
                dateOfBirth = c.Decedent.DateOfBirth,
                dateOfDeath = c.Decedent.DateOfDeath,
                placeOfDeath = c.Decedent.PlaceOfDeath,
                tagNumber = c.Decedent.TagNumber,
                storageLocation = c.Decedent.StorageLocation
            }
        });
    }

    // ✅ Assignment info endpoint
    [HttpGet("{caseNumber}/assignment")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> GetAssignment(string caseNumber)
    {
        var c = await _db.CaseFiles
            .FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);

        if (c == null)
            return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        var m = await _users.Users.IgnoreQueryFilters()
            .Where(u => u.Id == c.AssignedMorticianUserId)
            .Select(u => new { u.Id, u.Email, u.DisplayName })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            caseNumber = c.CaseNumber,
            isAssigned = c.AssignedMorticianUserId != null,
            assignedMortician = m
        });
    }

    // 🟢 Assign Mortician (Admin only)
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{caseNumber}/assign-mortician")]
    public async Task<IActionResult> AssignMortician(string caseNumber, [FromBody] AssignMorticianRequest request)
    {
        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        var user = await _users.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { message = "User not found." });

        if (!await _users.IsInRoleAsync(user, Roles.Mortician))
            return BadRequest(new { message = "User is not a Mortician." });

        c.AssignedMorticianUserId = user.Id;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Mortician assigned successfully.",
            caseNumber = c.CaseNumber,
            assignedMortician = new
            {
                user.Id,
                user.Email,
                user.DisplayName
            }
        });
    }

    // ✅ Get workflow tasks for a case (includes WorkflowStepTemplate)
    [HttpGet("{caseNumber}/tasks")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> GetTasks(string caseNumber)
    {
        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        var list = await _db.CaseTasks
            .Where(t => t.CaseFileId == c.Id)
            .Include(t => t.WorkflowStepTemplate)
            .OrderBy(t => t.WorkflowStepTemplate.SortOrder)
            .Select(t => new
            {
                t.Id,
                workflowStepTemplate = new
                {
                    t.WorkflowStepTemplate.Id,
                    t.WorkflowStepTemplate.Name,
                    t.WorkflowStepTemplate.Description,
                    t.WorkflowStepTemplate.SortOrder
                },
                status = t.Status.ToString(),
                notes = t.Notes,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // ✅ Update a task: status + notes
    [HttpPatch("{caseNumber}/tasks/{taskId:int}")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> UpdateTask(string caseNumber, int taskId, [FromBody] UpdateTaskRequest request)
    {
        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        if (c.Status == CaseStatus.Completed)
            return BadRequest(new { message = "Case is already completed." });

        var task = await _db.CaseTasks
            .Include(t => t.WorkflowStepTemplate)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CaseFileId == c.Id);

        if (task == null) return NotFound(new { message = "Task not found." });

        if (!Enum.TryParse<CaseTaskStatus>(request.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { message = "Invalid task status." });

        task.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var now = DateTime.UtcNow;
        task.Status = newStatus;

        if (newStatus == CaseTaskStatus.InProgress && task.StartedAt == null)
            task.StartedAt = now;

        if (newStatus == CaseTaskStatus.Done)
        {
            if (task.StartedAt == null) task.StartedAt = now;
            task.CompletedAt = now;
        }
        else
        {
            task.CompletedAt = null;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ✅ Complete case: sets CaseStatus.Completed only if all tasks are Done
    [HttpPatch("{caseNumber}/complete")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> CompleteCase(string caseNumber)
    {
        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        if (c.Status == CaseStatus.Completed)
            return NoContent();

        var anyNotDone = await _db.CaseTasks
            .AnyAsync(t => t.CaseFileId == c.Id && t.Status != CaseTaskStatus.Done);

        if (anyNotDone)
            return BadRequest(new { message = "All workflow steps must be Done before completing the case." });

        c.Status = CaseStatus.Completed;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ✅ Notes: list
    [HttpGet("{caseNumber}/notes")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> GetNotes(string caseNumber)
    {
        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        var list = await _db.CaseNotes
            .Where(n => n.CaseFileId == c.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                text = n.Text,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    // ✅ Notes: add
    [HttpPost("{caseNumber}/notes")]
    [Authorize(Roles = Roles.Mortician + "," + Roles.Admin)]
    public async Task<IActionResult> AddNote(string caseNumber, [FromBody] AddNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Text is required." });

        var c = await _db.CaseFiles.FirstOrDefaultAsync(x => x.CaseNumber == caseNumber);
        if (c == null) return NotFound(new { message = "Case not found." });

        await EnforceCaseAccess(c);

        var user = await _users.GetUserAsync(User);

        var note = new CaseNote
        {
            CaseFileId = c.Id,
            Text = request.Text.Trim(),
            CreatedByUserId = user?.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.CaseNotes.Add(note);
        await _db.SaveChangesAsync();

        return Ok(new { note.Id });
    }

    // ---- helpers

    private IActionResult ToActionResult(ServiceResult<object> result)
    {
        // ✅ FIX: ServiceResult<T> uses "Ok" (bool) not "Success".
        // "Success" is a static factory method, so "result.Success" becomes a method group (CS0428).
        if (result.Ok)
            return StatusCode(result.StatusCode, result.Value);

        return StatusCode(result.StatusCode, new { message = result.Error ?? "Request failed." });
    }

    private async Task EnforceCaseAccess(CaseFile c)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            throw new UnauthorizedAccessException();

        var isAdmin = User.IsInRole(Roles.Admin);
        var isMortician = User.IsInRole(Roles.Mortician);

        if (isAdmin) return;

        if (isMortician && c.AssignedMorticianUserId == user.Id) return;

        throw new UnauthorizedAccessException();
    }
}

public record AssignMorticianRequest(string UserId);

public record CreateCaseRequest(
    string CaseNumber,
    string? NextOfKinName,
    CreateDecedentRequest? Decedent,
    string? AssignedMorticianUserId
);

public record CreateDecedentRequest(
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    DateTime? DateOfDeath,
    string? PlaceOfDeath,
    string? TagNumber,
    string? StorageLocation
);

public record UpdateTaskRequest(string Status, string? Notes);

public record AddNoteRequest(string Text);