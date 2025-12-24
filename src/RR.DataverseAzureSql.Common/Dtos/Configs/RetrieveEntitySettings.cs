using Newtonsoft.Json;
using RR.DataverseAzureSql.Common.Constants;

namespace RR.DataverseAzureSql.Common.Dtos.Configs
{
    public class RetrieveEntitySettings
    {
        public string EntityLogicalName { get; set; }

        public string[] ExcludedFields { get; set; }
    }

    public static class RetrieveEntitySettingsFactory
    {
        public static string GetDefaultSettings()
        {
            var settings = new RetrieveEntitySettings[]
            {
                new RetrieveEntitySettings
                {
                    EntityLogicalName = EntityLogicalNames.Annotation,
                    ExcludedFields =  new string[]
                    {
                        "documentbody"
                    }
                }
            };
            return JsonConvert.SerializeObject(settings);
        }
    }
}
