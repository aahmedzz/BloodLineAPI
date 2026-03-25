using System.ComponentModel.DataAnnotations;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgetAndResetPasswordDto
{
    public class ResetPasswordRequest
    {
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string Token { get; set; } = string.Empty; 
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
