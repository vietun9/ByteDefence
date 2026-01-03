namespace ByteDefence.Api;

/// <summary>
/// Shared constants for JWT authentication configuration.
/// Used by both AuthService and JwtAuthenticationMiddleware.
/// </summary>
public static class JwtDefaults
{
    /// <summary>
    /// Default JWT secret for development only. 
    /// In production, this must be overridden via configuration.
    /// </summary>
    public const string DevelopmentSecret = "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!";

    /// <summary>
    /// Default JWT issuer.
    /// </summary>
    public const string DefaultIssuer = "ByteDefence";

    /// <summary>
    /// Default JWT audience.
    /// </summary>
    public const string DefaultAudience = "ByteDefence-API";
}
