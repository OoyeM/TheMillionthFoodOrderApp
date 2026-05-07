import { z } from 'zod';

const modifierTranslationSchema = z.object({
  name: z.string(),
});

const modifierItemSchema = z.object({
  id: z.string().optional(),
  translations: z.object({
    nl: modifierTranslationSchema,
    fr: modifierTranslationSchema,
    de: modifierTranslationSchema,
  }),
  priceAdjustment: z.number(),
});

export const modifierGroupEditSchema = z.object({
  translations: z.object({
    nl: z.object({ name: z.string().min(1, { message: 'Dutch name is required' }) }),
    fr: modifierTranslationSchema,
    de: modifierTranslationSchema,
  }),
  modifiers: z.array(modifierItemSchema).min(1),
});

export type ModifierGroupEditFormValues = z.infer<typeof modifierGroupEditSchema>;
