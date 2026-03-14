import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
// Initialize i18n before rendering the app
import './i18n/config';

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Failed to find root element with id "root"');
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
