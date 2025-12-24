namespace RR.DataverseAzureSql.Common.Interfaces.Services.Converters;

public interface IDateTimeConverter
{
    DateTime? UnixEpochDateTimeConverter(string data);
}

