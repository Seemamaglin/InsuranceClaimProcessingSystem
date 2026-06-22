using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class PolicyProfile : Profile
{
    public PolicyProfile()
    {
        CreateMap<Policy, PolicyResponse>()
            .ForMember(dest => dest.PolicyHolderName, opt => opt.MapFrom(src => $"{src.PolicyHolder.FirstName} {src.PolicyHolder.LastName}"))
            .ForMember(dest => dest.PolicyTypeName, opt => opt.MapFrom(src => src.PolicyType.TypeName));

        CreateMap<ApplyForPolicyRequest, Policy>()
            .ForMember(dest => dest.PolicyTypeId, opt => opt.MapFrom(src => src.PolicyTypeId))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.Ignore())
            .ForMember(dest => dest.CoverageAmount, opt => opt.MapFrom(src => src.CoverageAmount))
            .ForMember(dest => dest.PremiumAmount, opt => opt.MapFrom(src => src.PremiumAmount))
            .ForMember(dest => dest.PremiumFrequency, opt => opt.MapFrom(src => src.PremiumFrequency))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyNumber, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.RemainingCoverageAmount, opt => opt.Ignore())
            .ForMember(dest => dest.NextPremiumDueDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastPremiumPaidDate, opt => opt.Ignore())
            .ForMember(dest => dest.GracePeriodDays, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyDocumentUrl, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.LapsedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyHolder, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyType, opt => opt.Ignore())
            .ForMember(dest => dest.Claims, opt => opt.Ignore())
            .ForMember(dest => dest.Nominees, opt => opt.Ignore())
            .ForMember(dest => dest.PolicyPayments, opt => opt.Ignore())
            .ForMember(dest => dest.HealthRecord, opt => opt.Ignore());
    }
}