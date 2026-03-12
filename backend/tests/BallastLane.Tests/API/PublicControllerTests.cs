using BallastLane.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.Tests.API;

public sealed class PublicControllerTests
{
    private const string ExpectedStatus = "ok";

    [Fact]
    public void Ping_ShouldReturn200WithOkStatus()
    {
        var sut = new PublicController();

        var result = sut.Ping();

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value;
        var statusProp = value?.GetType().GetProperty("status")?.GetValue(value);
        Assert.Equal(ExpectedStatus, statusProp?.ToString());
    }
}
