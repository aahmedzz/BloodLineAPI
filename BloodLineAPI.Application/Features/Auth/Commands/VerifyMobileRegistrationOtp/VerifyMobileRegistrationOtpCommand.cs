using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyMobileRegistrationOtp;

public sealed record VerifyMobileRegistrationOtpCommand(
    string NationalId,
    string OtpCode) : IRequest<Result<string>>;
