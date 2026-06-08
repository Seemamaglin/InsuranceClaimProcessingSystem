using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class ThirdPartyClaimantProfile : Profile
{
    public ThirdPartyClaimantProfile()
    {
        CreateMap<ThirdPartyClaimant, ThirdPartyClaimantDto>();
    }
}