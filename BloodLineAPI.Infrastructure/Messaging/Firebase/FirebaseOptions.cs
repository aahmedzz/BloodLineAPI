namespace BloodLineAPI.Infrastructure.Messaging.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";
    public string ServiceAccountKeyPath { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
}