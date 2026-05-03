using RaceControl.Categories;

namespace RaceControl.Tests.Categories;

[TestClass]
public class Formula1Tests
{
    [TestMethod]
    public async Task Start_LiveTimingApi_ValidSession()
    {
        var formula1 = new Formula1();
        await formula1.StartAsync("gp");

        Assert.IsTrue(formula1.Connected);
    }

    [TestMethod]
    public async Task Start_LiveTimingApi_InvalidSession()
    {
        var formula1 = new Formula1();
        await formula1.StartAsync("fp4");

        Assert.IsFalse(formula1.Connected);
    }
}