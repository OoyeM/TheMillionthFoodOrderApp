import { Suspense } from 'react';
import type { ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

/**
 * Wraps lazy-loaded route components with a Suspense boundary and
 * a consistent loading indicator.
 */
export function SuspenseWrapper({ children }: Props) {
  return <Suspense fallback={<div aria-label="Loading…">Loading…</div>}>{children}</Suspense>;
}
