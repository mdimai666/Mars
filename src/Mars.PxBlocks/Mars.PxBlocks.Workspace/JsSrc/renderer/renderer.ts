// Ported from microsoft/pxt (MIT): pxtblocks/plugins/renderer/renderer.ts
import * as Blockly from "blockly";
import { PathObject } from "./pathObject";
import { ConstantProvider } from "./constants";
import { RenderInfo } from "./info";
import { Drawer } from "./drawer";

// blockly не экспортирует BlockStyle публично — берём тип из сигнатуры базового рендерера.
type BlockStyle = Parameters<Blockly.zelos.Renderer['makePathObject']>[1];

export interface UpdateBeforeRenderMixin {
    updateBeforeRender(): void;
}

type UpdateBeforeRenderBlock = UpdateBeforeRenderMixin & Blockly.BlockSvg;

export class Renderer extends Blockly.zelos.Renderer {
    override makePathObject(root: SVGElement, style: BlockStyle): PathObject {
        return new PathObject(root, style, this.getConstants() as ConstantProvider);
    }

    protected override makeConstants_(): ConstantProvider {
        return new ConstantProvider();
    }

    protected override makeRenderInfo_(block: Blockly.BlockSvg): RenderInfo {
        return new RenderInfo(this, block);
    }

    protected override makeDrawer_(
        block: Blockly.BlockSvg,
        info: Blockly.blockRendering.RenderInfo,
    ): Drawer {
        return new Drawer(block, info as RenderInfo);
    }

    render(block: Blockly.BlockSvg): void {
        if ((block as UpdateBeforeRenderBlock).updateBeforeRender) {
            (block as UpdateBeforeRenderBlock).updateBeforeRender();
        }
        super.render(block);
    }
}

Blockly.blockRendering.register("pxt", Renderer);
