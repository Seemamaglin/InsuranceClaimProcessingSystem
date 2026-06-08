using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class ClaimPaymentProfile : Profile
{
    public ClaimPaymentProfile()
    {
        CreateMap<ClaimPayment, ClaimPaymentDto>();
    }
}