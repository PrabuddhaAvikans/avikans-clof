using ATSolution.SharedKernel.Constants;
using System.ComponentModel.DataAnnotations;

namespace Identity.Api.DTOs.Requests;

public sealed record PatchUserRequestDto
{
    [MaxLength(UserFieldLengths.FirstName)]
    public string? FirstName { get; init; }

    [MaxLength(UserFieldLengths.LastName)]
    public string? LastName { get; init; }

    [EmailAddress]
    [MaxLength(UserFieldLengths.Email)]
    public string? Email { get; init; }
}