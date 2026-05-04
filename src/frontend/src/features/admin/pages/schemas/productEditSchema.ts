import { z } from 'zod';

const translationSchema = z.object({
  name: z.string(),
  description: z.string(),
});

export const productEditSchema = z.object({
  basePrice: z.number().positive(),
  imageUrl: z.string(),
  translations: z.object({
    nl: z.object({
      name: z.string().min(1, { message: 'Dutch name is required' }),
      description: z.string(),
    }),
    fr: translationSchema,
    de: translationSchema,
  }),
  allergens: z.array(z.number()),
  dietaryTags: z.array(z.number()),
});

export type ProductEditFormValues = z.infer<typeof productEditSchema>;
