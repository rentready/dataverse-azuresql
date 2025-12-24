using RR.DataverseAzureSql.Common.Interfaces.Services.Converters;
using RR.DataverseAzureSql.Tests.Common.Factories;
using Xunit;

namespace RR.DataverseAzureSql.Services.UnitTests.Services.Converters;

public class DateTimeConverterTests
{
    private readonly IDateTimeConverter _dateTimeConverter;

    public DateTimeConverterTests()
    {
        _dateTimeConverter = EntityFactory.CreateDateTimeConverter();
    }

    [Theory]
    [InlineData(@"/Date(1677279600000)/", 2023, 2, 24)]
    [InlineData(@"/Date(1677279600000+0300)/", 2023, 2, 25)]
    public void DateTimeConverter_Should_Return_DateTime(string data, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Act
        var actual = _dateTimeConverter.UnixEpochDateTimeConverter(data);

        // Assert
        Assert.Equal(expectedYear, actual?.Year);
        Assert.Equal(expectedMonth, actual?.Month);
        Assert.Equal(expectedDay, actual?.Day);
    }

    [Theory]
    [InlineData(@"A10 1")]
    [InlineData(@"A101")]
    public void DateTimeConverter_Should_Not_Return_DateTime(string data)
    {
        // Act
        var actual = _dateTimeConverter.UnixEpochDateTimeConverter(data);

        // Assert
        Assert.Null(actual);
    }
}

