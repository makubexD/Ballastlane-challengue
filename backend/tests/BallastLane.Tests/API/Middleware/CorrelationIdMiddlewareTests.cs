using BallastLane.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace BallastLane.Tests.API.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrelationIdHeader_WhenNotPresent()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey(CorrelationIdHeader));
        var responseValue = context.Response.Headers[CorrelationIdHeader].ToString();
        Assert.True(Guid.TryParse(responseValue, out _), "Header value should be a valid GUID.");
    }

    [Fact]
    public async Task InvokeAsync_ShouldEchoCorrelationIdHeader_WhenPresent()
    {
        const string existingId = "test-correlation-id-abc123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = existingId;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var responseValue = context.Response.Headers[CorrelationIdHeader].ToString();
        Assert.Equal(existingId, responseValue);
        Assert.Equal(existingId, context.Items[CorrelationIdHeader]);
    }
}
