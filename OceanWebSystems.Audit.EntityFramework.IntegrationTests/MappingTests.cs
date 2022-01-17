using AutoMapper;
using Xunit;

namespace OceanWebSystems.Audit.EntityFramework.IntegrationTests
{
    public class MappingTests
    {
        [Fact]
        public void CanMapModelToAuditModel()
        {
            var configuration = new MapperConfiguration(cfg =>
                cfg.CreateMap<TestModel, TestModelAuditRecord>()
                    .ForMember(d => d.AuditId, o => o.Ignore())
                    .ForMember(d => d.AuditDate, o => o.Ignore())
                    .ForMember(d => d.AuditAction, o => o.Ignore())
                    .ForMember(d => d.AuditUserId, o => o.Ignore())
                    .ForMember(d => d.AuditUserName, o => o.Ignore())
                    .ForMember(d => d.AuditUserDisplayName, o => o.Ignore()));

            configuration.AssertConfigurationIsValid();
        }
    }
}