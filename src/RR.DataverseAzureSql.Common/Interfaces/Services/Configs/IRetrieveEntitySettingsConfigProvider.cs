using RR.DataverseAzureSql.Common.Dtos.Configs;

namespace RR.DataverseAzureSql.Common.Interfaces.Services.Configs
{
    public interface IRetrieveEntitySettingsConfigProvider
    {
        public RetrieveEntitySettings[] Get();
    }
}
