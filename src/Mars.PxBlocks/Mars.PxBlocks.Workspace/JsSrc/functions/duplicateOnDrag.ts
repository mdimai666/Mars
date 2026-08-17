import * as Blockly from 'blockly/core';
import { ARGUMENT_REPORTER } from './constants';

// Порт plugins/duplicateOnDrag: shadow-репортёры параметров при вытаскивании из
// сигнатуры функции дублируются (в тело уезжает копия, в сокете остаётся shadow).

function isReporter(block: Blockly.Block): boolean {
    return block.type.startsWith(ARGUMENT_REPORTER);
}

interface DragStrategyInternals {
    block: Blockly.BlockSvg;
    startChildConn: Blockly.Connection | null;
}

// @ts-expect-error переопределяем protected-метод базовой стратегии
class DuplicateOnDragStrategy extends Blockly.dragging.BlockDragStrategy {
    protected getTargetBlock(): Blockly.BlockSvg {
        const self = this as unknown as DragStrategyInternals;
        // Штатное поведение делегирует drag родителю; для наших shadow-репортёров
        // тащим сам shadow, чтобы disconnectBlock мог его отсоединить и склонировать.
        if (self.block.isShadow() && isReporter(self.block)) {
            return self.block;
        }
        return super.getTargetBlock();
    }

    override drag(newLoc: Blockly.utils.Coordinate, e?: PointerEvent | KeyboardEvent): void {
        super.drag(newLoc, e);
        if (!e || e instanceof PointerEvent) {
            const self = this as unknown as DragStrategyInternals;
            self.block.moveDuringDrag(newLoc);
        }
    }

    private disconnectBlock(healStack: boolean): void {
        const self = this as unknown as DragStrategyInternals;
        const isShadow = self.block.isShadow();

        if (isShadow) self.block.setShadow(false);

        let clone: Blockly.Block | null = null;
        let target: Blockly.Connection | null = null;
        let xml: Element | null = null;
        if (isReporter(self.block) && self.block.outputConnection?.targetConnection) {
            xml = Blockly.Xml.blockToDom(self.block, true) as Element;
            if (!isShadow) clone = Blockly.Xml.domToBlock(xml, self.block.workspace);
            target = self.block.outputConnection.targetConnection;
        }

        if (healStack) self.startChildConn = self.block.nextConnection?.targetConnection ?? null;

        if (target && isShadow && xml) target.setShadowDom(xml);
        self.block.unplug(healStack);
        Blockly.blockAnimations.disconnectUiEffect(self.block);

        if (target && clone) target.connect(clone.outputConnection as Blockly.Connection);
    }
}

export function setDuplicateOnDragStrategy(block: Blockly.Block): void {
    (block as Blockly.BlockSvg).setDragStrategy?.(new DuplicateOnDragStrategy(block as Blockly.BlockSvg));
}
