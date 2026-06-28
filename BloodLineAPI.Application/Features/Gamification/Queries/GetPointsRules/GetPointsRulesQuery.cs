using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetPointsRules;

public record PointRuleDto(
    string ActionType,
    int Points,
    string TitleEn,
    string TitleAr,
    string DescriptionEn,
    string DescriptionAr,
    string IconName
);

public record GetPointsRulesQuery : IRequest<IReadOnlyList<PointRuleDto>>;

public sealed class GetPointsRulesQueryHandler(IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetPointsRulesQuery, IReadOnlyList<PointRuleDto>>
{
    public Task<IReadOnlyList<PointRuleDto>> Handle(GetPointsRulesQuery request, CancellationToken cancellationToken)
    {
        var httpReq = httpContextAccessor.HttpContext?.Request;
        var baseUrl = httpReq is not null 
            ? $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}/" 
            : string.Empty;

        var rules = new List<PointRuleDto>
        {
            new(
                PointActionType.DownloadApp.ToString(),
                100,
                "Download App",
                "تحميل وتفعيل التطبيق",
                "Download and activate your account on the mobile app.",
                "تحميل وتفعيل الحساب على تطبيق الهاتف.",
                baseUrl + "point_rules/download.png"
            ),
            new(
                PointActionType.WholeBloodDonation.ToString(),
                500,
                "Whole Blood Donation",
                "تبرع بالدم الكامل",
                "Donate whole blood at a center or campaign.",
                "التبرع بالدم الكامل في أحد المراكز أو الحملات.",
                baseUrl + "point_rules/bloodtype.png"
            ),
            new(
                PointActionType.PlateletPlasmaDonation.ToString(),
                700,
                "Platelet or Plasma Donation",
                "تبرع بالصفائح أو البلازما",
                "Donate platelets or plasma.",
                "التبرع بالصفائح الدموية أو البلازما.",
                baseUrl + "point_rules/opacity.png"
            ),
            new(
                PointActionType.EmergencyResponse.ToString(),
                800,
                "Emergency Appeal Response",
                "استجابة لطلب تبرع طارئ",
                "Donate blood in response to an urgent appeal.",
                "التبرع بالدم استجابة لنداء استغاثة طارئ.",
                baseUrl + "point_rules/emergency.png"
            ),
            new(
                PointActionType.ReadDailyInfo.ToString(),
                20,
                "Read Daily Health Tip",
                "قراءة النصيحة اليومية",
                "Read the daily health fact or calendar tip.",
                "قراءة النصيحة أو المعلومة الصحية اليومية.",
                baseUrl + "point_rules/menu_book.png"
            ),
            new(
                PointActionType.ShareDailyInfo.ToString(),
                50,
                "Share Daily Tip",
                "مشاركة النصيحة اليومية",
                "Share the daily tip and get a friend to visit.",
                "مشاركة النصيحة اليومية وزيارة صديق للرابط المشترك.",
                baseUrl + "point_rules/share.png"
            )
        };

        return Task.FromResult<IReadOnlyList<PointRuleDto>>(rules);
    }
}
