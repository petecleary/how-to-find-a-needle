import type { SearchResponse } from '@/api/client';
import gq01Ontology from './fixtures/gq-01-ontology.json';

// Real API responses for tests, captured from a running AppHost. To refresh one, post the golden query's
// preset (GET /api/demo/queries) to its stage with `pageSize: 50`, and save the JSON over the file.
//
// JSON imports type their strings loosely ("Incompatible" is just `string`), so the cast is needed. The
// file was produced by the API, whose contract `SearchResponse` describes.

/** GQ-01 "power adapter for my laptop", target device PROD-0001, on POST /api/search/ontology. */
export const gq01OntologyResponse = gq01Ontology as unknown as SearchResponse;
