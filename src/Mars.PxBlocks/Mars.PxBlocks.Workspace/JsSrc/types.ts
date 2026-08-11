export interface PxTypeInfo {
    name: string;
    shape: 'rounded' | 'hexagonal' | 'square';
    compatibleWith: string[];
}

let typeRegistry = new Map<string, PxTypeInfo>();

export function setTypes(typesJson: string): void {
    const parsed = JSON.parse(typesJson) as { types: PxTypeInfo[] };
    typeRegistry = new Map(parsed.types.map(t => [t.name, t]));
}

export function shapeForType(name: string): PxTypeInfo['shape'] | undefined {
    return typeRegistry.get(name)?.shape;
}

// Точное совпадение имён стыкуется всегда; "*" — с любым типом;
// неизвестные друг другу типы без явной совместимости не стыкуются.
export function areTypesCompatible(a: string, b: string): boolean {
    if (a === b) {
        return true;
    }
    const ta = typeRegistry.get(a);
    const tb = typeRegistry.get(b);
    if (!ta && !tb) {
        return false;
    }
    return (
        ta?.compatibleWith.includes(b) === true ||
        ta?.compatibleWith.includes('*') === true ||
        tb?.compatibleWith.includes(a) === true ||
        tb?.compatibleWith.includes('*') === true
    );
}
