namespace BloodLineAPI.Attributes;

/// <summary>
/// Indicates the target audience for the API endpoint.
/// Used to categorize endpoints in the OpenAPI documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiAudienceAttribute(string audience) : Attribute
{
    public string Audience { get; } = audience;
}

public static class Audience
{
    public const string System = "System";
    public const string Mobile = "Mobile";
}
