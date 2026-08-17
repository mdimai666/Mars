import * as Blockly from 'blockly/core';
import 'blockly/blocks';
import './renderer';
import './connectionChecker';
import './extensions/objectBuilder';
import './extensions/hat';

export { setTypes } from './types';

// Английская локаль встроена в blockly/core; другие языки (ru) подключим на этапе локализации.

const defaultOptions: Blockly.BlocklyOptions = {
    renderer: 'pxt',
    media: '_content/Mars.PxBlocks.Workspace/media/',
    grid: {
        spacing: 20,
        length: 3,
        colour: '#ccc',
        snap: true,
    },
    zoom: {
        controls: false,
        wheel: true,
        startScale: 1.0,
        maxScale: 3,
        minScale: 0.3,
        scaleSpeed: 1.2,
    },
    trashcan: false,
    move: {
        scrollbars: true,
        drag: true,
        wheel: false,
    },
    sounds: false,
    plugins: {
        connectionChecker: 'pxt',
    },
};

export function injectWorkspace(element: HTMLElement, optionsJson?: string, toolboxJson?: string): Blockly.WorkspaceSvg {
    const extra = optionsJson ? JSON.parse(optionsJson) as Blockly.BlocklyOptions : {};
    // toolbox должен существовать с момента inject, иначе updateToolbox позже не сработает.
    const toolbox = toolboxJson
        ? JSON.parse(toolboxJson)
        : { kind: 'categoryToolbox', contents: [] };
    const workspace = Blockly.inject(element, { ...defaultOptions, toolbox, ...extra });

    // Нативное меню категорий заменено Blazor-рейкой и скрыто CSS-ом из pxblocks.css
    // (хост подключает его link-ом в head); здесь прячем inline и пересчитываем метрики
    // на случай, если CSS ещё не применился, — иначе полотно останется
    // «ширина минус toolbox» до первого ресайза окна.
    const nativeToolbox = workspace.getToolbox() as Blockly.Toolbox | null;
    if (nativeToolbox?.HtmlDiv) {
        nativeToolbox.HtmlDiv.style.display = 'none';
    }
    workspace.resize();

    // «Переменные» и «Функции» — свои flyout-ы: блоки переменных ядра названы
    // core.variables.* (определения отдаёт сервер), а штатная категория Blockly
    // хардкодит variables_get/variables_set; в процедуры добавлен наш досрочный
    // return (procedures_return), которого в штатном наборе Blockly нет.
    workspace.registerToolboxCategoryCallback('VARIABLE', variablesFlyout);
    workspace.registerButtonCallback('CREATE_VARIABLE', (button) => {
        Blockly.Variables.createVariableButtonHandler(button.getTargetWorkspace());
    });
    workspace.registerToolboxCategoryCallback('PROCEDURE', proceduresFlyout);

    return workspace;
}

// Flyout категории «Переменные»: кнопка создания + get/set для каждой переменной.
// Blockly 13 ждёт JSON-массив (FlyoutDefinition); состояние поля переменной —
// {name, type}, как в штатном flyout (blockly/core/variables).
function variablesFlyout(workspace: Blockly.WorkspaceSvg): Blockly.utils.toolbox.FlyoutItemInfo[] {
    const items: Blockly.utils.toolbox.FlyoutItemInfo[] = [
        { kind: 'button', text: 'Create a variable', callbackkey: 'CREATE_VARIABLE' },
    ];

    for (const variable of workspace.getVariableMap().getVariablesOfType('')) {
        const fields = { VAR: { name: variable.getName(), type: variable.getType() } };
        items.push({ kind: 'block', type: 'core.variables.get', fields });
        items.push({ kind: 'block', type: 'core.variables.set', fields });
        items.push({ kind: 'block', type: 'core.variables.change', fields });
    }

    return items;
}

// Flyout категории «Функции»: повторяет штатный Blockly (def/defreturn/ifreturn
// + call-блоки для каждой определённой функции) и добавляет наш досрочный
// procedures_return со shadow-«пусто» в сокете значения.
function proceduresFlyout(workspace: Blockly.WorkspaceSvg): Blockly.utils.toolbox.FlyoutItemInfo[] {
    const items: Blockly.utils.toolbox.FlyoutItemInfo[] = [];

    if (Blockly.Blocks['procedures_defnoreturn']) {
        items.push({
            kind: 'block', type: 'procedures_defnoreturn', gap: 16,
            fields: { NAME: Blockly.Msg['PROCEDURES_DEFNORETURN_PROCEDURE'] },
        });
    }
    if (Blockly.Blocks['procedures_defreturn']) {
        items.push({
            kind: 'block', type: 'procedures_defreturn', gap: 16,
            fields: { NAME: Blockly.Msg['PROCEDURES_DEFRETURN_PROCEDURE'] },
        });
    }
    if (Blockly.Blocks['procedures_ifreturn']) {
        items.push({ kind: 'block', type: 'procedures_ifreturn', gap: 16 });
    }
    if (Blockly.Blocks['procedures_return']) {
        items.push({
            kind: 'block', type: 'procedures_return', gap: 24,
            inputs: { VALUE: { shadow: { type: 'core.logic.null' } } },
        });
    }
    if (items.length > 0) {
        (items[items.length - 1] as Blockly.utils.toolbox.BlockInfo).gap = 24;
    }

    const [noReturn, withReturn] = Blockly.Procedures.allProcedures(workspace);
    for (const [name, params] of noReturn) {
        items.push({ kind: 'block', type: 'procedures_callnoreturn', gap: 16, extraState: { name, params } });
    }
    for (const [name, params] of withReturn) {
        items.push({ kind: 'block', type: 'procedures_callreturn', gap: 16, extraState: { name, params } });
    }

    return items;
}

// Меню категорий скрыто CSS-ом (рейка в Blazor); flyout открываем программным выбором.
export function selectCategory(workspace: Blockly.WorkspaceSvg, name: string): boolean {
    const toolbox = workspace.getToolbox() as Blockly.Toolbox | null;
    if (!toolbox) return false;
    const item = toolbox.getToolboxItems().find((it) => {
        const category = it as Blockly.ToolboxCategory;
        return typeof category.getName === 'function' && category.getName() === name;
    });
    if (!item) return false;
    if (toolbox.getSelectedItem() === item) {
        // Категория уже выбрана, но flyout мог быть закрыт (drag-out, клик мимо):
        // повторный setSelectedItem — no-op, поэтому сбрасываем и выбираем заново.
        toolbox.clearSelection();
    }
    toolbox.setSelectedItem(item);
    return true;
}

export function isFlyoutVisible(workspace: Blockly.WorkspaceSvg): boolean {
    const toolbox = workspace.getToolbox() as Blockly.Toolbox | null;
    return toolbox?.getFlyout()?.isVisible() ?? false;
}

export function clearToolboxSelection(workspace: Blockly.WorkspaceSvg): void {
    (workspace.getToolbox() as Blockly.Toolbox | null)?.clearSelection();
}

export function updateToolbox(workspace: Blockly.WorkspaceSvg, toolboxJson: string): void {
    workspace.updateToolbox(JSON.parse(toolboxJson));
}

export function registerBlockDefinitions(definitionsJson: string): void {
    Blockly.common.defineBlocksWithJsonArray(JSON.parse(definitionsJson));
}

export function saveWorkspace(workspace: Blockly.WorkspaceSvg): string {
    return JSON.stringify(Blockly.serialization.workspaces.save(workspace));
}

export function loadWorkspace(workspace: Blockly.WorkspaceSvg, blocksJson: string): void {
    const state = JSON.parse(blocksJson);
    ensureUnknownBlockDefinitions(state);
    Blockly.serialization.workspaces.load(state, workspace);
}

// «Unknown»: определение блока удалено на сервере, но блок остался в сохранённом
// workspace. Регистрируем серый placeholder, чтобы полотно показало его, а не
// упало при загрузке; запуск всё равно блокируется серверным разбором.
function ensureUnknownBlockDefinitions(state: unknown): void {
    const rootBlocks = (state as { blocks?: { blocks?: unknown[] } })?.blocks?.blocks;
    if (!Array.isArray(rootBlocks)) return;
    for (const block of rootBlocks) {
        walkUnknownBlock(block as UnknownBlockShape, 'statement');
    }
}

interface UnknownBlockShape {
    type?: string;
    inputs?: Record<string, { block?: UnknownBlockShape; shadow?: UnknownBlockShape }>;
    next?: { block?: UnknownBlockShape };
}

function walkUnknownBlock(block: UnknownBlockShape, kind: 'statement' | 'value'): void {
    if (!block || typeof block.type !== 'string') return;

    if (!(block.type in Blockly.Blocks)) {
        registerUnknownPlaceholder(block.type, kind);
    }
    if (block.inputs) {
        for (const slot of Object.values(block.inputs)) {
            if (slot?.block) walkUnknownBlock(slot.block, 'value');
            if (slot?.shadow) walkUnknownBlock(slot.shadow, 'value');
        }
    }
    if (block.next?.block) {
        walkUnknownBlock(block.next.block, 'statement');
    }
}

function registerUnknownPlaceholder(type: string, kind: 'statement' | 'value'): void {
    Blockly.Blocks[type] = {
        init(this: Blockly.Block) {
            this.appendDummyInput().appendField(`Unknown: ${type}`);
            if (kind === 'value') {
                this.setOutput(true);
            } else {
                this.setPreviousStatement(true);
                this.setNextStatement(true);
            }
            this.setColour('#9E9E9E');
            this.setTooltip('Block definition was removed on the server — run is not possible');
        },
    };
}

export function clearWorkspace(workspace: Blockly.WorkspaceSvg): void {
    workspace.clear();
}

export function undo(workspace: Blockly.WorkspaceSvg, redo: boolean): void {
    workspace.undo(redo);
}

// Центрируем содержимое в видимой области без изменения масштаба.
export function centerContent(workspace: Blockly.WorkspaceSvg): void {
    workspace.scrollCenter();
}

// Подсветка исполняемого блока (события PxInterpreter → interop).
export function setBlockHighlight(workspace: Blockly.WorkspaceSvg, id: string, on: boolean): void {
    workspace.getBlockById(id)?.setHighlighted(on);
}

interface DotNetRef {
    invokeMethodAsync<T>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

// События Blockly → .NET: пакетируем с дебаунсом, UI-события (клики, выделение) не шумят.
export function registerEvents(workspace: Blockly.WorkspaceSvg, dotNetRef: DotNetRef): void {
    let batch: Record<string, unknown>[] = [];
    let timer: number | undefined;

    workspace.addChangeListener((event: Blockly.Events.Abstract) => {
        // Синхронизация рейки: Blockly сам закрывает flyout и сбрасывает выбор
        // (drag-out, клик мимо) — сообщаем .NET актуальное имя выбранной категории.
        if (event.type === Blockly.Events.TOOLBOX_ITEM_SELECT) {
            const select = event as Blockly.Events.ToolboxItemSelect;
            void dotNetRef.invokeMethodAsync<void>('OnToolboxSelect', toolboxItemName(workspace, select.newItem ?? null));
        }

        if (event.isUiEvent) {
            return;
        }
        batch.push(serializeEvent(event));
        if (timer === undefined) {
            timer = window.setTimeout(() => {
                timer = undefined;
                const events = batch;
                batch = [];
                void dotNetRef.invokeMethodAsync<void>('OnBlocklyEvents', JSON.stringify(events));
            }, 200);
        }
    });
}

function toolboxItemName(workspace: Blockly.WorkspaceSvg, id: string | null): string | null {
    if (!id) return null;
    const toolbox = workspace.getToolbox() as Blockly.Toolbox | null;
    const item = toolbox?.getToolboxItemById(id) as Blockly.ToolboxCategory | null;
    return item && typeof item.getName === 'function' ? item.getName() : id;
}

function serializeEvent(event: Blockly.Events.Abstract): Record<string, unknown> {
    const e = event as unknown as { blockId?: string; ids?: string[] };
    const result: Record<string, unknown> = { type: event.type };
    if (e.blockId !== undefined) {
        result.blockId = e.blockId;
    }
    if (e.ids !== undefined) {
        result.ids = e.ids;
    }
    return result;
}

export function disposeWorkspace(workspace: Blockly.WorkspaceSvg): void {
    workspace.dispose();
}

export function getVersion(): string {
    return Blockly.VERSION;
}
