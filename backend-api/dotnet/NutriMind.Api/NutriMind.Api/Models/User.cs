using System.ComponentModel.DataAnnotations;

namespace NutriMind.Api.Models
{
    public class User
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Refresh token for JWT
        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
