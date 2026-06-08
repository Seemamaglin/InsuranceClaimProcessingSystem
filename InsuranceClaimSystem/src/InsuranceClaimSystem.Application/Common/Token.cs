namespace InsuranceClaimSystem.Application.Common;

public class Token
{
    public string AccessToken { get; set; }
    public string TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }

    public Token(string accessToken, string refreshToken, DateTime expiresAt, DateTime refreshTokenExpiresAt, string tokenType = "Bearer", int expiresIn = 900)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token is required.", nameof(accessToken));
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        RefreshTokenExpiresAt = refreshTokenExpiresAt;
        TokenType = tokenType;
        ExpiresIn = expiresIn;
    }
}