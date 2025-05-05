using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Host.Shared.Hubs;

public class ChatHub : Hub
{
    private readonly INodeService nodeService;
    private readonly IServiceScopeFactory factory;
    private readonly IServiceProvider serviceProvider;

    //public static ChatHub instance = null;//костыль, потом убрать



    public ChatHub(INodeService nodeService, IServiceScopeFactory factory, IServiceProvider serviceProvider)
    {
        //instance = this;
        //Console.WriteLine("ChatHub::ctor");
        this.nodeService = nodeService;
        this.factory = factory;
        this.serviceProvider = serviceProvider;
    }

    /*public static void Initialize(NodeService nodeService, IServiceScopeFactory factory)
    {
        if ( instance == null)
            instance = new ChatHub(nodeService, factory);
    }*/


    public async Task SendMessage(string user, string message)
    {

        Console.WriteLine($">SendMessage= {user}; message={message}");
        await Clients.Others.SendAsync("ReceiveMessage", user, message);

    }

    public async void Inject(string nodeId)
    {
        //await nodeService.Inject(factory, nodeId);
        await nodeService.Inject(serviceProvider, nodeId);
    }

    public async void DebugMsg(string nodeId, DebugMessage msg)
    {
        //this.Clients.All.DebugMsg(nodeId, msg);
        await Clients.All.SendAsync("DebugMsg", nodeId, msg);
        //Clients.All.BroadcastMessage(name, message);
    }

    public async void NodeStatus(string nodeId, NodeStatus nodeStatus)
    {
        //this.Clients.All.NodeStatus(nodeId, nodeStatus);
        await Clients.All.SendAsync("NodeStatus", nodeId, nodeStatus);
    }
}
/*
public interface IChatHub
{
    public void DebugMsg(string nodeId, DebugMessage msg);
    public void NodeStatus(string nodeId, NodeStatus nodeStatus);


}
*/
//public interface IChatHub
//{
//    void SendMessage(string message);
//    void Inject(string nodeId);
//}
