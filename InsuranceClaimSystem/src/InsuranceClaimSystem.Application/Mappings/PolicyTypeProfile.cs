using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class PolicyTypeProfile : Profile
{
    public PolicyTypeProfile()
    {
        CreateMap<PolicyType, PolicyTypeDto>();
    }
}