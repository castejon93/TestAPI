using Application.DTOs.Auth;

namespace Application.Interfaces
{
    /// <summary>
    /// Application service interface for authentication operations.
    /// Handles use cases: registration, login, token management.
    /// 
    /// NOTE: This interface is in Application layer (not Domain) because:
    /// 1. It orchestrates application use cases, not domain logic
    /// 2. It works with DTOs, which are Application layer concerns
    /// 3. Clean Architecture: Domain layer must have NO external dependencies
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Authenticates a user and generates access and refresh tokens.
        /// </summary>
        Task<AuthResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Generates a new access token using a valid refresh token.
        /// </summary>
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Invalidates the user's refresh token (logout).
        /// </summary>
        Task<bool> RevokeTokenAsync(int userId);

        /// <summary>
        /// DEBUG: Gets a user by username for testing purposes.
        /// </summary>
        Task<Domain.Entities.User?> GetUserByUsernameAsync(string username);
    }
}