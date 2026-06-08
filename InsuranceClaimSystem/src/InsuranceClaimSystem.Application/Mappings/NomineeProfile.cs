using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class NomineeProfile : Profile
{
    public NomineeProfile()
    {
        CreateMap<Nominee, NomineeDto>()
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship.ToString()));

        CreateMap<NomineeRequest, Nominee>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.ContactEmail))
            .ForMember(dest => dest.SharePercentage, opt => opt.MapFrom(src => src.SharePercentage))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyId, opt => opt.MapFrom(src => src.PolicyId))
            .ForMember(dest => dest.PolicyHolderId, opt => opt.Ignore())
            .ForMember(dest => dest.EncryptedAadhaar, opt => opt.Ignore())
            .ForMember(dest => dest.AadhaarKeyReference, opt => opt.Ignore())
            .ForMember(dest => dest.AadhaarMasked, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Policy, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyHolder, opt => opt.Ignore());
    }
}