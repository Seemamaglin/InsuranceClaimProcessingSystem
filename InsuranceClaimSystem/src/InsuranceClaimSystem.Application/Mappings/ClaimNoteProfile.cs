using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Mappings;

public class ClaimNoteProfile : Profile
{
    public ClaimNoteProfile()
    {
        CreateMap<ClaimNote, ClaimNoteDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => $"{src.Author.FirstName} {src.Author.LastName}"));
    }
}