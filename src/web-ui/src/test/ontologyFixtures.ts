import type { TaxonomyNode, ValueVocabulary } from '@/api/client';

// A trimmed copy of real GET /api/taxonomy and GET /api/vocabularies output, for tests that need an ontology
// without a running API. The names match domain-ontology.ttl so a failing test reads like the real UI.

function concept(notation: string, label: string, narrower: TaxonomyNode[] = []): TaxonomyNode {
    return {
        notation,
        label,
        labels: { en: label },
        altLabels: [],
        definition: null,
        icon: null,
        isDeviceType: false,
        narrower,
    };
}

export const taxonomyFixture: TaxonomyNode[] = [
    concept('computers', 'Computers', [concept('laptops', 'Laptops')]),
    concept('power', 'Power', [
        concept('chargers', 'Chargers', [
            concept('laptop-chargers', 'Laptop chargers'),
            concept('usb-c-pd-chargers', 'USB-C PD chargers'),
        ]),
        concept('power-banks', 'Power banks'),
    ]),
];

export const vocabulariesFixture: ValueVocabulary[] = [
    {
        notation: 'connectors',
        label: 'Connectors',
        specs: ['chargingPort', 'connector'],
        values: [
            {
                notation: 'barrel-5.5mm',
                label: '5.5mm barrel',
                labels: { en: '5.5mm barrel' },
                altLabels: ['barrel connector', 'DC barrel jack'],
            },
            {
                notation: 'usb-c',
                label: 'USB-C',
                labels: { en: 'USB-C' },
                altLabels: ['Type-C', 'USB Type-C'],
            },
        ],
    },
    {
        notation: 'memory-types',
        label: 'Memory types',
        specs: ['memoryType'],
        values: [
            {
                notation: 'ddr5-sodimm',
                label: 'DDR5 SO-DIMM',
                labels: { en: 'DDR5 SO-DIMM' },
                altLabels: ['DDR5'],
            },
        ],
    },
];
