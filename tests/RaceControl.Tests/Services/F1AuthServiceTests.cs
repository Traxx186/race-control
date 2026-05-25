using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RaceControl.Services;

namespace RaceControl.Tests.Services;

[TestClass]
public class F1AuthServiceTests
{
    private static ILogger<F1AuthService>? _logger;
    private static IHttpClientFactory? _httpClientFactory;
    private static IWebHostEnvironment? _environment;

    public TestContext TestContext { get; set; }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(m => m.ContentRootPath).Returns(Environment.CurrentDirectory);

        _logger = new Mock<ILogger<F1AuthService>>().Object;
        _httpClientFactory = httpClientFactoryMock.Object;
        _environment = environmentMock.Object;

        Directory.CreateDirectory("storage");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        var f1AuthService = new F1AuthService(_logger, _environment, _httpClientFactory);
        File.Delete(f1AuthService.AuthDataFile);
    }

    [TestMethod]
    public async Task StoreAuthToken_WithNoFile_TokenSaved()
    {
        const string token = "token";
        var f1AuthService = new F1AuthService(_logger, _environment, _httpClientFactory);

        // Store test token
        await f1AuthService.StoreAuthToken("token");

        var fileContent = await File.ReadAllTextAsync(f1AuthService.AuthDataFile, TestContext.CancellationToken);
        var authData = JsonSerializer.Deserialize<F1AuthService.AuthData>(fileContent);

        Assert.AreEqual(token, authData!.SubscriptionToken);
    }

    [TestMethod]
    public async Task StoreAuthToken_OverwriteExistingToken_TokenSaved()
    {
        const string token = "test123456";
        var f1AuthService = new F1AuthService(_logger, _environment, _httpClientFactory);

        // Store a token
        await f1AuthService.StoreAuthToken("token");

        // Store new token
        await f1AuthService.StoreAuthToken(token);

        var fileContent = await File.ReadAllTextAsync(f1AuthService.AuthDataFile, TestContext.CancellationToken);
        var authData = JsonSerializer.Deserialize<F1AuthService.AuthData>(fileContent);

        Assert.AreEqual(token, authData!.SubscriptionToken);
    }
}