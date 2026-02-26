using Microsoft.EntityFrameworkCore;
using MortuaryAssistant.Api.Data;

namespace MortuaryAssistant.Api.Services;

public sealed class CaseService : ICaseService
{
    private readonly AppDbContext _db;

    public CaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<object>> GetAllCasesAsync()
    {
        var data = await _db.CaseFiles
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.CaseNumber,
                Status = c.Status.ToString(),
                c.CreatedAt,
                c.AssignedMorticianUserId
            })
            .ToListAsync();

        return ServiceResult<object>.Success(data, 200);
    }
}