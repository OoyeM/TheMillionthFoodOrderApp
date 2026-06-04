import type { UseFormRegister } from 'react-hook-form';
import type { TFunction } from 'i18next';
import type { SupportedLocale } from '../../../types/common';
import type { ProductEditFormValues } from '../pages/schemas/productEditSchema';
import { labelStyle, inputStyle, RequiredMark, FieldError } from './adminFormStyles';

// ---------------------------------------------------------------------------
// ProductTranslationFields — shared by ProductCreate and ProductEdit (both use
// the same ProductEditFormValues schema). Kept as a component with literal
// `register` path strings, which TypeScript resolves correctly (dynamic
// template-literal paths produce unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

export interface ProductTranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<ProductEditFormValues>;
  nlNameError: string | undefined;
  t: TFunction;
}

export function ProductTranslationFields({
  activeTab,
  register,
  nlNameError,
  t,
}: ProductTranslationFieldsProps): React.JSX.Element {
  if (activeTab === 'nl') {
    return (
      <>
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name-nl">
            Name <RequiredMark />
          </label>
          <input
            id="name-nl"
            type="text"
            {...register('translations.nl.name')}
            style={inputStyle(!!nlNameError)}
            placeholder={t('admin.products.namePlaceholder', { lang: 'NL' })}
          />
          {nlNameError && <FieldError message={nlNameError} />}
        </div>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="desc-nl">
            {t('admin.products.description')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
          </label>
          <textarea
            id="desc-nl"
            {...register('translations.nl.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={t('admin.products.descriptionPlaceholder', { lang: 'NL' })}
          />
        </div>
      </>
    );
  }
  if (activeTab === 'fr') {
    return (
      <>
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name-fr">
            Name
          </label>
          <input
            id="name-fr"
            type="text"
            {...register('translations.fr.name')}
            style={inputStyle(false)}
            placeholder={t('admin.products.namePlaceholder', { lang: 'FR' })}
          />
        </div>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="desc-fr">
            {t('admin.products.description')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
          </label>
          <textarea
            id="desc-fr"
            {...register('translations.fr.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={t('admin.products.descriptionPlaceholder', { lang: 'FR' })}
          />
        </div>
      </>
    );
  }
  return (
    <>
      <div style={{ marginBottom: '1rem' }}>
        <label style={labelStyle} htmlFor="name-de">
          Name
        </label>
        <input
          id="name-de"
          type="text"
          {...register('translations.de.name')}
          style={inputStyle(false)}
          placeholder={t('admin.products.namePlaceholder', { lang: 'DE' })}
        />
      </div>
      <div style={{ marginBottom: '1.5rem' }}>
        <label style={labelStyle} htmlFor="desc-de">
          {t('admin.products.description')}{' '}
          <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
        </label>
        <textarea
          id="desc-de"
          {...register('translations.de.description')}
          rows={3}
          style={{ ...inputStyle(false), resize: 'vertical' }}
          placeholder={t('admin.products.descriptionPlaceholder', { lang: 'DE' })}
        />
      </div>
    </>
  );
}
