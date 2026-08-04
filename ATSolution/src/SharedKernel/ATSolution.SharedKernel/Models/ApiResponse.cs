using System.Text.Json.Serialization;
using ATSolution.SharedKernel.Constants;

namespace ATSolution.SharedKernel.Models;

public sealed record ApiResponse
{
    [JsonPropertyName(ResponsePropertyNames.Success)]
    public bool Success { get; init; }

    [JsonPropertyName(ResponsePropertyNames.Message)]
    public string Message { get; init; } = string.Empty;

    public static ApiResponse Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static ApiResponse Failed(string message) =>
        new() { Success = false, Message = message };
}
