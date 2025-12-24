using System.Text;
using RR.DataverseAzureSql.Infra.Models;
using YamlDotNet.Serialization;

namespace RR.DataverseAzureSql.Infra.Helpers;

public static class FileHelpers
{
    public static List<EntityModel> RetrieveEntityListFromFile()
    {
        var path = GetFilePath("dynamics-service-settings.yml");

        using var r = new StreamReader(path);

        var json = r.ReadToEnd();

        var deserializer = new Deserializer();

        return deserializer.Deserialize<List<EntityModel>>(json);
    }

    public static string GetFilePath(string fileName)
    {
        var relativePath = new StringBuilder("..");
        do
        {
            var path = Path.GetFullPath(relativePath.ToString());

            relativePath.Append("/..");

            var file = Directory.EnumerateFiles(path, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(x => x.EndsWith(fileName));

            if (file != null)
            {
                return file;
            }
        } while (true);
    }
}

