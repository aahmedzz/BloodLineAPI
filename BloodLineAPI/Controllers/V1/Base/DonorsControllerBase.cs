using Asp.Versioning;
using BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;
using BloodLineAPI.Application.Features.Donors.Queries.GetAllDonors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.Base;

[ApiController]
[ApiVersion("1.0")]
public abstract class DonorsControllerBase(ISender sender) : ControllerBase
{
    protected readonly ISender Sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAllDonorsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDonorCommand command, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }
}
