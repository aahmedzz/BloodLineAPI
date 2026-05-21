using System;
using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.DeleteStaff;

public record DeleteStaffCommand(Guid Id) : IRequest<Result<bool>>;
