namespace Identity.Application.Auth;

public sealed record LoginResult(string Token, AuthUserDto User);
