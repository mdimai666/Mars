// Ported from microsoft/pxt (MIT): pxtblocks/plugins/renderer/pathObject.ts
// pxt.contrastRatio replaced with a local WCAG implementation.
import * as Blockly from "blockly";
import { ConstantProvider } from "./constants";

const DOTTED_OUTLINE_HOVER_CLASS = "blockly-dotted-outline-on-hover"
const HOVER_CLASS = "hover"

export class PathObject extends Blockly.zelos.PathObject {
    static CONNECTION_INDICATOR_RADIUS = 9;

    protected svgPathHighlighted: SVGElement | null = null;
    protected hasError = false;

    protected hasDottedOutlineOnHover = false;

    protected mouseOverData?: Blockly.browserEvents.Data;
    protected mouseLeaveData?: Blockly.browserEvents.Data;

    protected connectionPointIndicators = new WeakMap<Blockly.RenderedConnection, SVGElement>();
    staticConnectionIndicatorParentGroup: any;

    override setPath(pathString: string): void {
        super.setPath(pathString);
        if (this.svgPathHighlighted) {
            this.svgPathHighlighted.setAttribute('d', pathString);
        }
    }


    override updateHighlighted(enable: boolean) {
        if (enable) {
            if (!this.svgPathHighlighted) {
                const constants = this.constants as ConstantProvider;
                const filterId = this.hasError ? constants.errorOutlineFilterId : constants.highlightOutlineFilterId;
                this.svgPathHighlighted = this.svgPath.cloneNode(true) as SVGElement;
                this.svgPathHighlighted.classList.add('pxtRendererHighlight');
                this.svgPathHighlighted.setAttribute('fill', 'none');
                this.svgPathHighlighted.setAttribute(
                    'filter',
                    'url(#' + filterId + ')',
                );
                this.svgRoot.appendChild(this.svgPathHighlighted);
            }
        } else {
            if (this.svgPathHighlighted) {
                this.svgRoot.removeChild(this.svgPathHighlighted);
                this.svgPathHighlighted = null;
            }
        }
    }

    override updateSelected(enable: boolean): void {
        if (enable) {
            this.svgPath.classList.remove(HOVER_CLASS);
        }
        super.updateSelected(enable);
    }

    override addConnectionHighlight(connection: Blockly.RenderedConnection, connectionPath: string, offset: Blockly.utils.Coordinate, rtl: boolean): SVGElement {
        const result = super.addConnectionHighlight(connection, connectionPath, offset, rtl);

        // We add a group that our ConnectionPreviewer uses to add the connection preview indicators.
        // We create it here to manage the paint order.
        if (!this.staticConnectionIndicatorParentGroup) {
            this.staticConnectionIndicatorParentGroup = Blockly.utils.dom.createSvgElement("g", {
                class: "blocklyConnectionIndicatorParent"
            }, this.svgRoot);
        } else {
            // Move last in paint order.
            this.svgRoot.appendChild(this.staticConnectionIndicatorParentGroup);
        }

        return result;
    }

    override removeConnectionHighlight(connection: Blockly.RenderedConnection): void {
        this.staticConnectionIndicatorParentGroup?.remove();

        super.removeConnectionHighlight(connection);
    }

    // applyColour не переопределяем: контур вычисляет сам Zelos —
    // colourTertiary (blend('#000', primary, 0.25)), у shadow — tertiary родителя.
    // PXT-хаки (blend 0.6 к чёрному/белому, высветление shadow) на нашей палитре
    // давали «чёрную ручку» и светлое «гало» вместо тонального контура.

    setHasDottedOutlineOnHover(enabled: boolean) {
        this.hasDottedOutlineOnHover = enabled;

        if (enabled) {
            this.svgPath.classList.add(DOTTED_OUTLINE_HOVER_CLASS);
            if (!this.mouseOverData) {
                this.mouseOverData = Blockly.browserEvents.bind(
                    this.svgRoot,
                    "mouseover",
                    this,
                    () => {
                        this.svgPath.classList.add(HOVER_CLASS);
                    }
                );
                this.mouseLeaveData = Blockly.browserEvents.bind(
                    this.svgRoot,
                    "mouseleave",
                    this,
                    () => {
                        this.svgPath.classList.remove(HOVER_CLASS);
                    }
                );
            }
        }
        else {
            this.svgPath.classList.remove(DOTTED_OUTLINE_HOVER_CLASS);
            if (this.mouseOverData) {
                Blockly.browserEvents.unbind(this.mouseOverData);
                Blockly.browserEvents.unbind(this.mouseLeaveData!);

                this.mouseOverData = undefined;
                this.mouseLeaveData = undefined;
            }
            this.svgPath.classList.remove(DOTTED_OUTLINE_HOVER_CLASS);
        }
    }

    setHasError(hasError: boolean) {
        this.hasError = hasError;
    }

    isHighlighted() {
        return !!this.svgPathHighlighted;
    }
}

Blockly.Css.register(`
.blockly-dotted-outline-on-hover {
    transition: stroke .4s;
}
.blockly-dotted-outline-on-hover.hover {
    stroke-dasharray: 2;
    stroke: white;
    stroke-width: 2;
}
.blocklyDisabledPattern>.blocklyPath.pxtRendererHighlight {
    fill: none;
}
`)
