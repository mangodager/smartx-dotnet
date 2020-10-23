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
        ComponentNetworkInner relayNetworkInner = null;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>(); // 一级网络
            componentNetMsg.registerMsg(NetOpcode.P2P_NewBlock, P2P_NewBlock_Handle);

        }

        public override void Start()
        {
            ComponentNetMsg componentNetMsg = this.entity.GetComponent<ComponentNetMsg>();// 中继网络
            componentNetMsg.registerMsg(NetOpcode.Q2P_New_Node, Q2P_New_Node_Handle);

            relayNetworkInner = this.entity.GetComponent<ComponentNetworkInner>();
            Log.Info($"RelayNetwork.Start {relayNetworkInner.ipEndPoint}");
        }

        void P2P_NewBlock_Handle(Session session, int opcode, object msg)
        {
            P2P_NewBlock p2p_Block = msg as P2P_NewBlock;

            //var newBlock = new P2P_NewBlock() { block = p2p_Block.block, ipEndPoint = p2p_Block.ipEndPoint };
            var newBlock = new P2P_NewBlock() { block = p2p_Block.block, ipEndPoint = Entity.Root.GetComponent<ComponentNetworkInner>().ipEndPoint.ToString() };

            relayNetworkInner.BroadcastToAll(newBlock);
        }

        void Q2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            Q2P_New_Node new_Node = msg as Q2P_New_Node;
            //Log.Debug($"Q2P_New_Nod {new_Node.ActorId} \r\nHash: {new_Node.HashCode}");

            R2P_New_Node response = new R2P_New_Node() { Nodes = "", sendTime = new_Node.sendTime, nodeTime = TimeHelper.Now() };
            session.Reply(new_Node, response);
        }



    }


}
