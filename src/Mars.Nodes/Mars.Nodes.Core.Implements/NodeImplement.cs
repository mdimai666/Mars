namespace Mars.Nodes.Core.Implements;

public interface INodeImplement : INodeImplement<Node>
{

}

public interface INodeImplement<TNode> where TNode : Node
{
    public string Id => Node.Id;

    public TNode Node { get; }
    public IRED RED { get; set; }

    //public INodeImplement<TNode> Create(TNode node);
    public Task Execute(NodeMsg input, ExecuteAction callback);


}

public delegate void ExecuteAction(NodeMsg msg, int output = 0);

//public abstract class NodeImplement<TNode>: INodeImplement<TNode> where TNode : Node
//{
//    public TNode Node => node;

//    public string Id => Node.Id;

//    public IRED RED { get; set; }
//    protected TNode node;

//    //public virtual INodeImplement<TNode> Create(TNode node)
//    //{
//    //    this.node = node;
//    //    return (INodeImplement<TNode>)this;
//    //}

//    public NodeImplement(TNode node)
//    {
//        this.node = Node;
//    }

//  public Task Execute(NodeMsg input, ExecuteAction callback)

//#if asas

//    public virtual async Task Execute(InputMsg input, Action<object> callback, Action<Exception> Error, bool ErrorTolerance)
//    {

//        try
//        {
//            User user = input.Get<User>();

//            Applica app = input.Get("app") as Applica;




//            int age = 21;

//            input.Add("age", age);

//            int getAge = (int)input.Get("age");


//            if (user is not null)
//            {

//            }



//            input.Payload = user;

//            if (!ErrorTolerance) callback.Invoke(input);
//        }
//        catch (Exception e)
//        {
//            Error(e);
//        }
//        finally
//        {
//            if (ErrorTolerance)
//                callback.Invoke(input);
//        }

//    } 
//#endif
//}
