using AutoMapper;
using Identity.Application.Mappings.Resolvers;
using Identity.Application.Users;
using Identity.Domain.Users;

namespace Identity.Application.Mappings;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom<PasswordHashResolver>())
            .ForMember(dest => dest.CreatedOnUtc, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.ModifiedOnUtc, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow));

        CreateMap<UpdateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOnUtc, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForSourceMember(src => src.CurrentEmail, opt => opt.DoNotValidate());

        CreateMap<PatchUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOnUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOnUtc, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForSourceMember(src => src.CurrentEmail, opt => opt.DoNotValidate())
            .ForAllMembers(opt => opt.Condition((_, _, srcMember) => srcMember is not null));
    }
}