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
                cfg.CreateMap<TestModel, TestModelAudit>()
                    .ForMember(d => d.AuditId, o => o.Ignore())
                    .ForMember(d => d.AuditDate, o => o.Ignore())
                    .ForMember(d => d.AuditAction, o => o.Ignore())
                    .ForMember(d => d.UserId, o => o.Ignore())
                    .ForMember(d => d.UserName, o => o.Ignore())
                    .ForMember(d => d.UserDisplayName, o => o.Ignore()));

            configuration.AssertConfigurationIsValid();
        }
    }
}