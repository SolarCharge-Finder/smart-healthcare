import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';

import { notificationApi } from '../lib/api';
import { authStorage } from '../modules/auth/infra/authStorage';
import { NotificationItem, UnreadCountResponse } from '../types/notification';

function authHeaders() {
  const token = authStorage.getToken();

  if (!token) {
    return {};
  }

  return {
    Authorization: `Bearer ${token}`,
  };
}

function hasAuthToken() {
  return Boolean(authStorage.getToken());
}

export function useNotifications(enabled: boolean) {
  return useQuery<NotificationItem[]>({
    queryKey: ['notifications'],
    enabled: enabled && hasAuthToken(),
    refetchInterval: 30000,
    queryFn: async () => {
      try {
        const { data } = await notificationApi.get<NotificationItem[]>('/notifications', {
          headers: authHeaders(),
        });
        return data;
      } catch (error) {
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          authStorage.clear();
          throw new Error('Session expired. Please log in again to view notifications.');
        }

        throw error;
      }
    },
    retry: false,
  });
}

export function useUnreadNotificationCount(enabled: boolean) {
  return useQuery<number>({
    queryKey: ['notifications', 'unread-count'],
    enabled: enabled && hasAuthToken(),
    refetchInterval: 30000,
    queryFn: async () => {
      try {
        const { data } = await notificationApi.get<UnreadCountResponse>(
          '/notifications/unread-count',
          {
            headers: authHeaders(),
          },
        );
        return data.unreadCount;
      } catch (error) {
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          authStorage.clear();
          throw new Error('Session expired. Please log in again to view notifications.');
        }

        throw error;
      }
    },
    retry: false,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await notificationApi.put(`/notifications/${id}/mark-as-read`, undefined, {
        headers: authHeaders(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      await notificationApi.put('/notifications/mark-all-as-read', undefined, {
        headers: authHeaders(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  });
}
