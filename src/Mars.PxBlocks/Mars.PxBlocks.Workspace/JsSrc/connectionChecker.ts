import * as Blockly from 'blockly/core';
import { areTypesCompatible } from './types';

// Матрица совместимости типов приходит из C# (PxTypeRegistry).
export class PxConnectionChecker extends Blockly.ConnectionChecker {
    override doTypeChecks(a: Blockly.Connection, b: Blockly.Connection): boolean {
        const checkA = a.getCheck();
        const checkB = b.getCheck();
        if (!checkA || !checkB) {
            return true;
        }
        for (const typeA of checkA) {
            for (const typeB of checkB) {
                if (areTypesCompatible(typeA, typeB)) {
                    return true;
                }
            }
        }
        return false;
    }
}

Blockly.registry.register(Blockly.registry.Type.CONNECTION_CHECKER, 'pxt', PxConnectionChecker, true);
