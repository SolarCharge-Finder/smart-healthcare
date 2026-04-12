using System.Text;
using System.Threading;

namespace NotificationService.Services;

public static class NotificationMetrics
{
	private static long _eventsConsumed;
	private static long _eventFailures;
	private static long _notificationsCreated;
	private static long _remindersCreated;

	public static void IncEventsConsumed(int value = 1)
		=> Interlocked.Add(ref _eventsConsumed, value);

	public static void IncEventFailures(int value = 1)
		=> Interlocked.Add(ref _eventFailures, value);

	public static void IncNotificationsCreated(int value = 1)
		=> Interlocked.Add(ref _notificationsCreated, value);

	public static void IncRemindersCreated(int value = 1)
		=> Interlocked.Add(ref _remindersCreated, value);

	public static object Snapshot()
		=> new
		{
			eventsConsumed = Interlocked.Read(ref _eventsConsumed),
			eventFailures = Interlocked.Read(ref _eventFailures),
			notificationsCreated = Interlocked.Read(ref _notificationsCreated),
			remindersCreated = Interlocked.Read(ref _remindersCreated)
		};

	public static string SnapshotPrometheus()
	{
		var sb = new StringBuilder();

		sb.AppendLine("# TYPE notification_events_consumed_total counter");
		sb.AppendLine($"notification_events_consumed_total {Interlocked.Read(ref _eventsConsumed)}");

		sb.AppendLine("# TYPE notification_event_failures_total counter");
		sb.AppendLine($"notification_event_failures_total {Interlocked.Read(ref _eventFailures)}");

		sb.AppendLine("# TYPE notification_notifications_created_total counter");
		sb.AppendLine($"notification_notifications_created_total {Interlocked.Read(ref _notificationsCreated)}");

		sb.AppendLine("# TYPE notification_reminders_created_total counter");
		sb.AppendLine($"notification_reminders_created_total {Interlocked.Read(ref _remindersCreated)}");

		return sb.ToString();
	}
}
