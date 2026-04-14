using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(User);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, cancellationToken);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(User);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new { unreadCount = count });
    }

    [HttpPut("{id:guid}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(User);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var updated = await _notificationService.MarkAsReadAsync(userId, id, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = ResolveUserId(User);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var updated = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { updated });
    }

    private static Guid ResolveUserId(ClaimsPrincipal user)
    {
        var userIdClaim =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : Guid.Empty;
    }
}
