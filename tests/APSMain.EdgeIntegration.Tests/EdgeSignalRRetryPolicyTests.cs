using System.Net;
using Microsoft.AspNetCore.SignalR;
using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class EdgeSignalRRetryPolicyTests
{
    [Fact]
    public void Hub_registration_error_is_permanent()
    {
        Assert.False(EdgeSignalRRetryPolicy.IsTransient(new HubException("DEVICE_NOT_FOUND")));
    }

    [Fact]
    public void Http_unauthorized_is_permanent()
    {
        Assert.False(EdgeSignalRRetryPolicy.IsTransient(
            new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized)));
    }

    [Fact]
    public void Network_and_server_errors_are_transient()
    {
        Assert.True(EdgeSignalRRetryPolicy.IsTransient(new HttpRequestException("network")));
        Assert.True(EdgeSignalRRetryPolicy.IsTransient(
            new HttpRequestException("server", null, HttpStatusCode.ServiceUnavailable)));
        Assert.True(EdgeSignalRRetryPolicy.IsTransient(new TimeoutException()));
        Assert.True(EdgeSignalRRetryPolicy.IsTransient(new TaskCanceledException("timeout")));
    }

    [Fact]
    public void Non_gateway_server_error_and_rate_limit_are_permanent()
    {
        Assert.False(EdgeSignalRRetryPolicy.IsTransient(
            new HttpRequestException("server", null, HttpStatusCode.InternalServerError)));
        Assert.False(EdgeSignalRRetryPolicy.IsTransient(
            new HttpRequestException("rate limit", null, HttpStatusCode.TooManyRequests)));
    }
}
