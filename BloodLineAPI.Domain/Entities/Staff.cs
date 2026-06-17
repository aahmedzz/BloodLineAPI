namespace BloodLineAPI.Domain.Entities
{
    public class Staff :AuditableEntity
    {
        public string EmployeeIdentifier { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string ThirdName { get; set; } = string.Empty;
        public string? FourthName { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActiveEmployee { get; set; } = true;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        public string FullName => string.Join(" ", new[] { FirstName, SecondName, ThirdName, FourthName }
            .Where(static n => !string.IsNullOrWhiteSpace(n)));

        public User User { get; set; } = null!;
        public ICollection<BloodBag> CollectedBloodBags { get; set; } = new List<BloodBag>();
        public ICollection<BloodTestResult> BloodTestResults { get; set; } = new List<BloodTestResult>();
        public ICollection<MedicalScreening> MedicalScreenings { get; set; } = new List<MedicalScreening>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<DiscardRecord> AuthorizedDiscardRecords { get; set; } = new List<DiscardRecord>();
        public ICollection<IssuanceRecord> IssuedBloodBags { get; set; } = new List<IssuanceRecord>();
        public ICollection<UrgentBloodAppeal> UrgentBloodAppeals { get; set; } = new List<UrgentBloodAppeal>();
    }
}
