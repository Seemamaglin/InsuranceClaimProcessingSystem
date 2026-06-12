using FluentAssertions;
using InsuranceClaimSystem.Infrastructure.Services.Encryption;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class AesEncryptionServiceTests
{
    private const string TestEncryptionKey = "test-encryption-key-32-chars-long!!";

    private static IConfiguration CreateConfiguration(string key = TestEncryptionKey)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "EncryptionSettings:Key", key }
            })
            .Build();
    }

    private AesEncryptionService CreateService(string key = TestEncryptionKey)
    {
        var config = CreateConfiguration(key);
        return new AesEncryptionService(config);
    }

    [Fact]
    public void Encrypt_WithValidPlainText_ShouldReturnDifferentString()
    {
        // Arrange
        var service = CreateService();
        var plainText = "Hello, World!";

        // Act
        var encrypted = service.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
        encrypted.Should().NotContain(plainText);
    }

    [Fact]
    public void Decrypt_WithEncryptedText_ShouldReturnOriginalPlainText()
    {
        // Arrange
        var service = CreateService();
        var plainText = "Hello, World!";

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_WithUnicode_ShouldRoundTripCorrectly()
    {
        // Arrange
        var service = CreateService();
        var plainText = "Héllo, Wörld! 你好世界 🌐";

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_WithSamePlainTextTwice_ShouldReturnDifferentResults()
    {
        // Arrange
        var service = CreateService();
        var plainText = "TestPlainText123";

        // Act
        var encrypted1 = service.Encrypt(plainText);
        var encrypted2 = service.Encrypt(plainText);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Decrypt_WithInvalidCipherText_ShouldThrowFormatException()
    {
        // Arrange
        var service = CreateService();
        var invalidCipherText = "not-valid-base64!!";

        // Act
        var action = () => service.Decrypt(invalidCipherText);

        // Assert
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void MaskAadhaar_WithValidNumber_ShouldMaskCorrectly()
    {
        // Arrange
        var service = CreateService();
        var aadhaar = "123456789012";

        // Act
        var masked = service.MaskAadhaar(aadhaar);

        // Assert
        masked.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithFormattedNumber_ShouldExtractLast4()
    {
        // Arrange
        var service = CreateService();
        var aadhaar = "1234-5678-9012";

        // Act
        var masked = service.MaskAadhaar(aadhaar);

        // Assert
        masked.Should().Be("XXXX-XXXX-9012");
    }

    [Fact]
    public void MaskAadhaar_WithEmptyString_ShouldReturnDefaultMask()
    {
        // Arrange
        var service = CreateService();
        var aadhaar = "";

        // Act
        var masked = service.MaskAadhaar(aadhaar);

        // Assert
        masked.Should().Be("XXXX-XXXX-0000");
    }

    [Fact]
    public void MaskAadhaar_WithLessThan4Digits_ShouldReturnDefaultMask()
    {
        // Arrange
        var service = CreateService();
        var aadhaar = "123";

        // Act
        var masked = service.MaskAadhaar(aadhaar);

        // Assert
        masked.Should().Be("XXXX-XXXX-0000");
    }

    [Fact]
    public void Constructor_WithMissingKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var action = () => new AesEncryptionService(config);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("EncryptionSettings:Key is not configured.");
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldRoundTripCorrectly()
    {
        // Arrange
        var service = CreateService();
        var plainText = new string('A', 10000); // Long text

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_ShouldReturnEquivalentToSync()
    {
        // Arrange
        var service = CreateService();
        var plainText = "Async Test Data";

        // Act
        var encryptedBytes = await service.EncryptAsync(System.Text.Encoding.UTF8.GetBytes(plainText));
        var encryptedString = service.Encrypt(plainText);

        // Assert
        var decryptedFromAsync = service.Decrypt(System.Text.Encoding.UTF8.GetString(encryptedBytes));
        decryptedFromAsync.Should().Be(plainText);
    }

    [Fact]
    public void EncryptAsync_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var service = CreateService();
        var plainText = "Async Test Data";

        // Act
        var encryptedBytes = service.EncryptAsync(System.Text.Encoding.UTF8.GetBytes(plainText)).Result;

        // Assert
        encryptedBytes.Should().NotBeEmpty();
        var encryptedString = System.Text.Encoding.UTF8.GetString(encryptedBytes);
        encryptedString.Should().NotBe(plainText);
    }
}