import { Package, type LucideProps } from 'lucide-react';
import { DynamicIcon, iconNames, type IconName } from 'lucide-react/dynamic';

export interface CategoryIconProps extends Omit<LucideProps, 'name'> {
    /** A Lucide icon name from the taxonomy's `ex:icon`, e.g. "plug". */
    name: string | null;
}

/**
 * A category's icon, loaded by the name the TTL gives it, so a new category brings its own icon with no UI
 * change. An unknown or missing name shows a plain package.
 */
export function CategoryIcon({ name, ...props }: CategoryIconProps) {
    if (!isIconName(name)) {
        return <Package {...props} />;
    }

    return <DynamicIcon name={name} {...props} />;
}

function isIconName(name: string | null): name is IconName {
    return name !== null && iconNames.some((iconName) => iconName === name);
}
