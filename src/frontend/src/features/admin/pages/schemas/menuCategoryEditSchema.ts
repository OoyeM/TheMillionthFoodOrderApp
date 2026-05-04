import { z } from 'zod';

const localeNameSchema = z.object({ name: z.string() });

export const menuCategoryEditSchema = z.object({
  sortOrder: z.number().int().nonnegative(),
  imageUrl: z.string(),
  translations: z.object({
    nl: z.object({ name: z.string().min(1, { message: 'Dutch name is required' }) }),
    fr: localeNameSchema,
    de: localeNameSchema,
  }),
});

export type MenuCategoryEditFormValues = z.infer<typeof menuCategoryEditSchema>;
