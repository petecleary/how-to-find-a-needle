import type { components } from '@/api/schema';

// DeviceFits — with no target device, which of the catalog's devices a product fits: "Fits 6 of 13 laptops". Stage 5
// runs the same rules against every device of the type they name (ADR-0013). It's context, not a verdict: the
// product's status and order don't change. The names are one click away, so a row stays one line tall.

type DeviceFit = components['schemas']['DeviceFit'];

export interface DeviceFitsProps {
    fits: DeviceFit[] | null | undefined;
}

export function DeviceFits({ fits }: DeviceFitsProps) {
    if (!fits || fits.length === 0) {
        return null;
    }

    return (
        <>
            {fits.map((fit) => {
                const type = fit.deviceTypeLabel.toLowerCase();
                const summary =
                    fit.devices.length === 0
                        ? `Fits none of the ${fit.total} ${type} in the catalogue`
                        : `Fits ${fit.devices.length} of ${fit.total} ${type}`;

                return fit.devices.length === 0 ? (
                    <span key={fit.deviceType} className="text-[13px] text-muted-foreground">
                        {summary}
                    </span>
                ) : (
                    <details key={fit.deviceType} className="text-[13px] text-muted-foreground">
                        <summary className="cursor-pointer">{summary}</summary>
                        <span>{fit.devices.map((device) => device.name).join(', ')}</span>
                    </details>
                );
            })}
        </>
    );
}
