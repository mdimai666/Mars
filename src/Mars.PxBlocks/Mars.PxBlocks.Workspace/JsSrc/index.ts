import * as Blockly from 'blockly/core';
import 'blockly/blocks';
import './renderer';
import './connectionChecker';
import './extensions/objectBuilder';

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

    return workspace;
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
    Blockly.serialization.workspaces.load(JSON.parse(blocksJson), workspace);
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
