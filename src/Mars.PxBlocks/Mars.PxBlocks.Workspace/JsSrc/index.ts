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
        controls: true,
        wheel: true,
        startScale: 1.0,
        maxScale: 3,
        minScale: 0.3,
        scaleSpeed: 1.2,
    },
    trashcan: true,
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
    return Blockly.inject(element, { ...defaultOptions, toolbox, ...extra });
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

interface DotNetRef {
    invokeMethodAsync<T>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

// События Blockly → .NET: пакетируем с дебаунсом, UI-события (клики, выделение) не шумят.
export function registerEvents(workspace: Blockly.WorkspaceSvg, dotNetRef: DotNetRef): void {
    let batch: Record<string, unknown>[] = [];
    let timer: number | undefined;

    workspace.addChangeListener((event: Blockly.Events.Abstract) => {
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
