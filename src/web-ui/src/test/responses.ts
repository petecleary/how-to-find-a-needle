import type { SearchResponse } from '@/api/client';
import gq01Ontology from './fixtures/gq-01-ontology.json';
import gq04Structured from './fixtures/gq-04-structured.json';

// Real API responses for tests, captured from a running AppHost. To refresh one, post the golden query's
// preset (GET /api/demo/queries) to its stage with `pageSize: 50`, and save the JSON over the file.
//
// JSON imports type their strings loosely ("Incompatible" is just `string`), so the cast is needed. The
// file was produced by the API, whose contract `SearchResponse` describes.

/** GQ-01 "power adapter for my laptop", target device PROD-0001, on POST /api/search/ontology. */
export const gq01OntologyResponse = gq01Ontology as unknown as SearchResponse;

/** GQ-04: brand Brakk, at most £100, `voltageV: 18`, no query, on POST /api/search/structured. */
export const gq04StructuredResponse = gq04Structured as unknown as SearchResponse;
