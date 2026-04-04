using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointmentById
{
    public sealed class GetDonorAppointmentByIdQueryValidator : AbstractValidator<GetDonorAppointmentByIdQuery>
    {
        public GetDonorAppointmentByIdQueryValidator()
        {
            RuleFor(x => x.DonorId).NotEmpty();
            RuleFor(x => x.AppointmentId).NotEmpty();
        }
    }
}
