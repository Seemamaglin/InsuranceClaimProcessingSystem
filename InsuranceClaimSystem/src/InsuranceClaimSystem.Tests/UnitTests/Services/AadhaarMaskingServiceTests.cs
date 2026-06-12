using FluentAssertions;
using InsuranceClaimSystem.Infrastructure.Services;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class AadhaarMaskingServiceTests
{
    private readonly AadhaarMaskingService _service;

    public AadhaarMaskingServiceTests()
    {
        _service = new AadhaarMaskingService();
    }

    [Fact]
    public void MaskAadhaar_With12Digits_ShouldReturnMaskedFormat()
    {
        // Arrange
        var aadhaar = "123456789012";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithFormattedInput_ShouldReturnMaskedFormat()
    {
        // Arrange
        var aadhaar = "1234-5678-9012";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithEmptyString_ShouldReturnDefaultMask()
    {
        // Arrange
        var aadhaar = "";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-0000");
    }

    [Fact]
    public void MaskAadhaar_WithNull_ShouldReturnDefaultMask()
    {
        // Arrange
        string? aadhaar = null;

        // Act
        var result = _service.MaskAadhaar(aadhaar!);

        // Assert
        result.Should().Be("XXXX-XXXX-0000");
    }

    [Fact]
    public void MaskAadhaar_WithLessThan4Digits_ShouldReturnDefaultMask()
    {
        // Arrange
        var aadhaar = "123";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-0000");
    }

    [Fact]
    public void MaskAadhaar_WithNonDigitCharacters_ShouldReturnMaskedFormat()
    {
        // Arrange
        var aadhaar = "12-34-56-78-9012";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithSpacesAndDashes_ShouldReturnMaskedFormat()
    {
        // Arrange
        var aadhaar = "  1234 5678 9012  ";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithExactly4Digits_ShouldMaskCorrectly()
    {
        // Arrange
        var aadhaar = "0001";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-0001");
    }

    [Fact]
    public void MaskAadhaar_WithLeadingZerosOnly_ShouldReturnDefaultMask()
    {
        // Arrange
        var aadhaar = "000";

        // Act
        var result = _service.MaskAadhaar(aadhaar);

        // Assert
        result.Should().Be("XXXX-XXXX-0000");
    }
}