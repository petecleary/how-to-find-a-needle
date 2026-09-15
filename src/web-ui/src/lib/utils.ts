import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/** Joins conditional class names; when two Tailwind classes conflict (`p-2 p-4`), the last one wins. */
export function cn(...inputs: ClassValue[]): string {
    return twMerge(clsx(inputs));
}
