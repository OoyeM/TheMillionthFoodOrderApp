import { z } from 'zod';

export const shopCreateSchema = z.object({
  name: z.string().min(1, { message: 'Name is required.' }),
  slug: z
    .string()
    .min(1, { message: 'Slug is required.' })
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, {
      message: 'Slug must be lowercase letters, numbers and hyphens only (e.g. my-shop).',
    }),
  address: z.object({
    street: z.string().min(1, { message: 'Street is required.' }),
    number: z.string().min(1, { message: 'House number is required.' }),
    city: z.string().min(1, { message: 'City is required.' }),
    postalCode: z.string().min(1, { message: 'Postal code is required.' }),
    country: z.string().min(1),
  }),
  contactEmail: z.email({ message: 'Enter a valid email address.' }),
  contactPhone: z.string(),
});

export type ShopCreateFormValues = z.infer<typeof shopCreateSchema>;
