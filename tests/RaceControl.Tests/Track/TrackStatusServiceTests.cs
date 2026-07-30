using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RaceControl.Hubs;
using RaceControl.Services;

namespace RaceControl.Tests.Track;

[TestClass]
public class TrackStatusServiceTests
{
    private static ILogger<TrackStatusService>? _logger;
    private static IHubContext<TrackStatusHub, ITrackStatusHubClient>? _trackStatusHubContext;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _logger = new Mock<ILogger<TrackStatusService>>().Object;
        _trackStatusHubContext = new Mock<IHubContext<TrackStatusHub, ITrackStatusHubClient>>().Object;
    }

    [TestMethod]
    public void TryParseFlag_WithValidFlag_ReturnsTrue()
    {
        Assert.IsTrue(TrackStatusService.TryParseFlag("BLACK AND WHITE",  out _));
    }

    [TestMethod]
    public void TryParseFlag_WithInvalidFlag_ReturnsFalse()
    {
        Assert.IsFalse(TrackStatusService.TryParseFlag("RAIN",  out _));
    }
}