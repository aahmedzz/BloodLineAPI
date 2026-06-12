using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface ICampaignScheduler
{
    string? ScheduleActivation(Guid campaignId, DateTime localStartDateTime);
    string? ScheduleDeactivation(Guid campaignId, DateTime localEndDateTime);
    string? ScheduleCompletion(Guid campaignId, DateTime localEndDateTime);
    void UnscheduleJobs(IEnumerable<string> jobIds);
}
