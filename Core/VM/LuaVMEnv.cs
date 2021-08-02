using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using XLua;
using System.Linq;
using System.IO;

namespace ETModel
{
    public class LuaVMStack
    {
        public static string   s_consAddress;
        public static string   s_sender;
        public static BlockSub s_transfer = null;
        public static LuaVMDB  s_dbSnapshot = null;
        public static int      s_maxDepth = 0;

        public class StackItem
        {
            public string consAddress;
            public string sender;
        }
        private static Stack<StackItem> queue = new Stack<StackItem>();

        public static void Enqueue(string _consAddress, string _sender)
        {
            if(queue.FirstOrDefault( x=>x.consAddress==_consAddress) != null) {
                queue.Push(new StackItem());
                throw new Exception("LuaVMStack Enqueue throw");
            }

            var stack = new StackItem() { consAddress  = $"{_consAddress}"  , sender = $"{ _sender}" };
            queue.Push(stack);

            s_consAddress = stack.consAddress;
            s_sender      = stack.sender;
            s_maxDepth++;
        }

        public static void Dequeue()
        {
            queue.Pop();
            var stack = queue.Peek();
            s_consAddress = stack?.consAddress;
            s_sender      = stack?.sender;

        }

        public static int curDepth()
        {
            return queue.Count;
        }

        public static void Reset(BlockSub _transfer,LuaVMDB _dbSnapshot, string _consAddress, string _sender)
        {
            s_transfer   = _transfer;
            s_dbSnapshot = _dbSnapshot;
            s_maxDepth   = 0;
            queue.Clear();
            if (_consAddress != null && _sender != null)
            {
                Enqueue(_consAddress, _sender);
            }
        }

        public static void Add2TransferTemp(string msg, BlockSub _transfer=null)
        {
            _transfer = _transfer ?? s_transfer;
            if (_transfer != null && !string.IsNullOrEmpty(msg))
            {
                if (_transfer.temp == null)
                {
                    _transfer.temp = new List<string>();
                }
                _transfer.temp.Remove(msg);
                _transfer.temp.Add(msg);
            }
        }
    }

    // 智能合约lua脚本
    public class LuaVMScript
    {
        public byte[] script;
        public string tablName;

        static public LuaVMScript Get(DbSnapshot dbSnapshot, string consAddress)
        {
            var luaVMScript = dbSnapshot.Contracts.Get(consAddress);
            if (luaVMScript == null)
                throw new Exception($"Smart Contract not exist: {consAddress}");
            if (!string.IsNullOrEmpty(luaVMScript.tablName))
            {
                luaVMScript.script = FileHelper.GetFileData($"./Data/Contract/{luaVMScript.tablName}.lua").ToByteArray();
            }

            return luaVMScript;
        }
        static public LuaVMScript Get(LuaVMDB dbSnapshot, string consAddress)
        {
            var luaVMScript = dbSnapshot.Contracts.Get(consAddress);
            if (luaVMScript == null)
                throw new Exception($"Smart Contract not exist: {consAddress}");
            if (!string.IsNullOrEmpty(luaVMScript.tablName))
            {
                luaVMScript.script = FileHelper.GetFileData($"./Data/Contract/{luaVMScript.tablName}.lua").ToByteArray();
            }
            return luaVMScript;
        }
    }

    // 智能合约lua数据
    public class LuaVMContext
    {
        public byte[] jsonData;
    }

    public class FieldParam
    {
        public string type;
        public string value;

        public object GetValue()
        {
            return Convert.ChangeType(value, Type.GetType("System."+type));
        }
    }

    // 传入lua的参数
    public class LuaVMCall
    {
        public string       fnName;
        public FieldParam[] args;

        // "add(int32 "123",)"

        public string Encode()
        {
            try
            {
                string output = fnName + "(";
                for (int i = 0; i < args.Length; i++)
                {
                    if(args[i].type == "System.String")
                        output = $"{output}\"{args[i].value}\"";
                    else
                        output = $"{output}{args[i].value}";
                    if (i != args.Length - 1)
                        output = output + ",";
                }
                output += ")";
                return output;
            }
            catch (Exception)
            {
                return "error_call";
            }
        }

        public static LuaVMCall Decode(string str)
        {
            var luaVMCall = new LuaVMCall();
            try
            {
                string[] arrayName = str.Split('(');
                luaVMCall.fnName = arrayName[0];

                arrayName[1] = arrayName[1].Replace(" ", "").Replace(")", "");
                var list = new List<FieldParam>();
                if (arrayName[1] != "")
                {
                    string[] arrayParam = arrayName[1].Split(',');
                    for (int i = 0; i < arrayParam.Length; i++)
                    {
                        int indexOf = arrayParam[i].IndexOf("\"");
                        if (indexOf != -1)
                        {
                            var fieldParam = new FieldParam();
                            fieldParam.type = "String";
                            fieldParam.value = arrayParam[i].Replace("\"", "");
                            list.Add(fieldParam);
                        }
                        else
                        {
                            var fieldParam = new FieldParam();
                            fieldParam.type = "UInt64";
                            fieldParam.value = arrayParam[i];
                            list.Add(fieldParam);
                        }

                    }
                }
                luaVMCall.args = list.ToArray();
                if (luaVMCall.fnName[0] == '_')
                {
                    throw new Exception($"{luaVMCall.fnName} is private function");
                }
                return luaVMCall;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    // lua脚本运行环境
    public class LuaVMEnv : Component
    {
        Dictionary<string, LuaEnv> cacheLuaEnv = new Dictionary<string, LuaEnv>();
        public LuaEnv GetLuaEnv(string address,string prefix) 
        {
            string name = $"{prefix}___{LuaVMStack.curDepth()}___{address}";
            if (!cacheLuaEnv.TryGetValue(name, out LuaEnv luaEnv))
            {
                luaEnv = new LuaEnv();
                cacheLuaEnv.TryAdd(name, luaEnv);
            }

            return luaEnv;
        }

        public override void Awake(JToken jd = null)
        {


        }

        Consensus consensus = null;
        public override void Start()
        {
            consensus = Entity.Root.GetComponent<Consensus>();


        }

        string initScript = @"
if lualib == nil then
    lualib = CS.ETModel.LuaContract
    biglib = CS.ETModel.BigHelper
    rapidjson = require 'rapidjson'
    print = CS.ETModel.LuaContract.LogPrint 
    _G = nil
    coroutine = nil
    io = nil
    os = nil
    CS = nil
    require = nil
    --debug = nil

    luaDB = {}
    luaDB.GetValue = function(a,b)
        local v = lualib._GetValue(a,b)
        if v ~= '' and v ~= nil then
            return rapidjson.decode(v)
        end
        return nil
    end

    luaDB.SetValue = function(a,b,v)
        lualib._SetValue(a,b,rapidjson.encode(v));
    end

end";


        public static string GetContractAddress(BlockSub transfer)
        {
            //string address = string.IsNullOrEmpty(transfer.addressOut) 
            //                ? Wallet.ToAddress(CryptoHelper.Sha256(Encoding.UTF8.GetBytes($"{transfer.addressIn}#{transfer.data}#{transfer.timestamp}#{FileHelper.GetFileData($"./Data/Contract/{transfer.depend}.lua")}")))
            //                : transfer.addressOut;

            // debug
            string address = string.IsNullOrEmpty(transfer.addressOut)
                            ? Wallet.ToAddress(CryptoHelper.Sha256(Encoding.UTF8.GetBytes($"{transfer.addressIn}#{transfer.data}#{transfer.timestamp}#./Data/Contract/{transfer.depend}.lua")))
                            : transfer.addressOut;
            return address;
        }

        public static string GetContractAddress(string addressIn,string data,long timestamp,string depend)
        {
            //return Wallet.ToAddress(CryptoHelper.Sha256(Encoding.UTF8.GetBytes($"{addressIn}#{data}#{timestamp}#{FileHelper.GetFileData($"./Data/Contract/{depend}.lua")}")));
            // debug
            return Wallet.ToAddress(CryptoHelper.Sha256(Encoding.UTF8.GetBytes($"{addressIn}#{data}#{timestamp}#./Data/Contract/{depend}.lua)")));
        }

        public bool Execute(DbSnapshot dbSnapshot, BlockSub transfer,long height,out object[] result)
        {
            lock (this)
            {
            using (LuaVMDB luaVMDB = new LuaVMDB(dbSnapshot))
            {
                result = null;
                LuaVMCall luaVMCall = new LuaVMCall();
                LuaVMScript luaVMScript = null;
                LuaVMContext LuaVMContext = null;
                try
                {
                    string consAddress = GetContractAddress(transfer);
                    LuaEnv luaenv = GetLuaEnv(consAddress, "Execute");

                    //LuaEnv Global
                    LuaVMStack.Reset(transfer, luaVMDB, consAddress, transfer.addressIn);

                    luaenv.DoString(initScript);

                    if (string.IsNullOrEmpty(transfer.addressOut))
                    {
                        // 已存在
                        if (luaVMDB.Contracts.Get(consAddress) != null)
                            return false;

                        luaVMScript = new LuaVMScript() { script = FileHelper.GetFileData($"./Data/Contract/{transfer.depend}.lua").ToByteArray(), tablName = transfer.depend };
                        LuaVMContext = new LuaVMContext() { jsonData = "{}".ToByteArray() };
                        luaVMCall = LuaVMCall.Decode(transfer.data);
                        luaVMDB.Contracts.Add(consAddress, luaVMScript);
                        luaenv.DoString(luaVMScript.script);
                    }
                    else
                    {
                        luaVMScript = LuaVMScript.Get(luaVMDB, consAddress);
                        LuaVMContext = luaVMDB.Storages.Get(consAddress);
                        luaVMCall = LuaVMCall.Decode(transfer.data);
                        luaenv.DoString(luaVMScript.script, transfer.addressOut);
                        luaenv.DoString($"Storages = rapidjson.decode('{LuaVMContext.jsonData.ToStr()}')\n");
                    }

                    object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                    LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);

                    luaenv.Global.Set("curHeight", height);
                    luaenv.Global.Set("sender", transfer.addressIn);
                    luaenv.Global.Set("addressThis", consAddress);
                    if (luaFun != null)
                    {
                        result = luaFun.Call(args);

                        // rapidjson
                        luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                        LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                        luaVMDB.Storages.Add(consAddress, LuaVMContext);
                        luaVMDB.Commit();
                        luaenv.GC();

                        return true;
                    }
                }
                catch (Exception e)
                {
#if !RELEASE
                    Log.Error(e);
                    Log.Info($"LuaVMEnv.Execute Error, transfer.hash: {transfer.hash} , contract: {transfer.addressOut} func:{luaVMCall.fnName}");
#endif
                    var array = e.Message.Split("\n");
                    if (array != null && array.Length > 0)
                    {
                        array[0] = array[0].Replace("c# exception:XLua.LuaException: c# exception:System.Exception: ", "");
                        LuaVMStack.Add2TransferTemp(array[0]);
                    }
                }
                finally
                {
                    LuaVMStack.Reset(null, null, null, null);
                }
                return false;
                }
            }
        }

        public bool LuaCall(LuaVMDB dbSnapshot, string consAddress, string sender, string data,long height, out object[] result)
        {
            LuaVMCall luaVMCall = new LuaVMCall();
            LuaVMScript luaVMScript = null;
            LuaVMContext LuaVMContext = null;
            LuaEnv luaenv = GetLuaEnv(consAddress, "LuaCall");

            result = null;
            try
            {
                LuaVMStack.Enqueue(consAddress, sender);

                luaenv.DoString(initScript);

                luaVMScript = LuaVMScript.Get(dbSnapshot, consAddress);
                LuaVMContext = dbSnapshot.Storages.Get(consAddress);
                luaVMCall = LuaVMCall.Decode(data);
                luaenv.DoString(luaVMScript.script, consAddress);
                luaenv.DoString($"Storages = rapidjson.decode('{LuaVMContext.jsonData.ToStr()}')\n");

                object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);

                luaenv.Global.Set("curHeight", height);
                luaenv.Global.Set("sender", sender);
                luaenv.Global.Set("addressThis", consAddress);
                if (luaFun != null)
                {
                    result = luaFun.Call(args);

                    // rapidjson
                    luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                    LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                    dbSnapshot.Storages.Add(consAddress, LuaVMContext);

                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                LuaVMStack.Dequeue();
            }

            return false;
        }

        public bool LuaCreate(LuaVMDB dbSnapshot, string sender, string data, long timestamp, string depend, long height,out string consAddress)
        {
            LuaVMCall    luaVMCall    = new LuaVMCall();
            LuaVMScript  luaVMScript  = null;
            LuaVMContext LuaVMContext = null;
            LuaEnv luaenv = GetLuaEnv(depend, "LuaCreate");

            consAddress = GetContractAddress(sender, data, timestamp, depend);
            try
            {
                LuaVMStack.Enqueue(consAddress, sender);

                luaenv.DoString(initScript);

                luaVMScript = new LuaVMScript() { script = FileHelper.GetFileData($"./Data/Contract/{depend}.lua").ToByteArray(), tablName = depend };
                LuaVMContext = new LuaVMContext() { jsonData = "{}".ToByteArray() };
                luaVMCall = LuaVMCall.Decode(data);
                dbSnapshot.Contracts.Add(consAddress, luaVMScript);
                luaenv.DoString(luaVMScript.script);

                object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);
                luaenv.Global.Set("curHeight", height);
                luaenv.Global.Set("sender", sender);
                luaenv.Global.Set("addressThis", consAddress);
                if (luaFun != null)
                {
                    luaFun.Call(args);

                    // rapidjson
                    luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                    LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                    dbSnapshot.Storages.Add(consAddress, LuaVMContext);

                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                LuaVMStack.Dequeue();
            }
            return false;
        }

        public bool IsERC(LuaVMDB dbSnapshot, string consAddress,string scriptName)
        {
            var luaVMScript = LuaVMScript.Get(dbSnapshot, consAddress);
            if (luaVMScript != null)
            {
                if (string.IsNullOrEmpty(scriptName))
                    return true;
                return luaVMScript.tablName == scriptName;
            }
            return false;
        }

        public bool IsERC(DbSnapshot dbSnapshot, string consAddress, string scriptName)
        {
            var luaVMScript = LuaVMScript.Get(dbSnapshot, consAddress);
            if (luaVMScript != null)
            {
                if (string.IsNullOrEmpty(scriptName))
                    return true;
                return luaVMScript.tablName == scriptName;
            }
            return false;
        }

        public class RuleContract
        {
            public string Address;
            public RuleInfo[] RuleInfo;
        }

        public Dictionary<string, RuleInfo> GetRules(string address, long height, DbSnapshot dbSnapshot)
        {
            lock (this)
            {

            Dictionary<string, RuleInfo> rules = new Dictionary<string, RuleInfo>();
            try
            {
                LuaEnv luaenv = GetLuaEnv(address, "GetRules");
                LuaVMCall luaVMCall = new LuaVMCall();
                LuaVMScript luaVMScript = null;
                LuaVMContext LuaVMContext = null;

                var luaVMDB = new LuaVMDB(dbSnapshot);
                LuaVMStack.Reset(null, luaVMDB, null, null);

                LuaVMContext Storages = dbSnapshot?.Storages.Get(address);
                // rapidjson
                luaenv.DoString(initScript);
                luaVMScript = LuaVMScript.Get(dbSnapshot, address);
                LuaVMContext = dbSnapshot.Storages.Get(address);
                luaenv.DoString(luaVMScript.script, address);
                luaenv.DoString($"Storages = rapidjson.decode('{LuaVMContext.jsonData.ToStr()}')\n");
                luaVMCall.fnName = "update";
                luaVMCall.args = new FieldParam[0];

                object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);

                luaenv.Global.Set("curHeight", height);
                luaenv.Global.Set("sender", "");
                luaenv.Global.Set("addressThis", address);
                luaFun.Call(args);

                // rapidjson
                luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                dbSnapshot.Storages.Add(address, LuaVMContext);

                JToken jdStorages = JToken.Parse(LuaVMContext.jsonData.ToStr());
                JToken[] jdRule = jdStorages["Rules"].ToArray();
                for (int ii = 0; ii < jdRule.Length; ii++)
                {
                    RuleInfo rule = new RuleInfo();
                    rule.Address = jdRule[ii]["Address"].ToString();
                    rule.Contract = jdRule[ii]["Contract"]?.ToString();
                    rule.Amount = jdRule[ii]["Amount"]?.ToString();
                    rule.Start = long.Parse(jdRule[ii]["Start"].ToString());
                    rule.End = long.Parse(jdRule[ii]["End"].ToString());
                    rule.LBH = long.Parse(jdRule[ii]["LBH"].ToString());
                    rules.Remove(rule.Address);
                    rules.Add(rule.Address, rule);
                }
                luaenv.GC();
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
                Log.Info("GetRules Error!");
            }
            finally
            {
                LuaVMStack.Reset(null, null, null, null);
            }
            return rules;
            }
        }

        public static void TestRapidjson(string[] args)
        {
            // 测试代码
            LuaEnv luaenv = new LuaEnv();

            luaenv.DoString("rapidjson = require 'rapidjson' \n" +
            "print(rapidjson)\n" +
            //luaenv.DoString("print('LuaEnv Test') \n" +
            "testTable = {} \n" +
            "for i = 1 , 100*10000 , 1 do \n" +
            "testTable[ 'key_'..i] = ''..i\n" +
            "end\n");

            luaenv.DoString(
            "  print('rapidjson.encode start)')\n" +
            "local ss = rapidjson.encode(testTable,{ pretty = false,sort_keys = true,max_depth = 128})\n" +
            "  print('rapidjson.encode end)')\n" +
            "local aa = rapidjson.encode(testTable,{ pretty = false,sort_keys = true,max_depth = 128})\n" +
            "if ss==aa then\n" +
            "  print('if(ss==aa)')\n" +
            "end\n" +
            "  print('rapidjson.decode start)')\n" +
            "local cc = rapidjson.decode(aa)\n" +
            "print('table ', cc['key_1000'])\n" +
            "--print('json', ss)\n");

            luaenv.Dispose();

            Log.Debug($"配置： {args[0]}");
            string NodeKey = args[1];

        }

        public static void TestLib(string[] args)
        {
            Log.Debug("LuaVMEnv.TestLib");
            try
            {
                LuaEnv luaenv = new LuaEnv();
                luaenv.DoString(@"
-- 全局环境在注册表中的索引值（见lua.h）
local LUA_RIDX_GLOBALS = 2

-- 安全table的metatable标志
local SAFE_TABLE_FLAG = '.SAFETABLE'

local function CreateSafeTable(base)
	local new = {}			-- 新table
	local mt = {}			-- metatable

	-- 如果没有指定base则新建一个空table
	local proxy = (type(base) == 'table') and base or {}

    -- 操作重载

    mt.__index = proxy
    mt.__newindex = function() print('cannot modify safe table!') end
    mt.__len = function() return #proxy end
	mt.__pairs = function() return pairs(proxy) end
    mt.__ipairs = function() return ipairs(proxy) end

    -- 隐藏metatable
    mt.__metatable = 0
    -- 标记为安全table
    mt[SAFE_TABLE_FLAG] = true
    -- 设置新table的metatable
    setmetatable(new, mt)

    -- 返回新table和对应的代理table
    return new, proxy
end

old_env = _G
new_env = CreateSafeTable(_G)
_G = new_env


setfenv(1, new_env);

table.remove = 'aaa'

");                

                luaenv.DoString("lualib = CS.ETModel.LuaContract \n");
                //luaenv.DoString("print = CS.ETModel.LuaContract.LogPrint \n lualib = CS.ETModel.LuaContract \n");
                //luaenv.DoString("require = nil\n _G = nil\n coroutine = nil\n io = nil\n os = nil \n utf8 = nil \n debug = nil \n CS = nil \n");
                luaenv.DoString("require = nil\n coroutine = nil\n io = nil\n os = nil \n utf8 = nil \n debug = nil \n CS = nil \n");

                luaenv.DoString("for k,v in pairs(old_env) do \n" +
                "   print(string.format('%s => %s',k,v))\n" +
                "end");

                luaenv.DoString("print(require) \n");
                luaenv.DoString("print(_G) \n");
                luaenv.DoString("print(coroutine) \n");
                luaenv.DoString("print(io) \n");
                luaenv.DoString("print(os) \n");
                luaenv.DoString("print(utf8) \n");
                luaenv.DoString("print(debug) \n");
                luaenv.DoString("print(CS) \n");

                luaenv.DoString("lualib.VerifySignature('11','22','33') \n");

            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        public static void TestCoroutine(string[] args)
        {
            try
            {
                Wallet wallet = Wallet.GetWallet();
                WalletKey walletKey = wallet.GetCurWallet();

                Log.Info($"\npublic_key  {walletKey.publickey.ToHexString()}   {walletKey.publickey.ToHexString().Length}" );
                Log.Info($"\nprivate_key {walletKey.privatekey.ToHexString()}   {walletKey.privatekey.ToHexString().Length}" );

                byte[] message = "e92e086a818ee476dea6f46d15ededb441bbe7926e806efe45dbad14a4b627f0".HexToBytes();
                string aa = message.ToHexString();
                Log.Info($"\n aa {aa}   {aa.Length}");

                Log.Info($"\nmessage {message.ToHexString()}   {message.ToHexString().Length}" );

                byte[] signature = Wallet.Sign(message, walletKey);

                Log.Info($"\nsignature {signature.ToHexString()}  {signature.Length}");

                if (Wallet.Verify(signature, message,  Wallet.ToAddress(walletKey.publickey)) )
                {
                    Log.Info($"\nverify Success");
                }

                Log.Info($"\nAddress {walletKey.ToAddress()}  {walletKey.ToAddress().Length}");

            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        public static void Test_number(string[] args)
        {
            LuaEnv luaenv = new LuaEnv();

            //decimal aaa = 79228162514264337593543950335M;
            //decimal bbb = 79228162514264337593543950335M;

            luaenv.DoString("aa = 9 * 10000 * 100000000 * 1000000 \n" + 
                            "print(aa) \n");

            luaenv.DoString("bb = 3772005120*10000 \n" +
                            "bb = bb+1 \n" +
                            "print(bb) \n");

            long a = 3772005120L * 10000L;
            long b = long.MaxValue;
            Log.Info(a.ToString());
            Log.Info(b.ToString());

        }





    }
}
