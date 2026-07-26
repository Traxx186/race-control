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
}