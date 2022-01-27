using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace MobilePatcher
{
    public static class Program
    {
        internal static void Main()
        {
            Console.WriteLine("MobilePatcher v1.0");
            PatchWebSocketDll();
        }

        private static void PatchWebSocketDll()
        {
            if (!File.Exists("Discord.Net.WebSocket.dll"))
            {
                Console.WriteLine("Discord.Net.WebSocket.dll not found.");
                return;
            }

            ModuleDefMD module;
            try
            {
                module = ModuleDefMD.Load("Discord.Net.WebSocket.dll");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load Discord.Net.WebSocket.dll\n{e}");
                return;
            }

            var method = FindMethod(module, "Discord.API", "DiscordSocketApiClient", "MoveNext", "<SendIdentifyAsync>d__32")
                         ?? FindMethod(module, "Discord.API", "DiscordSocketApiClient", "MoveNext", "<SendIdentifyAsync>d__33");

            if (method is null)
            {
                Console.WriteLine("Could not find method SendIdentifyAsync.");
                return;
            }

            var instructions = method.Body.Instructions;
            if (instructions.Any(x => x.Operand is "android" or "darwin" or "Discord Android" or "Discord iOS"))
            {
                Console.WriteLine("Discord.Net.WebSocket.dll is already patched.");
                return;
            }

            Console.WriteLine("Patching Discord.Net.WebSocket.dll...");

            var setItemInstruction = instructions.FirstOrDefault(x =>
                x.OpCode == OpCodes.Callvirt &&
                x.Operand is MemberRef memberRef &&
                memberRef.Name == "set_Item" &&
                memberRef.Class.FullName == "System.Collections.Generic.Dictionary`2<System.String,System.String>");

            if (setItemInstruction is null)
            {
                Console.WriteLine("Method set_Item not found.");
                return;
            }

            int index = instructions.IndexOf(setItemInstruction);
            if (instructions[index - 1].Operand is not string strOperand || !strOperand.StartsWith("Discord.Net") || instructions[index - 2].Operand is not "$device")
            {
                Console.WriteLine("Method set_Item is not setting the expected values, something has changed in SendIdentifyAsync.");
                return;
            }

            instructions.Insert(++index, new Instruction(OpCodes.Dup));
            instructions.Insert(++index, new Instruction(OpCodes.Ldstr, "$os"));
            instructions.Insert(++index, new Instruction(OpCodes.Ldstr, "android"));
            instructions.Insert(++index, new Instruction(OpCodes.Callvirt, setItemInstruction.Operand));
            instructions.Insert(++index, new Instruction(OpCodes.Dup));
            instructions.Insert(++index, new Instruction(OpCodes.Ldstr, "$browser"));
            instructions.Insert(++index, new Instruction(OpCodes.Ldstr, "Discord Android"));
            instructions.Insert(++index, new Instruction(OpCodes.Callvirt, setItemInstruction.Operand));

            Console.WriteLine("Creating backup of Discord.Net.WebSocket.dll");
            try
            {
                File.Copy("Discord.Net.WebSocket.dll", "Discord.Net.WebSocket.dll.bak", true);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Failed to make a backup of Discord.Net.WebSocket.dll. Patching cancelled.\n{e}");
                return;
            }

            try
            {
                module.Write("Discord.Net.WebSocket.dll.mod");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to save patched Discord.Net.WebSocket.dll.mod\n{e}");
                return;
            }

            try
            {
                File.Move("Discord.Net.WebSocket.dll.mod", "Discord.Net.WebSocket.dll", true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to replace Discord.Net.WebSocket.dll with patched\n{e}");
                return;
            }
            finally
            {
                try
                {
                    File.Delete("Discord.Net.WebSocket.dll.mod");
                }
                catch { }
            }

            Console.WriteLine("Patched Discord.Net.WebSocket.dll successfully");
        }

        private static MethodDef FindMethod(ModuleDef module, string @namespace, string typeName, string methodName, string nestedTypeName = null)
        {
            foreach (var type in module.GetTypes())
            {
                if (type.Namespace != @namespace || type.Name != typeName) continue;

                TypeDef nestedType = null;
                if (nestedTypeName != null)
                {
                    foreach (var nType in type.NestedTypes)
                    {
                        if (nType.Name != nestedTypeName) continue;
                        nestedType = nType;
                    }
                }

                foreach (var method in (nestedType ?? type).Methods)
                {
                    if (method.Name == methodName)
                        return method;
                }
            }

            return null;
        }
    }
}