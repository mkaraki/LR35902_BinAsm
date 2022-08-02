using LR35902_BinASM;
using Mono.Options;
using System.Linq;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;
using Convert = System.Convert;

// ===== OPTIONS

bool boot_help = false;
string cnf_profile = "hw-gb";
bool cnf_continuous = false;
ushort cnf_counter_start_addr = 0;

ushort cnf_start_addr = 0x0;

string cnf_load_bin_file = string.Empty;

OptionSet opts = new OptionSet()
{
    { "h|help", "Show this help", v => boot_help = v != null },
    { "p|profile=", "Memory mapping profile", v => cnf_profile = v },
    { "b|binary=", "Load binary file", v => cnf_load_bin_file = v },
    { "c|continuous", "Continue after saw `RET`", v => cnf_continuous = v != null },
    { "a|start-address=", "Start address for address counter in hex (like DA00)", v => cnf_counter_start_addr = ushort.Parse(v, System.Globalization.NumberStyles.HexNumber) },
    { "f|from=", "Convert start address", v => cnf_start_addr = ushort.Parse(v, System.Globalization.NumberStyles.HexNumber) },
};
args = opts.Parse(args).ToArray();


if (boot_help)
{
    Console.WriteLine("LR35902 Bin Asm Converter");
    Console.WriteLine();

    opts.WriteOptionDescriptions(Console.Out);
    Environment.Exit(0);
}


// ===== MAIN PROGRAM

MapSearch map = new();


List<byte> instr = new();

if (cnf_load_bin_file == string.Empty)
{
    string str_instr = string.Empty;

    Console.Write("Opcodes (hex) > ");

    while (true)
    {
        var str = Console.ReadLine() ?? string.Empty;

        if (str == string.Empty || str == "end")
            break;

        str_instr += str.Trim();

        Console.Write("> ");
    }

    if (str_instr.Length < 2)
    {
        Console.Error.Write("Too short Opcode");
    }

    // Enter new line for pipe input
    Console.WriteLine();


    while (true)
    {
        if (str_instr.Length < 2) break;
        if (str_instr[0] == ' ' || str_instr[0] == ',' || str_instr[0] == '\t')
        {
            str_instr = str_instr.Substring(1);
            continue;
        }
        string two_dig_hex = str_instr.Trim().Substring(0, 2);
        instr.Add((byte)Convert.ToInt16(two_dig_hex.Trim(), 16));
        str_instr = str_instr.Substring(2);
    }
}
else if (File.Exists(cnf_load_bin_file))
{
    instr = File.ReadAllBytes(cnf_load_bin_file).ToList();
}
else
{
    Console.Error.WriteLine("Unable to find file");
    Environment.Exit(1);
}



Instruction[] memory_addr_comment_tgt = new Instruction[] { LD, JP, CALL };
Register[] memory_addr_comment_tgt_ld_reg = new Register[] { BC, DE, HL };

for (int i = (short)(cnf_start_addr - cnf_counter_start_addr); i < instr.Count;)
{
    var asmi = AsmInstr.GetAsmInstr(instr.Skip(i).ToArray(), out int skip);

    uint addrcnt = cnf_counter_start_addr + (uint)i;

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write(addrcnt.ToString("X").PadLeft(4, '0') + " \t");
    Console.ResetColor();

    string[] res = asmi.ToString().Split('\n');
    Console.Write(res[0]);
    if (asmi.Instruction == CB)
    {
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write((addrcnt + 1).ToString("X").PadLeft(4, '0') + " \t");
        Console.ResetColor();

        Console.Write(res[1]);
    }

    i += skip;

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