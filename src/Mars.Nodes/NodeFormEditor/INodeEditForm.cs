namespace NodeFormEditor;

public interface INodeEditForm
{
    //void OnEditPrepare();
    Task OnEditSave();
    Task OnEditCancel();
    Task OnEditDelete();
    //void OnEditResize();
    //void OnPaletteAdd();
    //void OnPaletteRemove();
}
