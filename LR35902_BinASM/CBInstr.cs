// Based on :
// - https://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LR35902_BinASM.Convert;
using static LR35902_BinASM.Instruction;

namespace LR35902_BinASM
{
    public struct CBInstr
    {

        public static CBInstr GetCBInstr(byte[] instr, out int skip)
        {
            if (instr.Length < 1) throw new ArgumentException("Instruction bytes must be larger than 1");

            // Default skip byte is 1 byte, 8bit.
            skip = 1;

            byte opcode = instr[0];
            byte last4bit_opcode = (byte)((byte)(opcode << 4) >> 4);

            Register reg = AsmInstr.GetRegisterFromLast3Bits(last4bit_opcode);

            switch (opcode >> 3)
            {
                case (0x0 << 1) + 0b0: return new(RLC, reg);
                case (0x0 << 1) + 0b1: return new(RRC, reg);
                case (0x1 << 1) + 0b0: return new(RL, reg);
                case (0x1 << 1) + 0b1: return new(RR, reg);
                case (0x2 << 1) + 0b0: return new(SLA, reg);
                case (0x2 << 1) + 0b1: return new(SRA, reg);
                case (0x3 << 1) + 0b0: return new(SWAP, reg);
                case (0x3 << 1) + 0b1: return new(SRL, reg);
            }

            ushort amount = (ushort)((byte)(opcode << 2) >> 5);

            if (opcode >= 0x40 && opcode < 0x80) return new(BIT, amount, reg);
            if (opcode >= 0x80 && opcode < 0xC0) return new(RES, amount, reg);
            if (opcode >= 0xC0 && opcode <= 0xFF) return new(SET, amount, reg);

            throw new NotImplementedException();
        }

        public CBInstr(Instruction instr, Register reg)
        {
            Instruction = instr;
            Register = reg;
        }

        public CBInstr(Instruction instr, ushort amount, Register reg) : this(instr, reg)
        {
            Amount = amount;
        }

        public Instruction Instruction { get; set; }

        public Register Register { get; set; }

        public ushort? Amount { get; set; } = null;

        public override string ToString()
        {
            string ret = Instruction.ToString();

            if (Amount != null)
                ret += "\t " + Amount;

            if (Register != null)
                ret += "\t " + Register.ToString();

            return ret;
        }

    }
}
