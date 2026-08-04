using AutoMapper;
using Identity.Api.DTOs.Requests;
using Identity.Api.DTOs.Responses;
using Identity.Application.Users;

namespace Identity.Api.Mappings;

public sealed class UserApiMappingProfile : Profile
{
    public UserApiMappingProfile()
    {
        CreateMap<CreateUserRequestDto, CreateUserCommand>();
        CreateMap<UpdateUserRequestDto, UpdateUserCommand>();
        CreateMap<PatchUserRequestDto, PatchUserCommand>();
        CreateMap<UserDto, UserResponseDto>();
    }
}