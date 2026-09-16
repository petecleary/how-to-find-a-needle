// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import type { ProductResult } from '@/api/client';
import type { RuleCheck } from '@/lib/traceDetails';
import { gq01OntologyResponse } from '@/test/responses';
import { FlaggedCard } from './FlaggedCard';

afterEach(cleanup);

const barrel = gq01OntologyResponse.results.find((product) => product.id === 'PROD-0014')!;

const queryCheck: RuleCheck = {
    candidateId: 'PROD-0014',
    rule: 'chargers → laptops',
    definition: "The charger's plug must fit the laptop's charging port.",
    accessorySpec: 'connector',
    accessoryValue: '5.5mm barrel',
    operator: 'equals',
    deviceSpec: 'chargingPort',
    deviceValue: 'USB-C',
    result: 'Fail',
    source: 'Query',
};

describe('FlaggedCard', () => {
    it('says "you asked for" when the check ran against the query instead of a device', () => {
        render(<FlaggedCard product={barrel} checks={[queryCheck]} icon={null} />);

        expect(screen.getByText('· you asked for')).not.toBeNull();
        expect(screen.queryByText('· needs')).toBeNull();
    });

    it('says "needs" for a check against the target device', () => {
        render(<FlaggedCard product={barrel} checks={[{ ...queryCheck, source: 'Device' }]} icon={null} />);

        expect(screen.getByText('· needs')).not.toBeNull();
    });

    it('lists which catalogue devices the product fits, when there is no target device', () => {
        const product: ProductResult = {
            ...barrel,
            compatibility: {
                ...barrel.compatibility,
                fits: [
                    {
                        deviceType: 'laptops',
                        deviceTypeLabel: 'Laptops',
                        total: 13,
                        devices: [{ id: 'PROD-0003', name: 'Blackbird Workmate 15' }],
                    },
                ],
            },
        };

        render(<FlaggedCard product={product} checks={[queryCheck]} icon={null} />);

        expect(screen.getByText('Fits 1 of 13 laptops')).not.toBeNull();
        expect(screen.getByText('Blackbird Workmate 15')).not.toBeNull();
    });
});
