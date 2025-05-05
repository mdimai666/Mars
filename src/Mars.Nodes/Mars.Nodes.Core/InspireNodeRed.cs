namespace Mars.Nodes.Core;

public interface INodeImplementatione
{
    //public Task<NodeExecuteResult<string>> Execute(NodeMessage msg);

    //
    public void OnDeployed(object context);
    public void OnEditorShow();

    //
    public void OnInput(NodeMessage msg, Action<NodeMessage> send, Action done);
    public void OnSend(NodeMessage msg);//[msg,null]
    public void OnLog(object msg, int level);
    public void OnClose(Action done, string state_removed_restarted);

    public void ChangeStatus(string color, string shape__ring_dot, string text);

    //workspace visualize
    void OnEditPrepare();
    void OnEditSave();
    void OnEditCancel();
    void OnEditDelete();
    void OnEditResize();
    void OnPaletteAdd();
    void OnPaletteRemove();

}

public class NodeMessage : NodeMessage<string>
{

}

public class NodeMessage<T>
{
    public T Payload { get; set; } = default(T)!;
    public string _msgId { get; set; } = "";
    public string Topic { get; set; } = "";

}

public class NodeExecuteResult<T>
{
    public bool Success { get; set; }
    public bool ErrorMessage { get; set; }
    public T Data { get; set; } = default!;
}


public class NodeEditProperty
{
    public string Value { get; set; } = "";
    public bool Required { get; set; }
    public Func<bool>? Validate { get; set; }
    public string type { get; set; } = "";
}


