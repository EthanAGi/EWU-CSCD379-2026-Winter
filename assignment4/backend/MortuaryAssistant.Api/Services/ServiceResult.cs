namespace MortuaryAssistant.Api.Services;

public sealed class ServiceResult<T>
{
    public bool Ok { get; }
    public int StatusCode { get; }
    public T? Value { get; }
    public string? Error { get; }

    private ServiceResult(bool ok, int statusCode, T? value, string? error)
    {
        Ok = ok;
        StatusCode = statusCode;
        Value = value;
        Error = error;
    }

    public static ServiceResult<T> Success(T value, int statusCode = 200)
        => new(true, statusCode, value, null);

    public static ServiceResult<T> Fail(string error, int statusCode = 400)
        => new(false, statusCode, default, error);
}