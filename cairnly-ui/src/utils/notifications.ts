import { toast } from '@heroui/react';

const SUCCESS_TOAST_TIMEOUT = 3000;

/** Shows a calm global success confirmation for completed user actions. */
export function showSuccessToast(message: string): void {
  toast.success(message, { timeout: SUCCESS_TOAST_TIMEOUT });
}
