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
  contactEmail: z.string().email({ message: 'Enter a valid email address.' }),
  contactPhone: z.string(),
});

export type ShopEditFormValues = z.infer<typeof shopEditSchema>;
