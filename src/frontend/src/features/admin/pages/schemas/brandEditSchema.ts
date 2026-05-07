import { z } from 'zod';

export const brandEditSchema = z.object({
  name: z.string().min(1, { message: 'Name is required.' }),
  contactEmail: z.string().email({ message: 'Enter a valid email address.' }),
  contactPhone: z.string(),
});

export type BrandEditFormValues = z.infer<typeof brandEditSchema>;
