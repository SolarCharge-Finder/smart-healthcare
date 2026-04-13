export interface NotificationItem {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: string | null;
}

export interface UnreadCountResponse {
  unreadCount: number;
}
