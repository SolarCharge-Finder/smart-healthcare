import { useMutation } from '@tanstack/react-query';
import { getCurrentUser } from '../infra/authApi';

export function useRefreshUser() {
  return useMutation({
    mutationFn: getCurrentUser,
  });
}
