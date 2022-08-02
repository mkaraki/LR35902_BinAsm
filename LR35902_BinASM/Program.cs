Console.Write("Opcode (hex) > ");

string str_instr = string.Empty;

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

for (int i = 0; i < instr.Count;)
{
    var asmi = LR35902_BinASM.AsmInstr.GetAsmInstr(instr.Skip(i).ToArray(), out int skip);
    i += skip;

    Console.WriteLine(asmi);
}