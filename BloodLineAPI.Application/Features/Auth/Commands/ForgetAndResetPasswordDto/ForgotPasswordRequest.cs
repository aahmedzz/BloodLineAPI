using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgetAndResetPasswordDto
{
    public class ForgotPasswordRequest
    {
        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

    }
}
