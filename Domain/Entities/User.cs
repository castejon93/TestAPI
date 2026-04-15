namespace Domain.Entities
{
    /// <summary>
    /// Represents a user in the system with authentication properties.
    /// Contains security-related fields for JWT authentication flow.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary key - unique identifier for the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique username for login purposes.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User's email address - must be unique.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password - NEVER store plain text passwords!
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's first name for display purposes.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name for display purposes.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's role for authorization (e.g., "Admin", "User").
        /// </summary>
        public string Role { get; set; } = "User";

        /// <summary>
        /// Current refresh token - used to obtain new access tokens.
        /// Null if user hasn't logged in or token was revoked.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// When the refresh token expires.
        /// After this time, user must log in again.
        /// </summary>
        public DateTime? RefreshTokenExpiryTime { get; set; }

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the user account is active.
        /// Inactive users cannot authenticate.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}