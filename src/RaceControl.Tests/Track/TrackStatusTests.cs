using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RaceControl.Hubs;
using RaceControl.Track;

namespace RaceControl.Tests.Track;

[TestClass]
public class TrackStatusTests
{
    private static ILogger<TrackStatus>? _logger;
    private static IHubContext<TrackStatusHub, ITrackStatusHubClient>? _trackStatusHubContext;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _logger = new Mock<ILogger<TrackStatus>>().Object;
        _trackStatusHubContext = new Mock<IHubContext<TrackStatusHub, ITrackStatusHubClient>>().Object;
    }

    [TestMethod]
    public void Try_ParseValidFlag_ReturnsTrue()
    {
        Assert.IsTrue(TrackStatus.TryParseFlag("BLACK AND WHITE",  out _));
    }

    [TestMethod]
    public void Try_ParseInvalidFlag_ReturnsFalse()
    {
        Assert.IsFalse(TrackStatus.TryParseFlag("RAIN",  out _));
    }
}