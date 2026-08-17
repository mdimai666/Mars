import * as Blockly from 'blockly/core';
import {
    CREATE_FUNCTION_CALLBACK, FUNCTION_CALL, FUNCTION_CALL_OUTPUT, FUNCTION_DEFINITION,
    FUNCTION_RETURN, DEFAULT_FUNCTION_NAME, FunctionArgument,
} from './constants';

// Реестр/обновление функций (аналог utils.ts/functionManager.ts плагина PXT),
// без своего undo-события: мутации группируются в Blockly events group.

export interface FunctionExtraState {
    name: string;
    functionid: string;
    arguments: FunctionArgument[];
}

export interface FunctionExtraStateLike {
    name?: string;
    functionid?: string;
    arguments?: FunctionArgument[];
}

// Динамические методы блоков семейства функций (объявлены в blocks.ts);
// BlockSvg — ради initSvg/render/select в менеджере и диалоге.
export interface FunctionBlock extends Blockly.BlockSvg {
    name_: string;
    functionId_: string;
    arguments_: FunctionArgument[];
    updateDisplay_(): void;
    updateArgumentInputs_(): void;
    loadExtraState(state: FunctionExtraStateLike): void;
    saveExtraState(): FunctionExtraState;
    mutationToDom(): Element;
    domToMutation(element: Element): void;
    addNumberExternal(): void;
    addStringExternal(): void;
    addBooleanExternal(): void;
    addArrayExternal(): void;
    addParam_(type: string, defaultName: string): void;
    updateFunctionSignature(): void;
    updateShape_(): void;
    returnValueVisible_?: boolean;
}

export function asFunctionBlock(block: Blockly.Block): FunctionBlock {
    return block as unknown as FunctionBlock;
}

export function getAllDefinitions(workspace: Blockly.Workspace): FunctionBlock[] {
    return workspace.getTopBlocks(false)
        .filter((b) => b.type === FUNCTION_DEFINITION)
        .map(asFunctionBlock);
}

export function getDefinition(name: string, workspace: Blockly.Workspace): FunctionBlock | null {
    return getAllDefinitions(workspace).find((b) => b.name_ === name) ?? null;
}

export function getCallers(name: string, workspace: Blockly.Workspace): FunctionBlock[] {
    return workspace.getAllBlocks(false)
        .filter((b) => (b.type === FUNCTION_CALL || b.type === FUNCTION_CALL_OUTPUT)
            && asFunctionBlock(b).name_ === name)
        .map(asFunctionBlock);
}

export function extraStateOf(block: FunctionBlock, nameOverride?: string): FunctionExtraState {
    return {
        name: nameOverride ?? block.name_,
        functionid: block.functionId_,
        arguments: block.arguments_.map((a) => ({ ...a })),
    };
}

// Имя, свободное от других функций и переменных (числовой суффикс при коллизии).
export function findLegalName(name: string, workspace: Blockly.Workspace, source: FunctionBlock | null): string {
    let candidate = name.trim() || DEFAULT_FUNCTION_NAME;
    const base = candidate;
    const inUse = (n: string) =>
        getAllDefinitions(workspace).some((d) => d !== source && d.name_ === n)
        || workspace.getVariableMap().getAllVariables().some((v) => v.getName() === n);
    let suffix = 2;
    while (inUse(candidate)) candidate = `${base}${suffix++}`;
    return candidate;
}

// Применение новой сигнатуры к определению и всем вызовам (rename/edit из диалога).
export function mutateCallersAndDefinition(
    oldName: string,
    workspace: Blockly.Workspace,
    state: FunctionExtraState,
): void {
    const group = Blockly.Events.getGroup();
    Blockly.Events.setGroup(group || true);
    try {
        const definition = getDefinition(oldName, workspace);
        const oldParams = definition ? definition.arguments_.map((a) => ({ ...a })) : [];
        if (definition) {
            definition.name_ = state.name;
            if (state.functionid) definition.functionId_ = state.functionid;
            definition.arguments_ = state.arguments.map((a) => ({ ...a }));
            definition.updateDisplay_();
            // Репортёры в теле переименовываются вместе с параметрами (по id).
            for (const oldParam of oldParams) {
                const updated = state.arguments.find((a) => a.id === oldParam.id);
                if (!updated || updated.name === oldParam.name) continue;
                for (const descendant of definition.getDescendants(false)) {
                    if (!descendant.type.startsWith('argument_reporter_')) continue;
                    if (descendant.getFieldValue('VALUE') === oldParam.name) {
                        descendant.setFieldValue(updated.name, 'VALUE');
                    }
                }
            }
        }
        for (const caller of getCallers(oldName, workspace)) {
            caller.name_ = state.name;
            caller.functionId_ = state.functionid || caller.functionId_;
            caller.arguments_ = state.arguments.map((a) => ({ ...a }));
            caller.updateDisplay_();
        }
    } finally {
        Blockly.Events.setGroup(group);
    }
}

// Валидатор поля имени определения: обрезает, уникализирует, разносит мутацию.
export function renameValidator(this: Blockly.FieldTextInput, name: string): string {
    const block = asFunctionBlock(this.getSourceBlock() as Blockly.Block);
    if (!block?.workspace || block.isInFlyout) return name.trim();
    const legal = findLegalName(name, block.workspace, block);
    if (block.name_ && legal !== block.name_) {
        const old = block.name_;
        queueMicrotask(() => {
            if (block.disposed) return;
            mutateCallersAndDefinition(old, block.workspace, extraStateOf(block, legal));
        });
    }
    return legal;
}

// Проверки диалога (аналог validateFunctionExternal): null — сигнатура легальна.
export function validateFunction(state: FunctionExtraState, workspace: Blockly.Workspace): string | null {
    const msg = Blockly.Msg;
    if (!state.name.trim()) return msg['FUNCTION_WARNING_EMPTY_NAME'];
    for (const arg of state.arguments) {
        if (!arg.name.trim()) return msg['FUNCTION_WARNING_EMPTY_NAME'];
    }
    const names = state.arguments.map((a) => a.name);
    if (new Set(names).size !== names.length) return msg['FUNCTION_WARNING_DUPLICATE_ARG'];
    if (names.includes(state.name)) return msg['FUNCTION_WARNING_ARG_NAME_IS_FUNCTION_NAME'];
    const otherFunction = getAllDefinitions(workspace)
        .find((d) => d.name_ === state.name && d.functionId_ !== state.functionid);
    const variable = workspace.getVariableMap().getAllVariables().find((v) => v.getName() === state.name);
    if (otherFunction || variable) {
        return (msg['VARIABLE_ALREADY_EXISTS'] ?? '"%1" is already in use.')
            .replace('%1', state.name);
    }
    return null;
}

export function newFunctionExtraState(workspace: Blockly.Workspace): FunctionExtraState {
    return {
        name: findLegalName(DEFAULT_FUNCTION_NAME, workspace, null),
        functionid: Blockly.utils.idGenerator.genUid(),
        arguments: [],
    };
}

// Есть ли в теле определения return со значением — тогда в flyout нужен и call_output.
export function definitionHasReturnValue(definition: FunctionBlock): boolean {
    return definition.getDescendants(false).some((d) =>
        d.type === FUNCTION_RETURN && !!d.getInput('RETURN_VALUE')?.connection?.targetBlock());
}

// Flyout категории «Functions» (MakeCode): кнопка создания, return с +/−,
// затем по функции — call (и call_output, если функция возвращает значение).
export function functionsFlyout(workspace: Blockly.WorkspaceSvg): Blockly.utils.toolbox.FlyoutItemInfo[] {
    const heading: Blockly.utils.toolbox.LabelInfo = {
        kind: 'label',
        text: Blockly.Msg['FUNCTION_FLYOUT_LABEL'],
        id: undefined,
    };
    (heading as unknown as Record<string, unknown>)['web-class'] = 'blocklyFlyoutHeading';
    const items: Blockly.utils.toolbox.FlyoutItemInfo[] = [
        { kind: 'button', text: Blockly.Msg['FUNCTION_CREATE_NEW'], callbackkey: CREATE_FUNCTION_CALLBACK },
        {
            // shadow «null» в сокете значения — как в прежнем flyout процедур.
            kind: 'block', type: FUNCTION_RETURN, gap: 16,
            inputs: { RETURN_VALUE: { shadow: { type: 'core.logic.null' } } },
        },
        {
            kind: 'block', type: 'core.functions.if_return', gap: 24,
            inputs: {
                CONDITION: { shadow: { type: 'core.logic.boolean', fields: { BOOL: 'TRUE' } } },
                VALUE: { shadow: { type: 'core.logic.null' } },
            },
        },
        heading,
    ];
    for (const definition of getAllDefinitions(workspace)) {
        const extra = extraStateOf(definition);
        items.push({ kind: 'block', type: FUNCTION_CALL, gap: 16, extraState: extra as unknown as Record<string, unknown> });
        if (definitionHasReturnValue(definition)) {
            items.push({ kind: 'block', type: FUNCTION_CALL_OUTPUT, gap: 16, extraState: extra as unknown as Record<string, unknown> });
        }
    }
    return items;
}
