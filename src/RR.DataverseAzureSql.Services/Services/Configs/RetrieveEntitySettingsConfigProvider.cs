using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RR.Common.Configs.Extensions;
using RR.Common.General;
using RR.Common.Validations;
using RR.DataverseAzureSql.Common.Constants;
using RR.DataverseAzureSql.Common.Dtos.Configs;
using RR.DataverseAzureSql.Common.Interfaces.Services.Configs;

namespace RR.DataverseAzureSql.Services.Services.Configs
{
    public class RetrieveEntitySettingsConfigProvider : IRetrieveEntitySettingsConfigProvider
    {
        private readonly IConfiguration _configuration;
        private readonly LazyWithoutExceptionCaching<RetrieveEntitySettings[]> _lazyRetrieveEntitySettings;

        public RetrieveEntitySettingsConfigProvider(IConfiguration configuration)
        {
            _configuration = configuration.IsNotNull(nameof(configuration));
            _lazyRetrieveEntitySettings = new LazyWithoutExceptionCaching<RetrieveEntitySettings[]>(ReadRetrieveEntitySettings);
        }
        public RetrieveEntitySettings[] Get()
        {
            return _lazyRetrieveEntitySettings.Value;
        }

        private RetrieveEntitySettings[] ReadRetrieveEntitySettings()
        {
            var value = _configuration.GetStringValue(ConfigSectionNames.RetrieveEntitySettings, false);
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<RetrieveEntitySettings>();
            }
            return JsonConvert.DeserializeObject<RetrieveEntitySettings[]>(value);
        }
    }
}
