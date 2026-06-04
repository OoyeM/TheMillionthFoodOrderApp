import { z } from 'zod';

export const shopEditSchema = z.object({
  name: z.string().min(1, { message: 'Name is required.' }),
  address: z.object({
    street: z.string().min(1, { message: 'Street is required.' }),
    number: z.string().min(1, { message: 'House number is required.' }),
    city: z.string().min(1, { message: 'City is required.' }),
    postalCode: z.string().min(1, { message: 'Postal code is required.' }),
    country: z.string().min(1),
  }),
  contactEmail: z.email({ message: 'Enter a valid email address.' }),
  contactPhone: z.string(),
  vatNumber: z.string(),
  kitchenDisplayEnabled: z.boolean(),
  ticketPrinterEnabled: z.boolean(),
  pushNotificationEnabled: z.boolean(),
  soundAlertEnabled: z.boolean(),
  // Eat-in ordering (US-FP-066)
  eatInEnabled: z.boolean(),
  eatInRequiresTableNumber: z.boolean(),
  // Time-slot ordering (US-FP-020). The interval <select> constrains the value to 5/10/15;
  // max-orders is only meaningful (and only rendered) when time-slot ordering is enabled,
  // but a sensible default keeps it valid while disabled.
  timeSlotOrderingEnabled: z.boolean(),
  timeSlotIntervalMinutes: z.number(),
  maxOrdersPerTimeSlot: z.number().int().min(1, { message: 'Max orders per slot must be at least 1.' }),
});

export type ShopEditFormValues = z.infer<typeof shopEditSchema>;
