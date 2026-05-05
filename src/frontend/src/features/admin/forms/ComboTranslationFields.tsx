import type { UseFormRegister } from 'react-hook-form';
import type { ComboProductEditFormValues } from '../pages/schemas/comboProductEditSchema';
import type { SupportedLocale } from '../../../types/common';
import { labelStyle, inputStyle, RequiredMark, FieldError } from './adminFormStyles';

// ---------------------------------------------------------------------------
// ComboTranslationFields — extracted so `register` call uses literal path
// strings, which TypeScript resolves correctly (dynamic template-literal paths
// produce unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

export interface ComboTranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<ComboProductEditFormValues>;
  nlNameError: string | undefined;
}

export function ComboTranslationFields({
  activeTab,
  register,
  nlNameError,
}: ComboTranslationFieldsProps): JSX.Element {
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
            placeholder="Combo name in NL"
          />
          {nlNameError && <FieldError message={nlNameError} />}
        </div>
        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor="desc-nl">
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id="desc-nl"
            {...register('translations.nl.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder="Combo description in NL"
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
            placeholder="Combo name in FR"
          />
        </div>
        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor="desc-fr">
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id="desc-fr"
            {...register('translations.fr.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder="Combo description in FR"
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
          placeholder="Combo name in DE"
        />
      </div>
      <div style={{ marginBottom: '0.5rem' }}>
        <label style={labelStyle} htmlFor="desc-de">
          Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
        </label>
        <textarea
          id="desc-de"
          {...register('translations.de.description')}
          rows={3}
          style={{ ...inputStyle(false), resize: 'vertical' }}
          placeholder="Combo description in DE"
        />
      </div>
    </>
  );
}
