namespace Audit.EntityFramework
{
    public class AuditConfigurationAttribute : Attribute
    {
        public AuditConfigurationAttribute(
            string? tableNamePrefix = "",
            string? tableNameSuffix = "AuditRecord",
            string? softDeleteColumnName = "IsDeleted",
            bool? softDeleteDeletedValue = true)
        {
            TableNamePrefix = tableNamePrefix;
            TableNameSuffix = tableNameSuffix;
            SoftDeleteColumnName = softDeleteColumnName;
            SoftDeleteDeletedValue = softDeleteDeletedValue;
        }

        public AuditConfigurationAttribute(
            AuditConfigurationOptions configOptions)
            : this(configOptions.TableNamePrefix,
                  configOptions.TableNameSuffix,
                  configOptions.SoftDeleteColumnName,
                  configOptions.SoftDeleteDeletedValue)
        {
        }

        public string? TableNamePrefix { get; }

        public string? TableNameSuffix { get; }

        public string? SoftDeleteColumnName { get; }

        public bool? SoftDeleteDeletedValue { get; }
    }
}
