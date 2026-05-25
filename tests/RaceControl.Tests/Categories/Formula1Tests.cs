using Microsoft.Extensions.Logging;
using Moq;
using RaceControl.Categories;
using RaceControl.Services;

namespace RaceControl.Tests.Categories;

[TestClass]
public class Formula1Tests
{
    private static ILogger? _logger;
    private static F1AuthService? _f1AuthService;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _logger = new Mock<ILogger<Formula1>>().Object;
        _f1AuthService = new Mock<F1AuthService>().Object;
    }

    [TestMethod]
    public async Task StartAsync_WithValidSessions_ReturnsConnected()
    {
        var formula1 = new Formula1(_logger!, _f1AuthService!);
        await formula1.StartAsync("gp");

        Assert.IsTrue(formula1.Connected);
    }

    [TestMethod]
    public async Task StartAsync_WithInvalidSessions_ReturnsNotConnected()
    {
        var formula1 = new Formula1(_logger!, _f1AuthService!);
        await formula1.StartAsync("fp4");

        Assert.IsFalse(formula1.Connected);
    }

    [TestMethod]
    public async Task StopAsync_WithValidSessions_ReturnsDisconnected()
    {
        var formula1 = new Formula1(_logger!, _f1AuthService!);
        await formula1.StartAsync("gp");
        await Task.Delay(5000);
        await formula1.StopAsync();

        Assert.IsFalse(formula1.Connected);
    }
}