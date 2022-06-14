using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace ETModel
{
    public class NodeManager : Component
    {
        Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
#if SmartX
        public static string networkIDCur  = "SmartX_4.2.0";
        public static string networkIDBase = "SmartX_4.2.0";
#else
        public static string networkIDCur  = "alpha_2.2.2";
        public static string networkIDBase = "alpha_2.2.2";
#endif
        public static bool CheckNetworkID(string id)
        {
            return id.IndexOf(networkIDBase) != -1;
        }

        static public int K_INDEX_ROW = 12; // height
        static public int K_INDEX_COL = 12; // width

        public class EnumState
        {
            public const long transferShow = 1;
            public const long openSyncFast = 2;
            public const long RelayNetwork = 4;
            public const long TransferComponent = 8;
        }

        public class NodeData
        {
            public long   nodeId;
            public string address;
            public string ipEndPoint;
            public int    kIndex; // K桶序号
            public long   state;
            public string version;
        }

        public static string GetIpV4()
        {
            try
            {
                using (System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 1337);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }

            }
            catch (Exception)
            {
            }

            return "";
        }

        // 节点队列
        List<NodeData> nodes = new List<NodeData>();
        ComponentNetworkInner networkInner = Entity.Root.GetComponent<ComponentNetworkInner>();
        public long nodeTimeOffset = 0;

        public override void Awake(JToken jd = null)
        {
            ComponentNetMsg componentNetMsg = Entity.Root.GetComponent<ComponentNetMsg>();
            //componentNetMsg.registerMsg(NetOpcode.A2M_HearBeat, A2M_HearBeat_Handle);
            componentNetMsg.registerMsg(NetOpcode.Q2P_New_Node, Q2P_New_Node_Handle);
            //componentNetMsg.registerMsg(NetOpcode.R2P_New_Node, R2P_New_Node_Handle);

            // 是否添加自身到Nodes列表
            bool bRun = true;
            if (jd["AddSelfToNodes"] != null)
            {
                bool.TryParse(jd["AddSelfToNodes"].ToString(), out bRun);
            }
            Run(bRun);
        }

        public long GetMyNodeId()
        {
            return StringHelper.HashCode(networkInner.ipEndPoint.ToString());
        }

        public long GetNodeTime()
        {
            return TimeHelper.Now() + nodeTimeOffset;
        }

        List<string> NodeSessions     = null;
        TimePass     NodeSessionsTime = new TimePass(60);
        List<string> GetNodeSessions()
        {
            if (NodeSessions == null || NodeSessionsTime.IsPassSet())
            {
                List<string> list = JsonHelper.FromJson<List<string>>(Program.jdNode["NodeSessions"].ToString());
                for (int ii = 0; ii < list.Count; ii++)
                {
                    list[ii] = NetworkHelper.DnsToIPEndPoint(list[ii]);
                }
                NodeSessions = list;
            }
            return NodeSessions;
        }

        public async void Run(bool bRun)
        {
            await Task.Delay(1 * 1000);

            if(LevelDBStore.db_MultiThread)
            {
                networkIDCur += " MT";
            }

            // DNS
            List<string> list = GetNodeSessions();
            List<string> list2 = JsonHelper.FromJson<List<string>>(Program.jdNode["NodeSessions"].ToString());

            // Get Internet IP
            try
            {
                if ( !string.IsNullOrEmpty(Program.jdNode["publicIP"]?.ToString()) )
                {
                    networkInner.ipEndPoint = NetworkHelper.ToIPEndPoint(Program.jdNode["publicIP"].ToString());
                }
                else
                {
                    networkInner.ipEndPoint = NetworkHelper.ToIPEndPoint(GetIpV4() + ":" + networkInner.ipEndPoint.Port);
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        Session session = null;
                        try
                        {
                            session = await networkInner.Get(NetworkHelper.ToIPEndPoint(list[ii]));
                        }
                        catch (Exception)
                        {
                        }
                        if (session != null && session.IsConnect())
                        {
                            Q2P_IP_INFO qIPNode = new Q2P_IP_INFO();
                            R2P_IP_INFO rIPNode = (R2P_IP_INFO)await session.Query(qIPNode, 15);
                            if (rIPNode != null)
                            {
                                networkInner.ipEndPoint = NetworkHelper.ToIPEndPoint(rIPNode.address + ":" + networkInner.ipEndPoint.Port);
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
            }
            Log.Info($"NodeManager  {networkInner.ipEndPoint.ToString()}");
            Log.Info($"NodeSessions {list2[0]}");
            Log.Info($"NodeVersion  {networkIDCur}");

            // 
            Q2P_New_Node new_Node = new Q2P_New_Node();
            new_Node.ActorId = GetMyNodeId();
            new_Node.address = Wallet.GetWallet().GetCurWallet().ToAddress();
            new_Node.ipEndPoint  = networkInner.ipEndPoint.ToString();

            long state = 0;
            var consensus = Entity.Root.GetComponent<Consensus>();
            if (consensus != null)
            {
                state |= consensus.transferShow ? EnumState.transferShow : 0;
                state |= consensus.openSyncFast ? EnumState.openSyncFast : 0;
            }
            state |= Entity.Root.GetComponentInChild<RelayNetwork>() != null ? EnumState.RelayNetwork : 0;
            state |= Entity.Root.GetComponentInChild<TransferComponent>() != null ? EnumState.TransferComponent : 0;
            new_Node.state   = state;

            List<List<NodeData>> nodesQuery = new List<List<NodeData>>();
            for (int ii = 0; ii < list.Count; ii++)
            {
                nodesQuery.Add(new List<NodeData>());
            }
            bool bResponse = false;
            bool bNodeTimeOffset = false;

            while (bRun && list.Count>0)
            {
                new_Node.version = NodeManager.networkIDCur;
                bResponse = false;
                bNodeTimeOffset = false;
                try
                {
                    for (int ii = 0; ii < list.Count; ii++)
                    {
                        new_Node.HashCode = StringHelper.HashCode(JsonHelper.ToJson(nodesQuery[ii]));

                        Session session = null;
                        try
                        {
                            session = await networkInner.Get(NetworkHelper.ToIPEndPoint(list[ii]));
                        }
                        catch (Exception)
                        {
                        }

                        if (session != null && session.IsConnect())
                        {
                            //Log.Debug($"NodeSessions connect " + r2P_New_Node.ActorId);

                            long sendTime = TimeHelper.Now();
                            R2P_New_Node r2P_New_Node = (R2P_New_Node)await session.Query(new_Node, 10f);
                            if (r2P_New_Node != null)
                            {
                                if (!bNodeTimeOffset)
                                {
                                    long timeNow = TimeHelper.Now();
                                     nodeTimeOffset = (timeNow - sendTime) / 2 + r2P_New_Node.nodeTime - timeNow;
                                    bNodeTimeOffset = true;
                                }
                                if (!string.IsNullOrEmpty(r2P_New_Node.Nodes))
                                {
                                    nodesQuery[ii] = JsonHelper.FromJson<List<NodeData>>(r2P_New_Node.Nodes);
                                    bResponse = true;
                                }
                                if (!string.IsNullOrEmpty(r2P_New_Node.Message))
                                {
                                    Log.Warning($"NodeSessions: {r2P_New_Node.Message}");
                                }
                            }
                        }
                    }

                    if(bResponse)
                    {
                        var newnodes = new List<NodeData>();
                        for (int ii = 0; ii < list.Count; ii++)
                        {
                            for (int jj = 0; jj < nodesQuery[ii].Count; jj++)
                            {
                                if (newnodes.Find(x => x.nodeId == nodesQuery[ii][jj].nodeId) == null)
                                {
                                    newnodes.Add(nodesQuery[ii][jj]);
                                }
                            }
                        }
                        nodes = newnodes;
                    }

                    // 等待5秒后关闭连接
                    await Task.Delay(10 * 1000);
                    list = GetNodeSessions();
                }
                catch (Exception)
                {
                    await Task.Delay(10 * 1000);
                }
            }
        }

        [MessageMethod(NetOpcode.Q2P_IP_INFO)]
        static public void Q2P_IP_INFO_Handle(Session session, int opcode, object msg)
        {
            Q2P_IP_INFO qNode = msg as Q2P_IP_INFO;
            R2P_IP_INFO response = new R2P_IP_INFO() { address = session.RemoteAddress.Address.ToString() };
            session.Reply(qNode, response);
        }

        //[MessageMethod(NetOpcode.Q2P_New_Node)]
        async void Q2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            Q2P_New_Node new_Node = msg as Q2P_New_Node;
            try
            {
                if(!CheckNetworkID(new_Node.version))
                {
                    R2P_New_Node response = new R2P_New_Node() { Nodes = "[]", nodeTime = TimeHelper.Now() };
                    response.Message = $"Network Version Not Compatible your:{new_Node.version} cur:{NodeManager.networkIDBase}";
                    session.Reply(new_Node, response);
                    return;
                }

                //Log.Debug($"Q2P_New_Node_Handle {new_Node.address} ipEndPoint: {new_Node.ipEndPoint}  1");
                Session sessionNew = await networkInner.Get(NetworkHelper.ToIPEndPoint(new_Node.ipEndPoint), 15);
                Q2P_IP_INFO qIPNode = new Q2P_IP_INFO();
                R2P_IP_INFO rIPNode = (R2P_IP_INFO)await sessionNew.Query(qIPNode, 15);

                if (rIPNode != null)
                {
                    NodeData data = new NodeData();
                    data.nodeId     = new_Node.ActorId;
                    data.address    = new_Node.address;
                    data.ipEndPoint = new_Node.ipEndPoint;
                    data.state      = new_Node.state;
                    data.version    = new_Node.version;
                    data.kIndex = GetkIndex();
                    AddNode(data);

                    R2P_New_Node response = new R2P_New_Node() { Nodes = "", nodeTime = TimeHelper.Now() };
                    string nodesjson = JsonHelper.ToJson(nodes);
                    if (StringHelper.HashCode(nodesjson) != new_Node.HashCode)
                    {
                        response.Nodes = nodesjson;
                        //session.Send(response);
                    }
                    session.Reply(new_Node, response);
                    return;
                }
            }
            catch(Exception)
            {
            }
            {
                R2P_New_Node response = new R2P_New_Node() { Nodes = "", nodeTime = TimeHelper.Now() };
                response.Message = "LAN not supported or Your network has a firewall";
                session.Reply(new_Node, response);
            }
        }

        void R2P_New_Node_Handle(Session session, int opcode, object msg)
        {
            //R2P_New_Node new_Node = msg as R2P_New_Node;
            //nodes = JsonHelper.FromJson<List<NodeData>>(new_Node.Nodes);
            //var tempnodes = JsonHelper.FromJson<List<NodeData>>(new_Node.Nodes);
            //for(int i=0;i< tempnodes.Count;i++)
            //{
            //    NodeData node = tempnodes[i];
            //    if (nodes.Find((n) => { return n.nodeId == node.nodeId; }) == null)
            //    {
            //        nodes.Add(node);
            //        Log.Debug($"\r\nAddNode {node.nodeId} {node.address} {node.ipEndPoint} kIndex:{node.kIndex} nodes:{nodes.Count}");
            //    }
            //}
        }

        Dictionary<long, float> nodesLastTime = new Dictionary<long, float>();
        public bool AddNode(NodeData data)
        {
            nodesLastTime.Remove(data.nodeId);
            nodesLastTime.Add(data.nodeId, TimeHelper.time);
            NodeData node = nodes.Find((n) => { return n.nodeId == data.nodeId; });
            if (node != null)
            {
                node.address = data.address;
                node.version = data.version;
                node.state = data.state;
                return false;
            }

            nodes.Add(data);

            Log.Debug($"\r\nAddNode {data.nodeId} {data.address} {data.ipEndPoint} kIndex:{data.kIndex} nodes:{nodes.Count}");
            return true;
        }

        TimePass timePass = new TimePass(1);
        public override void Update()
        {
            if (timePass.IsPassSet() && nodesLastTime.Count>0)
            {
                float lastTime = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    NodeData node = nodes[i];
                    if (nodesLastTime.TryGetValue(node.nodeId, out lastTime))
                    {
                        if (TimeHelper.time - lastTime > 600f )
                        {
                            nodes.Remove(node);
                            nodesLastTime.Remove(node.nodeId);
                            i--;

                            Log.Debug($"nodes.Remove {node.address} {node.nodeId} {nodes.Count}");
                        }
                    }
                }
            }
        }

        //async void UpdateNodes()
        //{
        //    Log.Debug($"NodeManager.UpdateNodes");

        //    Session session = null;
        //    while (true)
        //    {
        //        await Task.Delay(5 * 1000);
        //        for (int i = 0; i < nodes.Count; i++)
        //        {
        //            NodeData node = nodes[i];
        //            session = await networkInner.Get(NetworkHelper.ToIPEndPoint(node.ipEndPoint));
        //            if (session != null && session.IsConnect())
        //            {

        //            }
        //            else
        //            {
        //                nodes.RemoveAt(i);
        //                i--;
        //            }
        //            await Task.Delay(1000);
        //            session?.Dispose();
        //        }
        //    }
        //}

        public int GetNodeCount()
        {
            return nodes.Count;
        }

        public NodeData GetMyNode()
        {
            NodeData[] nodesTemp = nodes.Where(a => a.ipEndPoint == networkInner.ipEndPoint.ToString()).ToArray();
            if (nodesTemp.Length != 0)
                return nodesTemp[RandomHelper.Random() % nodesTemp.Length];
            return null;
        }

        public List<NodeData> GetNodeList()
        {
            return nodes;
        }

        public List<NodeData> GetNodeRandomList()
        {
            var lists = new List<NodeData>();
            for (int i = 0; i < nodes.Count; i++)
            {
                lists.Insert( RandomHelper.Range(0, lists.Count) , nodes[i]);
            }
            return lists;
        }

        public NodeData GetRandomNode()
        {
            NodeData[] nodesTemp = nodes.Where( a => a.ipEndPoint != networkInner.ipEndPoint.ToString() ).ToArray();
            if(nodesTemp.Length!=0)
                return nodesTemp[RandomHelper.Random() % nodesTemp.Length];
            return null;
        }

        public string GetRandomNode(long state)
        {
            NodeData[] nodesTemp = nodes.Where(a => a.ipEndPoint != networkInner.ipEndPoint.ToString() && (a.state & state) == state ).ToArray();
            if (nodesTemp.Length != 0)
                return nodesTemp[RandomHelper.Random() % nodesTemp.Length].ipEndPoint.ToString();
            return null;
        }

        public NodeData[] GetNode(long state)
        {
            return nodes.Where(a => a.ipEndPoint != networkInner.ipEndPoint.ToString() && (a.state & state) == state).ToArray();
        }

        public List<NodeData> GetBroadcastNode()
        {
            List<NodeData> result = new List<NodeData>();
            for (int i = 0; i < K_INDEX_ROW; i++)
            {
                List<NodeData> nodestmp = GetkList(i);
                if (nodestmp.Count > 0)
                {
                    result.Add(nodestmp[RandomHelper.Random() % nodestmp.Count]);
                }
            }
            return result;
        }

        async void SendAsync(IPEndPoint ipEndPoint, IMessage message)
        {
            Session session = await networkInner.Get(ipEndPoint);
            if (session != null && session.IsConnect())
            {
                session.Send(message);
            }
        }

        public void Broadcast(P2P_NewBlock p2p_Block, Block block)
        {
            var myNodeId = GetMyNodeId();
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node.nodeId != myNodeId)
                {
                    SendAsync(NetworkHelper.ToIPEndPoint(node.ipEndPoint), p2p_Block);
                }
            }
        }

        public void Broadcast(IMessage message)
        {
            // 向所在桶广播
            Broadcast2Kad(message);

            // 获取广播列表
            List<NodeData> result = GetBroadcastNode();

            // 剔除自己所在桶
            NodeData nodeSelf = nodes.Find((n) => { return n.nodeId == GetMyNodeId(); });
            if (nodeSelf != null)
            {
                NodeData nodeIgnore = result.Find((n) => { return n.kIndex == nodeSelf.kIndex; });
                if (nodeIgnore != null)
                {
                    result.Remove(nodeIgnore);
                }
            }

            // 开始广播
            for (int i = 0; i < result.Count; i++)
            {
                NodeData node = result[i];
                SendAsync(NetworkHelper.ToIPEndPoint(node.ipEndPoint), message);
            }

        }

        // Broadcast to my Kademlia
        public void Broadcast2Kad(IMessage message)
        {
            NodeData nodeSelf = nodes.Find((n) => { return n.nodeId == GetMyNodeId(); });
            if (nodeSelf == null)
                return;

            List<NodeData> result = GetkList(nodeSelf.kIndex);

            for (int i = 0; i < result.Count; i++)
            {
                NodeData node = result[i];
                if (node.nodeId != nodeSelf.nodeId) // ignore self
                {
                    SendAsync(NetworkHelper.ToIPEndPoint(node.ipEndPoint), message);
                }
            }

        }
        public void Broadcast2Kad(int kIndex, IMessage message)
        {
            List<NodeData> result = GetkList(kIndex);

            for (int i = 0; i < result.Count; i++)
            {
                NodeData node = result[i];
                SendAsync(NetworkHelper.ToIPEndPoint(node.ipEndPoint), message);
            }

        }

        // 如果收到的是桶外的数据 , 向K桶内进行一次广播
        public bool IsNeedBroadcast2Kad(string _ipEndPoint)
        {
            if (string.IsNullOrEmpty(_ipEndPoint))
                return false;

            var ipEndPoint = NetworkHelper.ToIPEndPoint(_ipEndPoint);

            NodeData nodetarget = nodes.Find((n) => { return n.nodeId == StringHelper.HashCode(_ipEndPoint); });
            NodeData nodeSelf   = nodes.Find((n) => { return n.nodeId == GetMyNodeId(); });
            if (nodetarget != null && nodeSelf != null)
            {
                if (nodetarget.kIndex != nodeSelf.kIndex)
                {
                    return true;
                }
            }
            return false;
        }

        List<NodeData> GetkList(int i)
        {
            return nodes.FindAll((node) => { return node.kIndex == i; });
        }

        int GetkIndex()
        {
            for (int i = 0; i < K_INDEX_ROW ;i++ )
            {
                List<NodeData> list = nodes.FindAll((node) => { return node.kIndex == i; });
                if (list.Count < K_INDEX_COL)
                    return i;
            }
            return RandomHelper.Random() % K_INDEX_ROW;
        }



    }


}










