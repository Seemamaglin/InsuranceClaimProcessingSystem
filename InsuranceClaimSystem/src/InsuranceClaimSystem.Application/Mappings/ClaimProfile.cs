using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class ClaimProfile : Profile
{
    public ClaimProfile()
    {
        CreateMap<Claim, ClaimDto>()
            .ForMember(dest => dest.ClaimantName, opt => opt.MapFrom(src => $"{src.Claimant.FirstName} {src.Claimant.LastName}"))
            .ForMember(dest => dest.AssignedReviewerName, opt => opt.MapFrom(src => src.AssignedReviewer != null ? $"{src.AssignedReviewer.FirstName} {src.AssignedReviewer.LastName}" : null))
            .ForMember(dest => dest.ClaimTypeName, opt => opt.MapFrom(src => src.ClaimType.TypeName))
            .ForMember(dest => dest.PolicyNumber, opt => opt.MapFrom(src => src.Policy != null ? src.Policy.PolicyNumber : string.Empty));

        CreateMap<Claim, ClaimDetailDto>()
            .ForMember(dest => dest.PolicyNumber, opt => opt.MapFrom(src => src.Policy != null ? src.Policy.PolicyNumber : string.Empty))
            .ForMember(dest => dest.ClaimantName, opt => opt.MapFrom(src => $"{src.Claimant.FirstName} {src.Claimant.LastName}"))
            .ForMember(dest => dest.AssignedReviewerName, opt => opt.MapFrom(src => src.AssignedReviewer != null ? $"{src.AssignedReviewer.FirstName} {src.AssignedReviewer.LastName}" : null))
            .ForMember(dest => dest.ClaimTypeName, opt => opt.MapFrom(src => src.ClaimType.TypeName))
            .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents))
            .ForMember(dest => dest.WorkflowHistory, opt => opt.MapFrom(src => src.WorkflowHistories))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.ClaimNotes))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.ClaimPayments))
            .ForMember(dest => dest.ThirdParties, opt => opt.MapFrom(src => src.ThirdPartyClaimants));

        CreateMap<SubmitClaimRequest, Claim>()
            .ForMember(dest => dest.PolicyId, opt => opt.MapFrom(src => src.PolicyId))
            .ForMember(dest => dest.ClaimTypeId, opt => opt.MapFrom(src => src.ClaimTypeId))
            .ForMember(dest => dest.IncidentDate, opt => opt.MapFrom(src => src.IncidentDate))
            .ForMember(dest => dest.IncidentDescription, opt => opt.MapFrom(src => src.IncidentDescription))
            .ForMember(dest => dest.ClaimedAmount, opt => opt.MapFrom(src => src.ClaimedAmount))
            .ForMember(dest => dest.NomineeId, opt => opt.MapFrom(src => src.NomineeId))
            .ForMember(dest => dest.ClaimantType, opt => opt.MapFrom(src => src.ClaimantType))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClaimNumber, opt => opt.Ignore())
            .ForMember(dest => dest.ClaimantId, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedReviewerId, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedManagerId, opt => opt.Ignore())
            .ForMember(dest => dest.IntimationDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsLateIntimation, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedAmount, opt => opt.Ignore())
            .ForMember(dest => dest.DeductibleAmount, opt => opt.Ignore())
            .ForMember(dest => dest.CoPayPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.FinalPayableAmount, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
            .ForMember(dest => dest.ResolvedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentRecipientType, opt => opt.Ignore())
            .ForMember(dest => dest.RecipientName, opt => opt.Ignore())
            .ForMember(dest => dest.RecipientAccountNumber, opt => opt.Ignore())
            .ForMember(dest => dest.RecipientBankName, opt => opt.Ignore())
            .ForMember(dest => dest.RecipientIFSC, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IncidentLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Policy, opt => opt.Ignore())
            .ForMember(dest => dest.ClaimType, opt => opt.Ignore())
            .ForMember(dest => dest.Claimant, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedReviewer, opt => opt.Ignore())
            .ForMember(dest => dest.Nominee, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore())
            .ForMember(dest => dest.ClaimNotes, opt => opt.Ignore())
            .ForMember(dest => dest.ClaimPayments, opt => opt.Ignore())
            .ForMember(dest => dest.WorkflowHistories, opt => opt.Ignore())
            .ForMember(dest => dest.ThirdPartyClaimants, opt => opt.Ignore());
    }
}