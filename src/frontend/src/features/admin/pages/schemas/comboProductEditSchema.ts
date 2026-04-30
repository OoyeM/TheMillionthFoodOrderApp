import { z } from 'zod';

const translationSchema = z.object({
  name: z.string(),
  description: z.string(),
});

export const comboProductEditSchema = z.object({
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
  componentProductIds: z.array(z.string()).min(2, { message: 'At least 2 component products required' }),
});

export type ComboProductEditFormValues = z.infer<typeof comboProductEditSchema>;
