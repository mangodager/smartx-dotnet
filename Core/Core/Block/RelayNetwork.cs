using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace ETModel
{
    public class RelayNetwork : Component
    {
        NodeManager nodeManager = Entity.Root.GetComponent<NodeManager>();
        ComponentNetworkInner relayNetworkInner = null;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>(); // 一级网络
            componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);

        }

        public override void Start()
        {
            var relayNetworkNetMsg = this.entity.GetComponent<ComponentNetMsg>();// 中继网络
            relayNetworkNetMsg.registerMsg(NetOpcode.Q2P_New_Node, Q2P_New_Node_Handle);
            relayNetworkNetMsg.registerMsg(NetOpcode.Q2P_Transfer, Q2P_Transfer_Handle);

            relayNetworkInner = this.entity.GetComponent<ComponentNetworkInner>();
            Log.Info($"RelayNetwork.Start {relayNetworkInner.ipEndPoint}");
        }

        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;
            if (p2p_Block.networkID != BlockMgr.networkID)
                return;

            //var newBlock = new P2P_NewBlock() { block = p2p_Block.block, networkID = BlockMgr.networkID,ipEndPoint = p2p_Block.ipEndPoint };
            var newBlock = new P2P_NewBlock() { block = p2p_Block.block, networkID = BlockMgr.networkID, ipEndPoint = Entity.Root.GetComponent<ComponentNetworkInner>().ipEndPoint.ToString() };

            relayNetworkInner.BroadcastToAll(newBlock);
        }

        void Q2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            Q2P_New_Node new_Node = msg as Q2P_New_Node;
            //Log.Debug($"Q2P_New_Nod {new_Node.ActorId} \r\nHash: {new_Node.HashCode}");
            if (nodeManager != null)
            {
                //Internet
                var nodes = nodeManager.GetNodeList();
                nodes = nodes.FindAll((n) => { return (n.state & NodeManager.EnumState.RelayNetwork) == NodeManager.EnumState.RelayNetwork; });

                //// LAN
                //var node = new NodeManager.NodeData();
                //node.address = Wallet.GetWallet().GetCurWallet().ToAddress();
                //node.nodeId = nodeManager.GetMyNodeId();
                //node.ipEndPoint = $"{relayNetworkInner.ipEndPoint.Address}:{Entity.Root.GetComponent<ComponentNetworkInner>().ipEndPoint.Port}";
                //node.state = 7;
                //var nodes = new List<NodeManager.NodeData>();
                //nodes.Add(node);

                // 
                R2P_New_Node response = new R2P_New_Node() { Nodes = JsonHelper.ToJson(nodes), nodeTime = TimeHelper.Now() };
                session.Reply(new_Node, response);
            }
        }

        public static void Q2P_Transfer_Handle(Session session, int opcode, object msg)
        {
            Q2P_Transfer q2p_Transfer = msg as Q2P_Transfer;
            BlockSub transfer = JsonHelper.FromJson<BlockSub>(q2p_Transfer.transfer);

            R2P_Transfer r2p_Transfer = new R2P_Transfer() { rel = "-10000" };
            if (transfer.CheckSign())
            {
                var rel = Entity.Root.GetComponent<Rule>().AddTransfer(transfer);
                if (rel == -1)
                {
                    HttpRpc.OnTransferAsync(transfer);
                }
            }
            session.Reply(q2p_Transfer, r2p_Transfer);
        }


    }


}
