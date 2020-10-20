using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ETModel;

namespace ETTools
{
    internal class OpcodeInfo
    {
        public string Name;
        public int Opcode;
    }

    public static class Program
    {
        public static void Main()
        {
            #region if use Protobuf3
            string protoc = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                protoc = "protoc.exe";
            }
            else
            {
                protoc = "protoc";
            }
            ProcessHelper.Run(protoc, "--csharp_out=\"./MessageProto/\" --proto_path=\"./\" OuterMessage.proto", waitExit: true);
            Proto2CS("ETModel" , "OuterMessage.proto", "./MessageProto/", "NetOpcode", "OuterOpcode", 5000);

            // 前端OuterOpcode.cs的命名空间需要修改
            Proto2CS("ETHotfix", "OuterMessage.proto", "./MessageProto/", "NetOpcode", "OuterOpcode_Client", 5000);
            // 前端OuterMessage.cs的命名空间需要修改
            StreamReader sr = new StreamReader(new FileStream("./MessageProto/OuterMessage.cs", FileMode.Open, FileAccess.Read, FileShare.ReadWrite), System.Text.Encoding.UTF8);
            string strTxt = sr.ReadToEnd();
            strTxt = strTxt.Replace("namespace ETModel {", "namespace ETHotfix {");
            strTxt = strTxt.Replace("MessagePool.", "ETModel.MessagePool.");
            File.WriteAllText("./MessageProto/OuterMessage_Client.cs", strTxt.ToString());


            #endregion

            // InnerMessage.proto生成cs代码
            Proto2CS InnerProto2CS = new Proto2CS();
            InnerProto2CS.GenerateProtoCS("ETModel", "InnerMessage.proto", "./MessageProto/", "NetOpcode", "InnerOpcode", 100);
            InnerProto2CS.GenerateOpcode("ETModel", "InnerMessage.proto", "./MessageProto/", "NetOpcode", "InnerOpcode", 100);

            //Proto2CS OuterProto2CS = new Proto2CS();
            //OuterProto2CS.GenerateProtoCS("ETModel", "OuterMessage.proto", "./MessageProto/", "NetOpcode", "OuterOpcode", 5000);
            //OuterProto2CS.GenerateOpcode("ETModel", "OuterMessage.proto", "./MessageProto/", "NetOpcode", "OuterOpcode", 5000);


            Console.WriteLine("proto2cs succeed!");
        }

        #region if use Protobuf3

        private const string protoPath = ".";
        private const string MessagePath = "./MessageProto/";
        private static readonly char[] splitChars = { ' ', '\t' };
        private static readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();

        public static void Proto2CS(string ns, string protoName, string outputPath, string opcodeClassName, string opcodeFileName, int startOpcode, bool isClient = true)
        {
            msgOpcode.Clear();
            string proto = Path.Combine(protoPath, protoName);

            string s = File.ReadAllText(proto);

            StringBuilder sb = new StringBuilder();
            sb.Append("using Google.Protobuf;\n");
            sb.Append($"namespace {ns}\n");
            sb.Append("{\n");

            bool isMsgStart = false;

            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();

                if (newline == "")
                {
                    continue;
                }

                if (newline.StartsWith("//"))
                {
                    sb.Append($"{newline}\n");
                }

                if (newline.StartsWith("message"))
                {
                    string parentClass = "";
                    isMsgStart = true;
                    string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }
                    else
                    {
                        parentClass = "";
                    }

                    if (GetAppType(msgName) == "")
                        continue;

                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    sb.Append($"\t[Message({opcodeClassName}.{msgName},{GetAppType(msgName)})]\n");
                    sb.Append($"\tpublic partial class {msgName} ");
                    if (parentClass != "")
                    {
                        sb.Append($": {parentClass} ");
                    }

                    sb.Append("{}\n\n");
                }

                if (isMsgStart && newline == "}")
                {
                    isMsgStart = false;
                }
            }

            sb.Append("}\n");

            GenerateOpcode(ns, opcodeClassName, opcodeFileName, outputPath, sb);
        }

        private static string GetAppType(string msgName)
        {
            string AppType = "";
            if (msgName[2] == 'G')
                AppType = "ETModel.AppType.Gate";
            else
            if (msgName[2] == 'C')
                AppType = "ETModel.AppType.Core";
            else
            if (msgName[2] == 'O')
                AppType = "ETModel.AppType.Out";
            else
            if (msgName[2] == 'P')
                AppType = "ETModel.AppType.Core";

            return AppType;
        }

        private static void GenerateOpcode(string ns, string opcodeClassName, string opcodeFileName, string outputPath, StringBuilder sb)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static partial class {opcodeClassName}");
            sb.AppendLine("\t{");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t public const ushort {info.Name} = {info.Opcode};");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string csPath = Path.Combine(outputPath, opcodeFileName + ".cs");
            File.WriteAllText(csPath, sb.ToString());
        }
        #endregion
    }

    public class Proto2CS
    {
        private const string protoPath = ".";
        private readonly char[] splitChars = { ' ', '\t' };
        private readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();

        public Proto2CS()
        {

        }

        public void GenerateProtoCS(string ns, string protoName, string outputPath, string opcodeClassName, string opcodeFileName, int startOpcode)
        {
            msgOpcode.Clear();
            string proto = Path.Combine(protoPath, protoName);
            string csPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(proto) + ".cs");

            string s = File.ReadAllText(proto);

            StringBuilder sb = new StringBuilder();
            sb.Append("using ETModel;\n");
            sb.Append("using System.Collections.Generic;\n");
            sb.Append($"namespace {ns}\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            string parentClass = "";
            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();

                if (newline == "")
                {
                    continue;
                }

                if (newline.StartsWith("//"))
                {
                    sb.Append($"{newline}\n");
                }

                if (newline.StartsWith("message"))
                {
                    parentClass = "";
                    isMsgStart = true;
                    string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }

                    bool isOpcode = GetAppType(msgName) != "";

                    if(isOpcode)
                        msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    if (isOpcode)
                        sb.Append($"\t[Message({opcodeClassName}.{msgName},{GetAppType(msgName)})]\n");

                    sb.Append($"\tpublic partial class {msgName}");
                    if (parentClass == "IActorMessage" || parentClass == "IActorRequest" || parentClass == "IActorResponse" ||
                        parentClass == "IFrameMessage")
                    {
                        sb.Append($": {parentClass}\n");
                    }
                    else if (parentClass != "")
                    {
                        sb.Append($": {parentClass}\n");
                    }
                    else
                    {
                        sb.Append("\n");
                    }

                    continue;
                }

                if (isMsgStart)
                {
                    if (newline == "{")
                    {
                        sb.Append("\t{\n");
                        continue;
                    }

                    if (newline == "}")
                    {
                        isMsgStart = false;
                        sb.Append("\t}\n\n");
                        continue;
                    }

                    if (newline.Trim().StartsWith("//"))
                    {
                        sb.AppendLine(newline);
                        continue;
                    }

                    if (newline.Trim() != "" && newline != "}")
                    {
                        if (newline.StartsWith("repeated"))
                        {
                            Repeated(sb, ns, newline);
                        }
                        else
                        {
                            Members(sb, newline, true);
                        }
                    }
                }
            }

            sb.Append("}\n");

            File.WriteAllText(csPath, sb.ToString());
        }

        public void GenerateOpcode(string ns, string protoName, string outputPath, string opcodeClassName, string opcodeFileName, int startOpcode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static partial class {opcodeClassName}");
            sb.AppendLine("\t{");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.AppendLine($"\t\t public const ushort {info.Name} = {info.Opcode};");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string csPath = Path.Combine(outputPath, opcodeFileName + ".cs");
            File.WriteAllText(csPath, sb.ToString());
        }

        private static string GetAppType(string msgName)
        {
            string AppType = "";

            if (msgName[1] != '2')
                AppType = "";
            else
            if (msgName[2] == 'G')
                AppType = "AppType.Gate";
            else
            if (msgName[2] == 'C')
                AppType = "AppType.Core";
            else
            if (msgName[2] == 'O')
                AppType = "AppType.Out";
            else
            if (msgName[2] == 'P')
                AppType = "AppType.Core";

            return AppType;
        }

        private void Repeated(StringBuilder sb, string ns, string newline)
        {
            try
            {
                int index = newline.IndexOf(";");
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[1];
                type = ConvertType(type);
                string name = ss[2];

                sb.Append($"\t\tpublic List<{type}> {name} = new List<{type}>();\n\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        private string ConvertType(string type)
        {
            string typeCs = "";
            switch (type)
            {
                case "int16":
                    typeCs = "short";
                    break;
                case "int32":
                    typeCs = "int";
                    break;
                case "bytes":
                    typeCs = "byte[]";
                    break;
                case "uint32":
                    typeCs = "uint";
                    break;
                case "long":
                    typeCs = "long";
                    break;
                case "int64":
                    typeCs = "long";
                    break;
                case "uint64":
                    typeCs = "ulong";
                    break;
                case "uint16":
                    typeCs = "ushort";
                    break;
                default:
                    typeCs = type;
                    break;
            }

            return typeCs;
        }

        private void Members(StringBuilder sb, string newline, bool isRequired)
        {
            try
            {
                int index = newline.IndexOf(";");
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[0];
                string name = ss[1];
                string typeCs = ConvertType(type);

                sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}\n\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }
    }
}