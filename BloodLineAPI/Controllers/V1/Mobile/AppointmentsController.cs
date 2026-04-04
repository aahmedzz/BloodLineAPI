using Asp.Versioning;
using BloodLineAPI.Application.Features.Appointments.Commands.BookDonationAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.CancelDonationAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.UpdateDonationAppointment;
using BloodLineAPI.Application.Features.Appointments.Queries.DonorAppointments;
using BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointmentById;
using BloodLineAPI.Attributes;
using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodLineAPI.Application.Common.Models.Appointment;

namespace BloodLineAPI.Controllers.V1.Mobile
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/mobile/[controller]")]
    [ApiAudience(Audience.Mobile)]
    [Authorize]
    public sealed class AppointmentsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        /// <summary>
        /// Get all appointments for the current donor, optionally filtered by status ("upcoming" or "past")
        /// </summary>
        /// <param name="status">"upcoming" for future appointments, "past" for history</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of appointments</returns>
        [HttpGet("list", Name = "GetDonorAppointments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> List([FromQuery] string status, CancellationToken cancellationToken)
        {
            var donorId = MobileDonorIdHelper.TryGetDonorId(User);
            if (donorId is null)
                return Unauthorized();

            var result = await _sender.Send(new GetDonorAppointmentsQuery(donorId.Value, status), cancellationToken);
            if (!result.IsSuccess)
                return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

            return Ok(result.Data);
        }

        /// <summary>
        /// Get a specific appointment by its ID for the current donor
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Appointment details</returns>
        [HttpGet("{id:guid}", Name = "GetDonorAppointmentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var donorId = MobileDonorIdHelper.TryGetDonorId(User);
            if (donorId is null)
                return Unauthorized();

            var result = await _sender.Send(new GetDonorAppointmentByIdQuery(donorId.Value, id), cancellationToken);
            if (!result.IsSuccess)
                return Problem(title: "Not Found", detail: result.Error, statusCode: StatusCodes.Status404NotFound);

            return Ok(result.Data);
        }

        /// <summary>
        /// Book a new donation appointment for the current donor
        /// </summary>
        /// <param name="body">Appointment booking details including date, time, donation type, and prescreening answers</param>
        /// <param name="cancellationToken"></param>
        /// <returns>New appointment ID</returns>
        [HttpPost(Name = "BookDonationAppointment")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Book([FromBody] BookDonationAppointmentRequest body, CancellationToken cancellationToken)
        {
            var donorId = MobileDonorIdHelper.TryGetDonorId(User);
            if (donorId is null)
                return Unauthorized();

            var cmd = new BookDonationAppointmentCommand(
                donorId.Value,
                body.DonationCenterId,
                body.ScheduledDate,
                body.BookTime,
                body.PrescreeningAnswers,
                body.DonationType
            );

            var result = await _sender.Send(cmd, cancellationToken);
            if (!result.IsSuccess)
                return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

            return Ok(new { id = result.Data });
        }

        /// <summary>
        /// Update an existing donation appointment for the current donor
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="body">Updated appointment details</param>
        /// <param name="cancellationToken"></param>
        [HttpPut("{id:guid}", Name = "UpdateDonationAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDonationAppointmentRequest body, CancellationToken cancellationToken)
        {
            var donorId = MobileDonorIdHelper.TryGetDonorId(User);
            if (donorId is null)
                return Unauthorized();

            var cmd = new UpdateDonationAppointmentCommand(
                donorId.Value,
                id,
                body.ScheduledDate,
                body.BookTime,
                body.DonationType
            );

            var result = await _sender.Send(cmd, cancellationToken);
            if (!result.IsSuccess)
            {
                var status = result.Error == "Appointment not found."? StatusCodes.Status404NotFound: StatusCodes.Status400BadRequest;

                return Problem(
                    title: status == StatusCodes.Status404NotFound ? "Not Found" : "Bad Request",
                    detail: result.Error,
                    statusCode: status
                );
            }

            return NoContent();
        }

        /// <summary>
        /// Cancel an existing donation appointment for the current donor
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="cancellationToken"></param>
        [HttpPost("{id:guid}/cancel", Name = "CancelDonationAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            var donorId = MobileDonorIdHelper.TryGetDonorId(User);
            if (donorId is null)
                return Unauthorized();

            var result = await _sender.Send(new CancelDonationAppointmentCommand(donorId.Value, id), cancellationToken);
            if (!result.IsSuccess)
            {
                var status = result.Error == "Appointment not found."? StatusCodes.Status404NotFound: StatusCodes.Status400BadRequest;

                return Problem(
                    title: status == StatusCodes.Status404NotFound ? "Not Found" : "Bad Request",
                    detail: result.Error,
                    statusCode: status
                );
            }

            return NoContent();
        }
    }
}