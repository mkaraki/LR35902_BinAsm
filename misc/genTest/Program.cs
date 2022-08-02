// Based on:
// - https://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html

using System.Text.Json;

Console.WriteLine(@"
// Based on : https://github.com/lmmendes/game-boy-opcodes (MIT License)

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static LR35902_BinASM.AsmInstr;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;
using JC = LR35902_BinASM.JumpCondition;
using static LR35902_BinASM.RSTp;

namespace LR35902_BinASM.Tests
{
    [TestClass]
    public class BytesToAsmInstr
    {
        private static byte[] b(params byte[] b) => b;
");

string template = @"
        [TestMethod]
        public void %VALID_INSTR%_%VALID_REGS%()
        {
            var e = GetAsmInstr(b(%BYTE1%, %BYTE2%, %BYTE3%), out int i);

            Assert.AreEqual(%VALID_SKIP%, i);
            Assert.AreEqual(new AsmInstr() { 
                Instruction = Instruction.%VALID_INSTR%,
                %OPT%
            }, e);
        }";

string pre_byte2 = "0x12";
string pre_byte3 = "0x34";

uint abyte_8bit = 0x12;
uint abyte_16bit = 0x3412;

string[] av_regs = new string[] { "A", "F", "B", "C", "D", "E", "H", "L", "AF", "BC", "DE", "HL", "HL+", "HL-", "SP", "PC" };
string[] av_imm = new string[] { "d8", "d16", "a8", "a16", "r8" };
string[] av_imm16b = new string[] { "d16", "a16" };
string[] av_jc = new string[] { "NZ", "Z", "NC", "C" };

using (JsonDocument opcodejson = JsonDocument.Parse(
    File.ReadAllText("opcode-json/opcodes.json", System.Text.Encoding.UTF8).Replace("\r\n", "").Replace("\n", "")
    ))
{
    for (int i = 0; i <= 0xFF; i++)
    {
        // TODO: CB Support
        if (i == 0xCB) continue;
 
        string byte1 = "0x" + i.ToString("x").PadLeft(2, '0');

        if (!opcodejson.RootElement.GetProperty("unprefixed").TryGetProperty(byte1, out var opc))
            continue;
        
        string optstr = "";

        int vskip = 1;

        List<string> regs = new();

        string name_reg = string.Empty;

        for (int opi = 1; opi <= 2; opi++)
        {
            if (opc.TryGetProperty($"operand{opi}", out JsonElement op1))
            {
                string opn = op1.GetRawText().Trim('"');
                string opn_reg_eval = opn.TrimStart('(').TrimEnd(')');
                if (av_regs.Contains(opn_reg_eval))
                {
                    string src_friendly_reg_name = opn.Replace("+", "Plus").Replace("-", "Minus");

                    if (opn.StartsWith('(')) src_friendly_reg_name = 'Q' + src_friendly_reg_name.Trim('(', ')');

                    regs.Add(src_friendly_reg_name);

                    name_reg += $"{src_friendly_reg_name}_";
                }
                else if (av_imm.Contains(opn_reg_eval))
                {
                    if (av_imm16b.Contains(opn_reg_eval))
                    {
                        optstr += $"\nOperand = {abyte_16bit},";
                        vskip += 2;
                    }
                    else
                    {
                        optstr += $"\nOperand = {abyte_8bit},";
                        vskip += 1;
                    }

                    if (opi == 1)
                        optstr += "\nOperandFirst = true,";

                    name_reg += $"{opn_reg_eval}_";
                }
                else if (av_jc.Contains(opn))
                {
                    optstr += $"\nJumpCondition = JC.{opn},";
                    name_reg += $"{opn}_";
                }
                else if (opn.EndsWith("H"))
                {
                    optstr += $"\nRST = p{opn},";
                    name_reg += $"{opn}_";
                }
            }
        }

        for (int regi = 0; regi < 2 && regi < regs.Count; regi++)
        {
            optstr += '\n' + $"Register{regi + 1} = {regs[regi]},";
        }


        string work = template
            .Replace("%BYTE1%", byte1)
            .Replace("%BYTE2%", pre_byte2)
            .Replace("%BYTE3%", pre_byte3)
            .Replace("%VALID_INSTR%", opc.GetProperty("mnemonic").GetRawText().Trim('"'))
            .Replace("%VALID_REGS%", name_reg)
            .Replace("%VALID_SKIP%", vskip.ToString())
            .Replace("%OPT%", optstr)
            ;

        Console.WriteLine(work);
    }
}

Console.WriteLine(@"
    }
}");