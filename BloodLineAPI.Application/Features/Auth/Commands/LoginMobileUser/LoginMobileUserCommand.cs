using BloodLineAPI.Application.Common.Models;
using MediatR;
using MediatR.Pipeline;


namespace BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser
{
    public sealed record LoginMobileUserCommand
    (
        string Identifier, 
        string Password
    ) : IRequest<Result<DonorAuthResponse>>;
}
