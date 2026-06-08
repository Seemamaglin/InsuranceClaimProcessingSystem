namespace InsuranceClaimSystem.Application.Common;

public class ApiResponse
{
    public bool Success { get; }
    public string Message { get; }
    public List<Error> Errors { get; }

    private ApiResponse(bool success, string message, List<Error> errors)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse Ok(string message = "") => new(true, message, new List<Error>());

    public static ApiResponse Fail(string message, List<Error>? errors = null) =>
        new(false, message, errors ?? new List<Error>());
}

public class ApiResponse<T>
{
    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }
    public List<Error> Errors { get; }

    private ApiResponse(bool success, string message, T? data, List<Error> errors)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new(true, message, data, new List<Error>());

    public static ApiResponse<T> Fail(string message, List<Error>? errors = null) =>
        new(false, message, default, errors ?? new List<Error>());

    public static ApiResponse<T> FromResult(Result<T> result, string successMessage = "")
    {
        if (result.IsSuccess)
            return Ok(result.Value, successMessage);

        return Fail(result.Error.Description, new List<Error> { result.Error });
    }
}