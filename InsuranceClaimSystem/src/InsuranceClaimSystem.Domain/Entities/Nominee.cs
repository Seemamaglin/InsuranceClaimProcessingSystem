using System;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities;

public class Nominee : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid PolicyHolderId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public RelationshipType Relationship { get; set; }
    public DateTime DateOfBirth { get; set; }

    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public byte[] EncryptedAadhaar { get; set; } = Array.Empty<byte>();
    public string AadhaarKeyReference { get; set; } = string.Empty;
    public string AadhaarMasked { get; set; } = string.Empty;

    public decimal SharePercentage { get; set; }
    public bool IsActive { get; set; } = true;

    public Policy Policy {get; set;}
    public User PolicyHolder { get; set; }
    
}