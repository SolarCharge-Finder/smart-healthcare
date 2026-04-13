"use client";

import { useMemo, useState } from "react";

import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
  useUnreadNotificationCount,
} from "../../hooks/useNotifications";
import { NotificationItem } from "../../types/notification";

function formatTime(value: string) {
  return new Date(value).toLocaleString();
}

function sortUnreadFirst(items: NotificationItem[]) {
  return [...items].sort((a, b) => {
    if (a.isRead === b.isRead) {
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    }

    return a.isRead ? 1 : -1;
  });
}

export default function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const notifications = useNotifications(true);
  const unreadCount = useUnreadNotificationCount(true);
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const items = useMemo(
    () => sortUnreadFirst(notifications.data ?? []),
    [notifications.data]
  );

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="relative inline-flex h-10 w-10 items-center justify-center rounded-full border border-gray-200 bg-white text-gray-600 hover:bg-gray-50"
        aria-label="Notifications"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="currentColor"
          className="h-5 w-5"
        >
          <path d="M12 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 006 14h12a1 1 0 00.707-1.707L18 11.586V8a6 6 0 00-6-6z" />
          <path d="M9.5 16a2.5 2.5 0 005 0h-5z" />
        </svg>

        {(unreadCount.data ?? 0) > 0 ? (
          <>
            <span className="absolute -right-0.5 -top-0.5 inline-flex h-3 w-3 rounded-full bg-red-500" />
            <span className="absolute -right-2 -top-2 inline-flex min-h-5 min-w-5 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold text-white">
              {(unreadCount.data ?? 0) > 99 ? "99+" : unreadCount.data}
            </span>
          </>
        ) : null}
      </button>

      {isOpen ? (
        <div className="absolute right-0 z-50 mt-2 w-[22rem] overflow-hidden rounded-xl border border-gray-200 bg-white shadow-2xl">
          <div className="flex items-center justify-between border-b border-gray-100 px-4 py-3">
            <p className="text-sm font-semibold text-gray-900">Notifications</p>
            <button
              type="button"
              onClick={() => markAllRead.mutate()}
              className="text-xs font-medium text-blue-600 hover:text-blue-700"
              disabled={markAllRead.isPending}
            >
              Mark all as read
            </button>
          </div>

          <div className="max-h-96 overflow-y-auto">
            {notifications.isLoading ? (
              <p className="px-4 py-3 text-sm text-gray-500">Loading notifications...</p>
            ) : null}

            {notifications.isError ? (
              <p className="px-4 py-3 text-sm text-red-600">
                {notifications.error instanceof Error
                  ? notifications.error.message
                  : "Failed to load notifications."}
              </p>
            ) : null}

            {!notifications.isLoading && items.length === 0 ? (
              <p className="px-4 py-3 text-sm text-gray-500">No notifications yet.</p>
            ) : null}

            {items.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`w-full border-b border-gray-100 px-4 py-3 text-left transition hover:bg-gray-50 ${
                  item.isRead ? "bg-white" : "bg-blue-50/70"
                }`}
                onClick={() => markRead.mutate(item.id)}
              >
                <p className="text-sm font-semibold text-gray-900">{item.title}</p>
                <p className="mt-1 text-xs text-gray-600">{item.message}</p>
                <p className="mt-1 text-[11px] text-gray-500">{formatTime(item.createdAt)}</p>
              </button>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}
