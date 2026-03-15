/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_MOCK_AUTH: string;
  readonly VITE_MOCK_ROLE: string;
  readonly VITE_MOCK_DISPLAY_NAME: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
