using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Controllers;

namespace VitalNexus.UnitTests;

public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOkObjectResult_WithExpectedHealthPayload()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var response = okResult.Value!;
        var responseType = response.GetType();

        Assert.Equal("ok", responseType.GetProperty("status")?.GetValue(response));
        Assert.Equal("VitalNexus.Api", responseType.GetProperty("service")?.GetValue(response));

        var environmentValue = responseType.GetProperty("environment")?.GetValue(response) as string;
        Assert.False(string.IsNullOrWhiteSpace(environmentValue));

        var utcValue = responseType.GetProperty("utc")?.GetValue(response) as string;
        Assert.False(string.IsNullOrWhiteSpace(utcValue));
        Assert.True(DateTime.TryParse(utcValue, out _));
    }
}
