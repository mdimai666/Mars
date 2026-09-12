import * as Blockly from 'blockly/core';

// Удаление блока броском в toolbox — как в MakeCode: рейка категорий (наш
// Blazor-компонент вместо нативного toolbox) регистрируется delete-областью, и
// Blockly сам удаляет блок при отпускании над ней (штатный механизм DRAG_TARGET +
// DELETE_AREA). Призрачный вид блока во время переноса Blockly тоже делает сам:
// wouldDelete унаследован от Blockly.DeleteArea — удаляются только самостоятельные
// deletable-блоки.

/// Вес как у штатного трэшкана — порядок обхода drag-target'ов.
const RAIL_WEIGHT = 2;

const DROP_CLASS = 'pxb-rail-drop';

class RailDeleteArea extends Blockly.DeleteArea {
    constructor(private readonly rail: HTMLElement) {
        super();
    }

    // contains() вызывается с координатами указателя (clientX/clientY) — значит
    // прямоугольник нужен в координатах вьюпорта, а не контейнера Blockly.
    getClientRect() {
        const box = this.rail.getBoundingClientRect();
        if (box.width === 0 || box.height === 0)
            return null;
        return new Blockly.utils.Rect(box.top, box.bottom, box.left, box.right);
    }

    onDragEnter(): void {
        this.rail.classList.add(DROP_CLASS);
    }

    onDragExit(): void {
        this.rail.classList.remove(DROP_CLASS);
    }

    onDrop(): void {
        this.rail.classList.remove(DROP_CLASS);
    }
}

/// Зарегистрировать рейку как delete-область (container — контейнер Blockly).
export function registerRailDeleteArea(workspace: Blockly.WorkspaceSvg, container: HTMLElement): void {
    // Рейка лежит не рядом с контейнером полотна, а в корне редактора (обёртка между ними).
    const rail = container.closest('.px-blocks-editor')?.querySelector<HTMLElement>('.pxb-rail') ?? null;
    if (!rail) {
        console.warn('PxBlocks: rail element not found — drag-to-toolbox delete is disabled');
        return;
    }

    rail.classList.remove(DROP_CLASS);
    workspace.getComponentManager().addComponent({
        component: new RailDeleteArea(rail),
        weight: RAIL_WEIGHT,
        capabilities: [
            Blockly.ComponentManager.Capability.DELETE_AREA,
            Blockly.ComponentManager.Capability.DRAG_TARGET,
        ],
    });
}
