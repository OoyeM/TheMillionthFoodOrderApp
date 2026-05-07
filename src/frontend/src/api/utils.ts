import type { AxiosResponse } from 'axios';

/**
 * Extracts the `.data` payload from an Axios response.
 * Designed for use in `.then()` chains:
 *   apiClient.get<T>(url).then(extractData)
 */
export const extractData = <T>(response: AxiosResponse<T>): T => response.data;

/**
 * Discards the response and returns `undefined`.
 * Designed for use in `.then()` chains on void endpoints (e.g. DELETE, void PUT/PATCH):
 *   apiClient.delete(url).then(toVoid)
 */
export const toVoid = (): undefined => undefined;
