import { describe, expect, it } from 'vitest';
import { taxonomyFixture } from '@/test/ontologyFixtures';
import { categoryIcon, conceptLabel, narrowerNotations } from './taxonomy';

const withIcons = taxonomyFixture.map((concept) =>
    concept.notation === 'power'
        ? {
              ...concept,
              narrower: concept.narrower.map((child) => ({ ...child, icon: 'plug' })),
          }
        : concept,
);

describe('taxonomy lookups', () => {
    it('follows narrower concepts to any depth', () => {
        const [, power] = taxonomyFixture;

        expect(power && narrowerNotations(power)).toEqual([
            'chargers',
            'laptop-chargers',
            'usb-c-pd-chargers',
            'power-banks',
        ]);
    });

    it('labels a concept, falling back to its notation while the taxonomy loads', () => {
        expect(conceptLabel(taxonomyFixture, 'chargers')).toBe('Chargers');
        expect(conceptLabel(null, 'chargers')).toBe('chargers');
    });

    it("uses the first category's icon", () => {
        expect(categoryIcon(withIcons, ['chargers', 'laptops'])).toBe('plug');
        expect(categoryIcon(withIcons, ['laptops'])).toBeNull();
        expect(categoryIcon(null, ['chargers'])).toBeNull();
    });
});
