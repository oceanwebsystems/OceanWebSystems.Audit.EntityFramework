using System.Runtime.CompilerServices;
using VerifyTests;

namespace OceanWebSystems.Audit.EntityFramework.Tests
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Enable();
        }
    }
}
