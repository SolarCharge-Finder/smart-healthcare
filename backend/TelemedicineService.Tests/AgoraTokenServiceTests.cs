using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TelemedicineService.Services;

namespace TelemedicineService.Tests;

public class AgoraTokenServiceTests
{
    [Fact]
    public void GenerateToken_WithoutAppId_ThrowsInvalidOperationException()
    {
        var service = CreateService(new AgoraOptions
        {
            AppId = string.Empty,
            AppCertificate = "certificate"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken("channel-a"));
        Assert.Contains("AppId", ex.Message);
    }

    [Fact]
    public void GenerateToken_WithoutAppCertificate_ThrowsInvalidOperationException()
    {
        var service = CreateService(new AgoraOptions
        {
            AppId = "app-id",
            AppCertificate = string.Empty
        });

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken("channel-a"));
        Assert.Contains("AppCertificate", ex.Message);
    }

    private static AgoraTokenService CreateService(AgoraOptions options)
    {
        return new AgoraTokenService(
            Options.Create(options),
            NullLogger<AgoraTokenService>.Instance);
    }
}
