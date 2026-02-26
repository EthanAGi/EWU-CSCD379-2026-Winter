namespace MortuaryAssistant.Api.Services;

public interface ICaseService
{
    Task<ServiceResult<object>> GetAllCasesAsync();
}