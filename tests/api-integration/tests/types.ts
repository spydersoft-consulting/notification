export interface NotificationDeliveryDto {
  channel: string; // "InApp" | "Email" | "Sms"
  status: string; // "Pending" | "Sent" | "Failed" | "Skipped"
  externalId: string | null;
  error: string | null;
  attemptedAt: string | null;
}

export interface NotificationDto {
  id: string;
  userId: string;
  source: string;
  type: string;
  subject: string;
  body: string;
  data: Record<string, string> | null;
  priority: string; // "Low" | "Normal" | "High"
  status: string; // "Created" | "Dispatching" | "Dispatched" | "PartiallyFailed"
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
  entityType: string | null;
  entityId: string | null;
  deliveries: NotificationDeliveryDto[] | null;
}

export interface DeviceDto {
  id: string;
  deviceType: string; // "Web" | "Ios" | "Android"
  label: string;
  lastSeenAt: string;
  registeredAt: string;
  isActive: boolean;
}

export interface NotificationPushDto {
  id: string;
  source: string;
  type: string;
  subject: string;
  body: string;
  priority: string;
  createdAt: string;
}
