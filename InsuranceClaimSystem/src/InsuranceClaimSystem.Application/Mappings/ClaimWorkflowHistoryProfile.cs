using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class ClaimWorkflowHistoryProfile : Profile
{
    public ClaimWorkflowHistoryProfile()
    {
        CreateMap<ClaimWorkflowHistory, ClaimWorkflowHistoryDto>()
            .ForMember(dest => dest.ChangedByUserName, opt => opt.MapFrom(src => $"{src.ChangedByUser.FirstName} {src.ChangedByUser.LastName}"))
            .ForMember(dest => dest.PreviousStatus, opt => opt.MapFrom(src => src.PreviousStatus.HasValue ? src.PreviousStatus.Value.ToString() : null))
            .ForMember(dest => dest.NewStatus, opt => opt.MapFrom(src => src.NewStatus.HasValue ? src.NewStatus.Value.ToString() : null));
    }
}