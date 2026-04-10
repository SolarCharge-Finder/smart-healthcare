using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using TelemedicineService.Data;
using TelemedicineService.Models;
using TelemedicineService.Services;

namespace TelemedicineService.Tests;

public class TelemedicineSessionServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_WithPaidAppointment_CreatesSessionAndReturnsResponse()
    {
        var appointmentId = Guid.NewGuid();
        await using var db = CreateDbContext();

        using var client = new HttpClient(new JsonResponseHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = appointmentId, status = "Paid" })))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var tokenService = new FakeAgoraTokenService();
        var service = CreateService(db, tokenService, client);

        var result = await service.CreateSessionAsync(appointmentId);

        Assert.Equal(appointmentId, result.AppointmentId);
        Assert.Equal("app-id", result.AgoraAppId);
        Assert.Equal($"appointment-{appointmentId}", result.ChannelName);
        Assert.NotEmpty(result.PatientToken);
        Assert.NotEmpty(result.DoctorToken);

        var session = await db.TelemedicineSessions.SingleAsync(s => s.AppointmentId == appointmentId);
        Assert.Equal(result.ChannelName, session.ChannelName);
        Assert.True(session.TokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateSessionAsync_WithMissingAppointment_ThrowsKeyNotFoundException()
    {
        await using var db = CreateDbContext();

        using var client = new HttpClient(new StatusThrowingHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var service = CreateService(db, new FakeAgoraTokenService(), client);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateSessionAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateSessionAsync_WithUnpaidAppointment_ThrowsInvalidOperationException()
    {
        var appointmentId = Guid.NewGuid();
        await using var db = CreateDbContext();

        using var client = new HttpClient(new JsonResponseHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = appointmentId, status = "Pending" })))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var service = CreateService(db, new FakeAgoraTokenService(), client);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSessionAsync(appointmentId));
        Assert.Contains("must be 'Paid'", ex.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_WithExistingActiveSession_DoesNotCreateDuplicate()
    {
        var appointmentId = Guid.NewGuid();
        await using var db = CreateDbContext();

        db.TelemedicineSessions.Add(new TelemedicineSession
        {
            AppointmentId = appointmentId,
            ChannelName = $"appointment-{appointmentId}",
            TokenExpiresAt = DateTime.UtcNow.AddMinutes(20),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        using var client = new HttpClient(new JsonResponseHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = appointmentId, status = "Paid" })))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var tokenService = new FakeAgoraTokenService();
        var service = CreateService(db, tokenService, client);

        var result = await service.CreateSessionAsync(appointmentId);

        Assert.Equal($"appointment-{appointmentId}", result.ChannelName);
        Assert.Equal(1, await db.TelemedicineSessions.CountAsync(s => s.AppointmentId == appointmentId));
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithExpiredSession_ReturnsNull()
    {
        var appointmentId = Guid.NewGuid();
        await using var db = CreateDbContext();

        db.TelemedicineSessions.Add(new TelemedicineSession
        {
            AppointmentId = appointmentId,
            ChannelName = $"appointment-{appointmentId}",
            TokenExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        using var client = new HttpClient(new JsonResponseHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = appointmentId, status = "Paid" })))
        {
            BaseAddress = new Uri("http://appointment-service/")
        };

        var service = CreateService(db, new FakeAgoraTokenService(), client);

        var result = await service.GetActiveSessionAsync(appointmentId);

        Assert.Null(result);
    }

    private static TelemedicineSessionService CreateService(
        TelemedicineDbContext db,
        IAgoraTokenService tokenService,
        HttpClient client)
    {
        return new TelemedicineSessionService(
            db,
            tokenService,
            client,
            NullLogger<TelemedicineSessionService>.Instance,
            Options.Create(new AgoraOptions
            {
                AppId = "app-id",
                AppCertificate = "app-cert",
                TokenExpirySeconds = 3600
            }));
    }

    private static TelemedicineDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TelemedicineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TelemedicineDbContext(options);
    }

    private sealed class FakeAgoraTokenService : IAgoraTokenService
    {
        public string GenerateToken(string channelName, uint expirySeconds = 3600)
            => $"token-{channelName}-{expirySeconds}";
    }

    private sealed class JsonResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _payload;

        public JsonResponseHandler(HttpStatusCode statusCode, string json)
        {
            _statusCode = statusCode;
            _payload = Encoding.UTF8.GetBytes(json);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_payload)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class StatusThrowingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StatusThrowingHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Request failed.", null, _statusCode);
        }
    }
}
