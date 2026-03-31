using System.Text.Json.Serialization;

namespace BloodLineAPI.Domain.Enums
{
    public enum RhFactor
    {
        [JsonStringEnumMemberName("+")]
        Positive,

        [JsonStringEnumMemberName("-")]
        Negative
    }
}
