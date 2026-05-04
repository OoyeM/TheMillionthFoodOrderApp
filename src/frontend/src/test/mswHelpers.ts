import { http, HttpResponse, type HttpHandler } from 'msw';

type Method = 'get' | 'post' | 'put' | 'delete' | 'patch';

/**
 * Returns an MSW handler that responds with the given status (and optional body)
 * to a single request to the given path.
 *
 * Use this to replace inline `server.use(http.get(path, () => new HttpResponse(null, { status })))`
 * blocks in tests.
 */
export function mockEndpoint<TBody = unknown>(
  method: Method,
  path: string,
  status: number,
  body?: TBody,
): HttpHandler {
  const responder = () => {
    if (body === undefined) {
      return new HttpResponse(null, { status });
    }
    return HttpResponse.json(body, { status });
  };
  return http[method](path, responder);
}
