using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyForgotPasswordOtp;

public sealed record VerifyForgotPasswordOtpCommand(
    string NationalId,
    string OtpCode) : IRequest<Result<string>>;
