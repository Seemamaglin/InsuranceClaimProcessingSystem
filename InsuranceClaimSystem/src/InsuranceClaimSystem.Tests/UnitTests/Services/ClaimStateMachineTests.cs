using FluentAssertions;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Services;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class ClaimStateMachineTests
{
    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Submitted, true)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.UnderReview, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.DocumentsPending, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.Approved, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.Rejected, true)]
    [InlineData(ClaimStatus.DocumentsPending, ClaimStatus.UnderReview, true)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Closed, true)]
    [InlineData(ClaimStatus.Rejected, ClaimStatus.Closed, true)]
    public void CanTransition_WithValidTransition_ShouldReturnTrue(ClaimStatus from, ClaimStatus to, bool expected)
    {
        // Act
        var result = ClaimStateMachine.CanTransition(from, to);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Draft, ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.DocumentsPending, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.DocumentsPending, ClaimStatus.Rejected)]
    public void CanTransition_WithInvalidTransition_ShouldReturnFalse(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var result = ClaimStateMachine.CanTransition(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Draft)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.DocumentsPending)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Rejected)]
    public void CanTransition_FromClosedToAny_ShouldReturnFalse(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var result = ClaimStateMachine.CanTransition(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_FromRejectedToApproved_ShouldReturnFalse()
    {
        // Act
        var result = ClaimStateMachine.CanTransition(ClaimStatus.Rejected, ClaimStatus.Approved);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Submitted)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Closed)]
    public void ValidateTransition_WithValidTransition_ShouldNotThrow(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var act = () => ClaimStateMachine.ValidateTransition(from, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Draft)]
    public void ValidateTransition_WithInvalidTransition_ShouldThrowBusinessRuleException(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var act = () => ClaimStateMachine.ValidateTransition(from, to);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage($"*Invalid claim status transition from {from} to {to}*");
    }
}