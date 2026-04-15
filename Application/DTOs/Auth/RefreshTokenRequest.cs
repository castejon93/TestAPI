using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// Request to refresh an expired access token.
    /// Requires both the expired access token and valid refresh token.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// The expired access token (JWT).
        /// </summary>
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// The refresh token issued during login.
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}