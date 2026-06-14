using System;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IDonorStatusScheduler
{
    void ScheduleStatusReset(Guid donorId, DateTime lockoutUntil);
}
