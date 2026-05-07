import { z } from 'zod';

const hexColor = z
  .string()
  .regex(/^#[0-9a-f]{6}$/i, { message: 'Enter a valid hex color (e.g. #2563eb)' });

export const brandThemingSchema = z.object({
  colors: z.object({
    primary: hexColor,
    secondary: hexColor,
    accent: hexColor,
  }),
  typography: z.object({
    headingFont: z.string().min(1),
    bodyFont: z.string().min(1),
  }),
  customDomain: z.string(),
});

export type BrandThemingFormValues = z.infer<typeof brandThemingSchema>;
