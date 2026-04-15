using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// Data Transfer Object for user login requests.
    /// Supports login via email or username.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's email address or username for authentication.
        /// </summary>
        [Required(ErrorMessage = "Email or username is required")]
        public string EmailOrUsername { get; set; } = string.Empty;

        /// <summary>
        /// User's password - will be verified against stored hash.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}