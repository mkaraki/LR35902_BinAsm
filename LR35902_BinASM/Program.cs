using LR35902_BinASM;
using Mono.Options;
using System.Linq;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;
using Convert = System.Convert;

// ===== OPTIONS

string cnf_profile = "hw-gb";
bool cnf_continuous = false;

OptionSet opts = new OptionSet()
{
    { "p|profile=", "Mapping Profile", v => cnf_profile = v },
    { "c|continuous", "Continue after saw `RET`", v => cnf_continuous = v != null }
};
args = opts.Parse(args).ToArray();


// ===== MAIN PROGRAM

Console.Write("Opcodes (hex) > ");

string str_instr = string.Empty;

LR35902_BinASM.MapSearch map = new();

while (true)
{
    var str = Console.ReadLine() ?? string.Empty;

    if (str == string.Empty || str == "end")
        break;

    str_instr += str.Trim();
}

if (str_instr.Length < 2)
{
    Console.Error.Write("Too short Opcode");
}

List<byte> instr = new();

while (true)
{
    if (str_instr.Length < 2) break;
    if (str_instr[0] == ' ' || str_instr[0] == ',' || str_instr[0] == '\t') str_instr = str_instr.Substring(1);
    string two_dig_hex = str_instr.Substring(0, 2);
    instr.Add((byte)Convert.ToInt16(two_dig_hex, 16));
    str_instr = str_instr.Substring(2);
}

Instruction[] memory_addr_comment_tgt = new Instruction[] { LD, JP, CALL };
Register[] memory_addr_comment_tgt_ld_reg = new Register[] { BC, DE, HL };

for (int i = 0; i < instr.Count;)
{
    var asmi = AsmInstr.GetAsmInstr(instr.Skip(i).ToArray(), out int skip);
    i += skip;

    Console.Write(asmi);

    if (asmi.Instruction == RET && asmi.Equals(new AsmInstr(RET)))
    {
        if (cnf_continuous)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" \t; Detected `RET`");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("END - Detected `RET`");
            Console.ResetColor();
            break;
        }
    }

    if (
        memory_addr_comment_tgt.Contains(asmi.Instruction) &&
        asmi.Operand.HasValue &&
        (asmi.Instruction != LD || Array.IndexOf(memory_addr_comment_tgt_ld_reg, asmi.Register1) != -1)
        )
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string message = await map.GetMapAsync(asmi.Operand.Value, "memory", cnf_profile);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        Console.ForegroundColor = ConsoleColor.Green;
        if (message != null) Console.Write(" \t; " + message);
        Console.ResetColor();
    }

    Console.WriteLine();
}