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
    // 智能合约lua脚本
    public class LuaVMScript
    {
        public byte[] script;
        public string tablName;
        public void Deserialize(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            if (size != 0)
            {
                script = reader.ReadBytes(size);
            }
            tablName = reader.ReadString();

        }
        public void Serialize(BinaryWriter writer)
        {
            if (script != null)
            {
                writer.Write(script.Length);
                writer.Write(script);
            }
            else
            {
                writer.Write(0);
            }
            writer.Write(tablName ?? "");
        }
    }

    // 智能合约lua数据
    public class LuaVMContext
    {
        public byte[] jsonData;
        public void Deserialize(BinaryReader reader)
        {
            int size = reader.ReadInt32();
            if (size != 0)
            {
                jsonData = reader.ReadBytes(size);
            }
        }
        public void Serialize(BinaryWriter writer)
        {
            if (jsonData != null)
            {
                writer.Write(jsonData.Length);
                writer.Write(jsonData);
            }
            else
            {
                writer.Write(0);
            }
        }
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
                    output = output + args[i].type + ":" + args[i].value;
                    if (i != args.Length - 1)
                        output = output + ",";
                }
                output += ")";
                return output;
            }
            catch (Exception)
            {
                return "";
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
                return luaVMCall;
            }
            catch (Exception)
            {
                luaVMCall.args = new FieldParam[0];
                return luaVMCall;
            }
        }
    }

    // lua脚本运行环境
    public class LuaVMEnv : Component
    {
        Dictionary<string, LuaEnv> cacheLuaEnv = new Dictionary<string, LuaEnv>();
        public LuaEnv GetLuaEnv(string address) 
        {
            if (!cacheLuaEnv.TryGetValue(address, out LuaEnv luaEnv))
            {
                luaEnv = new LuaEnv();
                cacheLuaEnv.TryAdd(address, luaEnv);
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
    SaveAccount = function( account,tableData)
        local jsonData = rapidjson.encode(tableData)
        lualib.SaveAccount(dbSnapshot,contractAddress,account,jsonData)
    end
    LoadAccount = function( account )
        local jsondata  = lualib.LoadAccount(dbSnapshot,contractAddress,account)
        if jsondata ~= nil and jsondata ~= '' then
            local tabledata = rapidjson.decode(jsondata)
            return jsontable
        end
        return nil
    end
    print = CS.ETModel.LuaContract.LogPrint 
    _G = nil
    coroutine = nil
    io = nil
    os = nil
    CS = nil
    require = nil
    --debug = nil
end";


        public static string GetContractAddress(BlockSub transfer)
        {
            string address = string.IsNullOrEmpty(transfer.addressOut) 
                            ? Wallet.ToAddress(CryptoHelper.Sha256(Encoding.UTF8.GetBytes($"{transfer.addressIn}#{transfer.data}#{transfer.timestamp}#{FileHelper.GetFileData($"./Data/Contract/{transfer.depend}.lua")}")))
                            : transfer.addressOut;
            return address;
        }

        public static string     s_consAddress;
        public static BlockSub   s_transfer;
        public static DbSnapshot s_dbSnapshot = null;
        public bool Execute(DbSnapshot dbSnapshot, BlockSub transfer,long height,out object[] result)
        {
            LuaVMCall luaVMCall = new LuaVMCall();
            LuaVMScript luaVMScript = null;
            LuaVMContext LuaVMContext = null;
            result = null;
            try
            {
                string address = GetContractAddress(transfer);
                LuaEnv luaenv  = GetLuaEnv(address);

                //LuaEnv Global
                s_consAddress = address;
                s_transfer    = transfer;
                s_dbSnapshot  = dbSnapshot;

                luaenv.DoString(initScript);

                if (string.IsNullOrEmpty(transfer.addressOut))
                {
                    // 已存在
                    if (dbSnapshot.Contracts.Get(address) != null)
                        return false;

                    luaVMScript = new LuaVMScript() { script = FileHelper.GetFileData($"./Data/Contract/{transfer.depend}.lua").ToByteArray() };
                    LuaVMContext = new LuaVMContext() { jsonData = "{}".ToByteArray() };
                    luaVMCall = LuaVMCall.Decode(transfer.data);
                    dbSnapshot.Contracts.Add(address, luaVMScript);
                    luaenv.DoString(luaVMScript.script);
                }
                else
                {
                    luaVMScript = dbSnapshot.Contracts.Get(address);
                    LuaVMContext = dbSnapshot.Storages.Get(address);
                    luaVMCall = LuaVMCall.Decode(transfer.data);
                    luaenv.DoString(luaVMScript.script, transfer.addressOut);
                    luaenv.DoString($"Storages = rapidjson.decode('{LuaVMContext.jsonData.ToStr()}')\n");
                }

                object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);

                luaenv.Global.Set("curHeight", height);
                luaenv.Global.Set("sender",transfer.addressIn);
                result = luaFun?.Call(args);

                // rapidjson
                luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                dbSnapshot.Storages.Add(address, LuaVMContext);
                luaenv.GC();

                s_consAddress = "";
                s_dbSnapshot  = null;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                Log.Info($"LuaVMEnv.Execute Error, transfer.hash: {transfer.hash} , contract: {transfer.addressOut} func:{luaVMCall.fnName}");
            }
            s_consAddress = "";
            s_dbSnapshot  = null;
            return false;
        }

        public class RuleContract
        {
            public string Address;
            public RuleInfo[] RuleInfo;
        }

        public Dictionary<string, RuleInfo> GetRules(string address, long height,bool bCommit=false)
        {
            Dictionary<string, RuleInfo> rules = new Dictionary<string, RuleInfo>();
            try
            {
                LuaEnv luaenv = GetLuaEnv(address);
                LuaVMCall luaVMCall = new LuaVMCall();
                LuaVMScript luaVMScript = null;
                LuaVMContext LuaVMContext = null;

                using (DbSnapshot dbSnapshot = Entity.Root.GetComponent<LevelDBStore>().GetSnapshot())
                {
                    s_dbSnapshot = dbSnapshot;
                    LuaVMContext Storages = dbSnapshot?.Storages.Get(address);
                    // rapidjson
                    luaenv.DoString(initScript);
                    luaVMScript = dbSnapshot.Contracts.Get(address);
                    LuaVMContext = dbSnapshot.Storages.Get(address);
                    luaenv.DoString(luaVMScript.script, address);
                    luaenv.DoString($"Storages = rapidjson.decode('{LuaVMContext.jsonData.ToStr()}')\n");
                    luaVMCall.fnName = "update";
                    luaVMCall.args = new FieldParam[0];

                    object[] args = luaVMCall.args.Select(a => a.GetValue()).ToArray();
                    LuaFunction luaFun = luaenv.Global.Get<LuaFunction>(luaVMCall.fnName);

                    luaenv.DoString($"curHeight    =  {height}\n");
                    luaFun.Call(args);

                    // rapidjson
                    luaenv.DoString("StoragesJson = rapidjson.encode(Storages)\n");
                    LuaVMContext.jsonData = luaenv.Global.Get<string>("StoragesJson").ToByteArray();
                    if (bCommit)
                    {
                        dbSnapshot.Storages.Add(address, LuaVMContext);
                        dbSnapshot.Commit();
                    }

                    JToken jdStorages = JToken.Parse(LuaVMContext.jsonData.ToStr());
                    JToken[] jdRule = jdStorages["Rules"].ToArray();
                    for (int ii = 0; ii < jdRule.Length; ii++)
                    {
                        RuleInfo rule = new RuleInfo();
                        rule.Address = jdRule[ii]["Address"].ToString();
                        rule.Start = long.Parse(jdRule[ii]["Start"].ToString());
                        rule.End = long.Parse(jdRule[ii]["End"].ToString());
                        rule.LBH = long.Parse(jdRule[ii]["LBH"].ToString());
                        rules.Remove(rule.Address);
                        rules.Add(rule.Address, rule);
                    }
                    luaenv.GC();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
                Log.Info("GetRules Error!");
            }
            return rules;
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
