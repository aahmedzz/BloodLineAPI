using System.IO;
using System.Reflection;
using System.Text.Json;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDailyInfo;

public record DailyInfoDto(
    int Id,
    string TitleEn,
    string TitleAr,
    string ContentEn,
    string ContentAr,
    bool AlreadyReadToday
);

public record GetDailyInfoQuery(Guid DonorId) : IRequest<DailyInfoDto>;

public static class DailyInfoProvider
{
    private static readonly List<TipItem> Tips;

    static DailyInfoProvider()
    {
        var assembly = typeof(DailyInfoProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream("BloodLineAPI.Application.Resources.daily_info.json") 
            ?? throw new FileNotFoundException("Embedded daily_info.json resource not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        Tips = JsonSerializer.Deserialize<List<TipItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
            ?? new List<TipItem>();
    }

    public static TipItem GetTipForDay(int dayOfYear)
    {
        if (Tips.Count == 0) return new TipItem(1, "No Tips", "لا توجد نصائح", "No Tips", "لا توجد نصائح", "");
        var index = (dayOfYear - 1) % Tips.Count;
        return Tips[index];
    }
}

public record TipItem(
    int Id,
    string TitleEn,
    string TitleAr,
    string ContentEn,
    string ContentAr,
    string IconName
);

public sealed class GetDailyInfoQueryHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<GetDailyInfoQuery, DailyInfoDto>
{
    public async Task<DailyInfoDto> Handle(GetDailyInfoQuery request, CancellationToken cancellationToken)
    {
        var localNow = dateTimeProvider.LocalNow;
        var localTodayDate = localNow.Date;

        var tip = DailyInfoProvider.GetTipForDay(localNow.DayOfYear);

        var alreadyReadToday = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == request.DonorId &&
                            pt.ActionType == PointActionType.ReadDailyInfo &&
                            pt.TransactionDate.Date == localTodayDate,
                      cancellationToken);

        return new DailyInfoDto(
            tip.Id,
            tip.TitleEn,
            tip.TitleAr,
            tip.ContentEn,
            tip.ContentAr,
            alreadyReadToday
        );
    }
}
