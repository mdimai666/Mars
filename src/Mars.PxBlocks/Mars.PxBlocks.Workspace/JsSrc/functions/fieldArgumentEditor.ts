import * as Blockly from 'blockly/core';

// Поле имени параметра в диалоге редактирования функции (порт PXT
// pxtblocks/plugins/functions/fields/fieldArgumentEditor.ts): клик по полю
// открывает виджет с иконками — стрелки перемещают параметр в сигнатуре,
// корзинка удаляет. В PXT была только корзинка; стрелки — наше дополнение.

const REMOVE_ARG_URI =
    "data:image/svg+xml;charset=UTF-8,%3c?xml version='1.0' encoding='UTF-8' standalone='no'?%3e%3csvg width='20px' height='20px' viewBox='0 0 20 20' version='1.1' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'%3e%3c!-- Generator: Sketch 48.1 (47250) - http://www.bohemiancoding.com/sketch --%3e%3ctitle%3edelete-argument v2%3c/title%3e%3cdesc%3eCreated with Sketch.%3c/desc%3e%3cdefs%3e%3c/defs%3e%3cg id='Page-1' stroke='none' stroke-width='1' fill='none' fill-rule='evenodd'%3e%3cg id='delete-argument-v2' stroke='%23FF661A'%3e%3cg id='Group' transform='translate(3.000000, 2.500000)'%3e%3cpath d='M1,3 L13,3 L11.8900496,14.0995037 C11.8389294,14.6107055 11.4087639,15 10.8950124,15 L3.10498756,15 C2.59123611,15 2.16107055,14.6107055 2.10995037,14.0995037 L1,3 Z' id='Rectangle' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'%3e%3c/path%3e%3cpath d='M7,11 L7,6' id='Line' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'%3e%3c/path%3e%3cpath d='M9.5,11 L9.5,6' id='Line-Copy' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'%3e%3c/path%3e%3cpath d='M4.5,11 L4.5,6' id='Line-Copy-2' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'%3e%3c/path%3e%3crect id='Rectangle-2' fill='%23FF661A' x='0' y='2.5' width='14' height='1' rx='0.5'%3e%3c/rect%3e%3cpath d='M6,0 L8,0 C8.55228475,-1.01453063e-16 9,0.44771525 9,1 L9,3 L5,3 L5,1 C5,0.44771525 5.44771525,1.01453063e-16 6,0 Z' id='Rectangle-3' stroke-width='1.5'%3e%3c/path%3e%3c/g%3e%3c/g%3e%3c/g%3e%3c/svg%3e";

const arrowUri = (path: string) =>
    'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(
        `<svg width="20px" height="20px" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="${path}" stroke="#FF661A" stroke-width="2.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>`);
const MOVE_UP_URI = arrowUri('M4 12.5 L10 6.5 L16 12.5');
const MOVE_DOWN_URI = arrowUri('M4 7.5 L10 13.5 L16 7.5');

type BindData = ReturnType<typeof Blockly.browserEvents.conditionalBind>;

interface SignatureParent {
    updateFunctionSignature?: () => void;
}

export class FieldArgumentEditor extends Blockly.FieldTextInput {
    private argButtons_: Array<{ data: BindData; el: HTMLImageElement }> = [];

    override showEditor(e?: Event): void {
        super.showEditor(e);
        const div = Blockly.WidgetDiv.getDiv();
        if (!div) return;
        div.classList.add('argumentEditorInput');
        const add = (cls: string, src: string, fn: () => void) => {
            const el = document.createElement('img');
            el.className = cls;
            el.src = src;
            const data = Blockly.browserEvents.conditionalBind(el, 'mousedown', this, fn);
            this.argButtons_.push({ data, el });
            div.appendChild(el);
        };
        add('argumentEditorMoveUpIcon', MOVE_UP_URI, () => this.moveArg_(-1));
        add('argumentEditorRemoveIcon', REMOVE_ARG_URI, () => this.removeArg_());
        add('argumentEditorMoveDownIcon', MOVE_DOWN_URI, () => this.moveArg_(1));
    }

    protected override widgetDispose_(): void {
        for (const b of this.argButtons_) {
            Blockly.browserEvents.unbind(b.data);
            b.el.remove();
        }
        this.argButtons_ = [];
        super.widgetDispose_();
    }

    private editorInputName_(): string | null {
        const block = this.sourceBlock_;
        const parent = block?.getParent();
        if (!block || !parent) return null;
        for (const input of parent.inputList) {
            if (input.connection?.targetBlock() === block) return input.name;
        }
        return null;
    }

    private refreshSignature_(parent: Blockly.Block): void {
        const fn = (parent as unknown as SignatureParent).updateFunctionSignature;
        if (typeof fn === 'function') fn.call(parent);
    }

    protected removeArg_(): void {
        const parent = this.sourceBlock_?.getParent();
        const inputName = this.editorInputName_();
        if (!parent || !inputName) return;
        Blockly.WidgetDiv.hide();
        parent.removeInput(inputName);
        this.refreshSignature_(parent);
    }

    protected moveArg_(dir: number): void {
        const block = this.sourceBlock_;
        const parent = block?.getParent();
        const inputName = this.editorInputName_();
        if (!block || !parent || !inputName) return;
        const values = parent.inputList.filter((i) => i.type === Blockly.inputs.inputTypes.VALUE);
        const idx = values.findIndex((i) => i.name === inputName);
        const neighbour = values[idx + dir];
        if (idx < 0 || !neighbour) return;
        Blockly.WidgetDiv.hide();
        if (dir < 0) {
            parent.moveInputBefore(inputName, neighbour.name);
        } else {
            const after = parent.inputList[parent.inputList.indexOf(neighbour) + 1];
            parent.moveInputBefore(inputName, after ? after.name : null);
        }
        this.refreshSignature_(parent);
    }
}

Blockly.fieldRegistry.register('field_argument_editor', FieldArgumentEditor);

Blockly.Css.register(`
.argumentEditorInput {
    overflow: visible;
}
.argumentEditorMoveUpIcon,
.argumentEditorRemoveIcon,
.argumentEditorMoveDownIcon {
    position: absolute;
    width: 24px;
    height: 24px;
    top: -40px;
    cursor: pointer;
}
.argumentEditorMoveUpIcon { left: 50%; margin-left: -42px; }
.argumentEditorRemoveIcon { left: 50%; margin-left: -12px; }
.argumentEditorMoveDownIcon { left: 50%; margin-left: 18px; }
`);
