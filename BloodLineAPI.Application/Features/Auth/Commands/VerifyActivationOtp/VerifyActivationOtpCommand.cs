using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyActivationOtp;

public sealed record VerifyActivationOtpCommand(
    string NationalId,
    string OtpCode) : IRequest<Result<DonorAuthResponse>>;
