namespace Audit.EntityFramework
{
    public class AuditConfigurationOptions
    {
        public string? TableNamePrefix { get; set; }

        public string? TableNameSuffix { get; set; }

        public string? SoftDeleteColumnName { get; set; }

        public bool? SoftDeleteDeletedValue { get; set; }
    }
}
