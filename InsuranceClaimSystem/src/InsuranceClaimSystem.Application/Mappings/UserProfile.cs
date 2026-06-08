using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Auth;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

        CreateMap<User, AuthResponse>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Token, opt => opt.MapFrom(_ => string.Empty));

        CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Specialization, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationStatus, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationRejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.IsFirstLogin, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutUntil, opt => opt.Ignore())
            .ForMember(dest => dest.Policies, opt => opt.Ignore())
            .ForMember(dest => dest.Claims, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.KYCDocuments, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());
    }
}