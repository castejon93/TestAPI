// Quick utility to generate and verify BCrypt hashes
// Run with: dotnet run --project Infrastructure -- generate-hash
// Or use in your Swagger testing

using BCrypt.Net;

namespace Infrastructure.Utilities
{
    /// <summary>
    /// Utility class to generate BCrypt password hashes for seed data.
    /// Use this to create new hashes when you know the password.
    /// </summary>
    public static class PasswordHashGenerator
    {
        /// <summary>
        /// Generates a BCrypt hash for a given password.
        /// </summary>
        public static string GenerateHash(string password, int workFactor = 11)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        /// <summary>
        /// Verifies a password against a BCrypt hash.
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// Test method to validate hash/password combinations.
        /// Call this from Program.cs during development if needed.
        /// </summary>
        public static void TestHashGeneration()
        {
            const string testPassword = "Admin123!";
            
            // Generate a new hash
            var newHash = GenerateHash(testPassword);
            Console.WriteLine($"Password: {testPassword}");
            Console.WriteLine($"New Hash: {newHash}");
            
            // Verify it works
            var isValid = VerifyPassword(testPassword, newHash);
            Console.WriteLine($"Verification: {isValid}");
        }
    }
}
