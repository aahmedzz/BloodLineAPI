using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.BloodEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;

public sealed class GetMobileLabResultsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMobileLabResultsQuery, Result<MobileLabResultResponse>>
{
    public async Task<Result<MobileLabResultResponse>> Handle(
        GetMobileLabResultsQuery request,
        CancellationToken cancellationToken)
    {
        var donation = await dbContext.DonationAppointments
            .Include(da => da.DonationCenter)
            .Include(da => da.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
            .FirstOrDefaultAsync(da => da.Id == request.DonationId, cancellationToken);

        if (donation == null)
        {
            return Result<MobileLabResultResponse>.Failure("Donation appointment not found.");
        }

        if (donation.DonorId != request.DonorId)
        {
            return Result<MobileLabResultResponse>.Failure("Unauthorized access to lab results.");
        }

        if (donation.BloodBag == null)
        {
            return Result<MobileLabResultResponse>.Failure("Lab results are not available for this donation.");
        }

        var latestTestResult = donation.BloodBag.BloodTestResults
            .OrderByDescending(r => r.TestDate)
            .FirstOrDefault();

        if (latestTestResult == null)
        {
            return Result<MobileLabResultResponse>.Failure("Lab results are not available for this donation.");
        }

        var donationTypeDisplay = donation.DonationType switch
        {
            Domain.Enums.DonationType.WholeBlood => "Whole Blood",
            Domain.Enums.DonationType.Platelets => "Platelets",
            Domain.Enums.DonationType.Plasma => "Plasma",
            _ => donation.DonationType.ToString()
        };

        var hivRes = FormatResultBilingual(latestTestResult.HivResult);
        var hbvRes = FormatResultBilingual(latestTestResult.HepatitisBResult);
        var hcvRes = FormatResultBilingual(latestTestResult.HepatitisCResult);
        var syphilisRes = FormatResultBilingual(latestTestResult.SyphilisResult);

        var parameters = new List<LabTestParameterDto>
        {
            new("فيروس نقص المناعة البشرية (HIV)", "HIV", hivRes.Ar, hivRes.En),
            new("التهاب الكبد الوبائي ب (HBV)", "Hepatitis B", hbvRes.Ar, hbvRes.En),
            new("التهاب الكبد الوبائي ج (HCV)", "Hepatitis C", hcvRes.Ar, hcvRes.En),
            new("مرض الزهري (Syphilis)", "Syphilis", syphilisRes.Ar, syphilisRes.En)
        };

        FollowUpGuidanceDto? followUpGuidance = null;

        if (!latestTestResult.IsSafe)
        {
            var isHivPositive = string.Equals(latestTestResult.HivResult, "positive", StringComparison.OrdinalIgnoreCase);
            var isHepPositive = string.Equals(latestTestResult.HepatitisBResult, "positive", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(latestTestResult.HepatitisCResult, "positive", StringComparison.OrdinalIgnoreCase);
            var isSyphilisPositive = string.Equals(latestTestResult.SyphilisResult, "positive", StringComparison.OrdinalIgnoreCase);

            var titleAr = "إرشادات طبية هامة ومتابعة سرية";
            var titleEn = "Important Medical Guidance & Confidential Follow-up";

            var msgAr = "عزيزي المتبرع، نود إحاطتك بأن نتائج الفحص المبدئي أظهرت تفاعلاً إيجابياً لأحد الفحوصات الطبية. يرجى العلم أن فحوصات بنك الدم هي فحوصات مسحية عالية الحساسية للمحافظة على سلامة المرضى، وقد تعطي أحياناً نتائج إيجابية كاذبة. لا داعي للقلق، ولكن من الضروري إجراء فحص تأكيدي.";
            var msgEn = "Dear donor, please note that the initial screening tests showed a positive reaction for one of the medical parameters. Please be reassured that blood bank tests are highly sensitive screening tests to ensure patient safety, and can sometimes yield false positive results. There is no need to panic, but a confirmatory test is required.";

            var stepsAr = new List<string>();
            var stepsEn = new List<string>();
            var contacts = new List<ContactOrganizationDto>();

            if (isHivPositive)
            {
                stepsAr.Add("1. تجنب التبرع بالدم أو الأعضاء.");
                stepsAr.Add("2. تجنب مشاركة أدوات النظافة الشخصية (مثل فرشاة الأسنان أو شفرات الحلاقة).");
                stepsAr.Add("3. يرجى التواصل مع البرنامج الوطني لمكافحة الإيدز للحصول على استشارة وفحص تأكيدي مجاناً وبسرية مطلقة.");

                stepsEn.Add("1. Refrain from donating blood, plasma, or organs.");
                stepsEn.Add("2. Avoid sharing personal care items (such as toothbrushes or razors).");
                stepsEn.Add("3. Please contact the National AIDS Control Program for free and completely confidential counseling and confirmatory testing.");

                contacts.Add(new ContactOrganizationDto(
                    "البرنامج الوطني لمكافحة الإيدز (وزارة الصحة والسكان)",
                    "National AIDS Control Program (Ministry of Health)",
                    "08007008000",
                    "وزارة الصحة والسكان، القاهرة، مصر",
                    "Ministry of Health and Population, Cairo, Egypt"
                ));
                contacts.Add(new ContactOrganizationDto(
                    "الخط الساخن الوطني لمكافحة الإيدز",
                    "National AIDS Helpline",
                    "0233152801",
                    "خدمة هاتفية سرية، وزارة الصحة",
                    "Confidential Telephone Helpline, Ministry of Health"
                ));
            }

            if (isHepPositive)
            {
                stepsAr.Add("1. استشر طبيب أمراض كبدية مختص للتقييم والمتابعة.");
                stepsAr.Add("2. تجنب مشاركة الأدوات الحادة أو أدوات العناية الشخصية كشفرات الحلاقة ومقلمات الأظافر.");
                stepsAr.Add("3. توجه لأقرب وحدة تابعة للجنة القومية لمكافحة الفيروسات الكبدية لإجراء فحص تأكيدي (PCR) والحصول على العلاج مجاناً.");

                stepsEn.Add("1. Consult a specialized liver physician for medical evaluation and follow-up.");
                stepsEn.Add("2. Avoid sharing sharp items or personal hygiene items (like razors and nail clippers).");
                stepsEn.Add("3. Go to the nearest NCCVH treatment center to perform a confirmatory PCR test and receive free treatment.");

                contacts.Add(new ContactOrganizationDto(
                    "اللجنة القومية لمكافحة الفيروسات الكبدية (مبادرة 100 مليون صحة)",
                    "National Committee for Control of Viral Hepatitis (NCCVH)",
                    "15335",
                    "وحدات ومراكز علاج الفيروسات الكبدية الموزعة بالجمهورية",
                    "National network of specialized viral hepatitis centers across Egypt"
                ));
                contacts.Add(new ContactOrganizationDto(
                    "الخط الساخن لوزارة الصحة والسكان",
                    "Ministry of Health Inquiry Line",
                    "15311",
                    "استفسارات عامة وتوجيه لأقرب مركز علاج",
                    "General inquiries and guidance to the nearest treatment clinic"
                ));
            }

            if (isSyphilisPositive)
            {
                stepsAr.Add("1. استشر طبيب أمراض جلدية وتناسلية مختص للحصول على الاستشارة الطبية والعلاج المناسب (غالباً بنسلين).");
                stepsAr.Add("2. تجنب الاتصال الحميم حتى إتمام العلاج لتجنب نقل العدوى.");
                stepsAr.Add("3. ينصح بفحص شريكك الطبي للتأكد من سلامته.");

                stepsEn.Add("1. Consult a dermatologist or venereologist for medical advice and proper treatment (usually simple penicillin).");
                stepsEn.Add("2. Avoid intimate contact until treatment is fully completed to prevent transmission.");
                stepsEn.Add("3. It is highly recommended to screen your partner as well.");

                contacts.Add(new ContactOrganizationDto(
                    "مستشفيات الحميات والعيادات الجلدية والتناسلية (وزارة الصحة)",
                    "Fever Hospitals & Dermatology Clinics (Ministry of Health)",
                    "105",
                    "العيادات الخارجية بمستشفيات الحميات على مستوى الجمهورية",
                    "Outpatient clinics in MOH Fever Hospitals nationwide"
                ));
            }

            if (stepsAr.Count == 0)
            {
                stepsAr.Add("1. يرجى مراجعة طبيب الرعاية الأولية أو زيارة مركز التبرع بالدم للاستفسار عن تفاصيل النتائج.");
                stepsAr.Add("2. تجنب التبرع بالدم مرة أخرى لحين مراجعة الطبيب وتأكيد سلامتك.");

                stepsEn.Add("1. Please consult your primary care physician or visit the blood donation center to inquire about the details of your results.");
                stepsEn.Add("2. Refrain from donating blood again until you confirm your eligibility with a doctor.");

                contacts.Add(new ContactOrganizationDto(
                    "الخط الساخن لوزارة الصحة",
                    "Ministry of Health General Line",
                    "105",
                    "استفسارات عامة وتوجيه طبي",
                    "General medical inquiries and referral guidance"
                ));
            }

            var fullMessageAr = msgAr + "\n\nالخطوات المطلوبة:\n" + string.Join("\n", stepsAr);
            var fullMessageEn = msgEn + "\n\nRequired Steps:\n" + string.Join("\n", stepsEn);

            followUpGuidance = new FollowUpGuidanceDto(
                WarningTitleAr: titleAr,
                WarningTitleEn: titleEn,
                GuidanceMessageAr: fullMessageAr,
                GuidanceMessageEn: fullMessageEn,
                Contacts: contacts
            );
        }

        var response = new MobileLabResultResponse(
            DonationId: donation.Id,
            DonationDate: donation.ScheduledDate.ToString("yyyy-MM-dd"),
            DonationType: donationTypeDisplay,
            DonationCenterName: donation.DonationCenter?.Name ?? string.Empty,
            IsSafe: latestTestResult.IsSafe,
            Notes: latestTestResult.Notes,
            TestResults: parameters,
            FollowUpGuidance: followUpGuidance
        );

        return Result<MobileLabResultResponse>.Success(response);
    }

    private static (string Ar, string En) FormatResultBilingual(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return ("سالب", "Negative");
        }

        return result.Trim().ToLowerInvariant() switch
        {
            "negative" => ("سالب", "Negative"),
            "positive" => ("موجب", "Positive"),
            _ => (result, result)
        };
    }
}
