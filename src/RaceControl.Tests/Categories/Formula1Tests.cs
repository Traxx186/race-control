using Microsoft.Extensions.Logging;
using Moq;
using RaceControl.Categories;

namespace RaceControl.Tests.Categories;

[TestClass]
public class Formula1Tests
{
    private static ILogger? _logger;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _logger = new Mock<ILogger<Formula1>>().Object;
    }

    [TestMethod]
    public async Task Start_LiveTimingApi_ValidSession()
    {
        var formula1 = new Formula1(_logger!);
        await formula1.StartAsync("gp");

        Assert.IsTrue(formula1.Connected);
    }

    [TestMethod]
    public async Task Start_LiveTimingApi_InvalidSession()
    {
        var formula1 = new Formula1(_logger!);
        await formula1.StartAsync("fp4");

        Assert.IsFalse(formula1.Connected);
    }

    [TestMethod]
    public async Task Start_LiveTimingApi_StopSuccessfully()
    {
        var formula1 = new Formula1(_logger!);
        await formula1.StartAsync("gp");
        await Task.Delay(5000);
        await formula1.StopAsync();

        Assert.IsFalse(formula1.Connected);
    }
}