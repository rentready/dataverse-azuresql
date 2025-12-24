using System.Globalization;
using System.Text.RegularExpressions;
using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;

namespace RR.DataverseAzureSql.Services.Services.Converters;

public class DateTimeConverter : IDateTimeConverter
{
    private readonly Regex s_regex = new("^/Date\\(([+-]*\\d+)\\)/$", RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private readonly Regex s_regex_with_tz = new("^/Date\\(([+-]*\\d+)([+-])(\\d{2})(\\d{2})\\)/$", RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public DateTime? UnixEpochDateTimeConverter(string data)
    {
        var match = s_regex.Match(data);
        var match_with_tz = s_regex_with_tz.Match(data);

        if (match.Success)
        {
            long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime);

            var dateTime = DateTime.UnixEpoch
                .AddMilliseconds(unixTime)
                .ToUniversalTime();

            return dateTime;
        }
        else if (match_with_tz.Success)
        {
            long.TryParse(match_with_tz.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime);
            int.TryParse(match_with_tz.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours);
            int.TryParse(match_with_tz.Groups[4].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes);

            var sign = match_with_tz.Groups[2].Value[0] == '+' ? 1 : -1;
            var utcOffset = new TimeSpan(hours * sign, minutes * sign, 0);

            var dateTime = DateTimeOffset.UnixEpoch
                .AddMilliseconds(unixTime)
                .ToOffset(utcOffset)
                .DateTime;

            return dateTime;
        }
        else
        {
            return null;
        }
    }
}

