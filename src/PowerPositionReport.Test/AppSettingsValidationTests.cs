using System.ComponentModel.DataAnnotations;
using PowerPositionReport.Configuration;

namespace PowerPositionReport.Test;

public class AppSettingsValidationTests
{
    [Fact]
    public void ValidSettings_PassValidation()
    {
        var settings = new AppSettings { OutputPath = @"C:\Reports", IntervalMinutes = 5 };

        var isValid = TryValidate(settings, out var results);

        Assert.True(isValid, string.Join("; ", results.Select(r => r.ErrorMessage)));
    }

    [Fact]
    public void EmptyOutputPath_FailsValidation()
    {
        var settings = new AppSettings { OutputPath = string.Empty, IntervalMinutes = 5 };

        var isValid = TryValidate(settings, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AppSettings.OutputPath)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveIntervalMinutes_FailsValidation(int interval)
    {
        var settings = new AppSettings { OutputPath = @"C:\Reports", IntervalMinutes = interval };

        var isValid = TryValidate(settings, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AppSettings.IntervalMinutes)));
    }

    private static bool TryValidate(AppSettings settings, out List<ValidationResult> results)
    {
        var context = new ValidationContext(settings);
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(settings, context, results, validateAllProperties: true);
    }
}
