using VerifyXunit;
using Xunit;

namespace OceanWebSystems.Audit.EntityFramework.Tests
{
    [UsesVerify]
    public class AuditEntityGeneratorSnapshotTests
    {
        [Fact]
        public Task NoAuditIncludeAttribute_NoAuditEntityGenerated()
        {
            // The source code to test.
            var source = @"using Audit.EntityFramework;

namespace OceanWebSystems.Data.Models
{
    public partial class NoAuditIncludeAttribute
    {
        public int Id { get; set; }
    }
}";

            // Pass the source code to our helper and snapshot test the output.
            return TestHelper.Verify(source);
        }

        [Fact]
        public Task NoProperties_AuditEntityGeneratedWithOnlyAuditId()
        {
            // The source code to test.
            var source = @"using Audit.EntityFramework;

namespace OceanWebSystems.Data.Models
{
    [AuditInclude]
    public partial class NoProperties
    {
    }
}";

            // Pass the source code to our helper and snapshot test the output.
            return TestHelper.Verify(source);
        }

        [Fact]
        public Task WithProperties_AuditEntityGeneratedCorrectly()
        {
            // The source code to test.
            var source = @"using System;
using System.ComponentModel.DataAnnotations;
using Audit.EntityFramework;

namespace OceanWebSystems.Data.Models
{
    [AuditInclude]
    public class WithProperties
    {
        [Key]
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime DateProperty { get; set; }

        public DateTime? NullableDateProperty { get; set; }

        public int? NullableIntegerProperty { get; set; }

        public byte ByteProperty { get; set; }

        public byte? NullableByteProperty { get; set; }

        public long LongProperty { get; set; }

        public long? NullableLongProperty { get; set; }

        public bool BooleanProperty { get; set; }

        public bool? NullableBooleanProperty { get; set; }

        public char CharProperty { get; set; }

        public char? NullableCharProperty { get; set; }

        public decimal DecimalProperty { get; set; }

        public decimal? NullableDecimalProperty { get; set; }

        public double DoubleProperty { get; set; }

        public double? NullableDoubleProperty { get; set; }

        public float FloatProperty { get; set; }

        public float? NullableFloatProperty { get; set; }

        public Guid GuidProperty { get; set; }

        public Guid? NullableGuidProperty { get; set; }

        public short ShortProperty { get; set; }

        public short? NullableShortProperty { get; set; }

        public DayOfWeek EnumProperty { get; set; }

        public DayOfWeek? NullableEnumProperty { get; set; }
    }
}
";

            // Pass the source code to our helper and snapshot test the output.
            return TestHelper.Verify(source);
        }
    }
}