using ATSolution.SharedKernel.Constants;
using System.ComponentModel.DataAnnotations;

namespace Identity.Api.DTOs.Requests;

public sealed record UpdateUserRequestDto
{
    [Required]
    [MaxLength(UserFieldLengths.FirstName)]
    public string FirstName { get; init; } = null!;

    [Required]
    [MaxLength(UserFieldLengths.LastName)]
    public string LastName { get; init; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(UserFieldLengths.Email)]
    public string Email { get; init; } = null!;
}