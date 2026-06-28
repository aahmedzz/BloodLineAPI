using System;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IEmergencyAppealScheduler
{
    string ScheduleDeactivation(Guid appealId, TimeSpan delay);
}
