// Based on :
// - https://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html
// - http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf

namespace LR35902_BinASM
{
    public partial struct AsmInstr
    {
        public AsmInstr(Instruction instruction)
        {
            Instruction = instruction;
        }

        public AsmInstr(Instruction instruction, RSTp rst) : this(instruction)
        {
            RST = rst;
        }

        public AsmInstr(Instruction instruction, Register? reg1) : this(instruction)
        {
            Register1 = reg1;
        }

        public AsmInstr(Instruction instruction, Register reg1, Register reg2) : this(instruction, reg1)
        {
            Register2 = reg2;
        }

        public AsmInstr(Instruction instruction, UInt16 operand) : this(instruction)
        { 
            Operand = operand;
        }

        public AsmInstr(Instruction instruction, Register? reg1, UInt16 operand, bool operandFirst = false, bool addSP = false) : this(instruction, reg1)
        {
            Operand = operand;
            OperandFirst = operandFirst;
            AddSP = addSP;
        }

        public AsmInstr(Instruction instruction, JumpCondition jumpCondition) : this(instruction)
        {
            JumpCondition = jumpCondition;
        }

        public AsmInstr(Instruction instruction, JumpCondition jumpCondition, UInt16 operand) : this(instruction, operand)
        {
            JumpCondition = jumpCondition;
        }
        
        public Instruction Instruction { get; set; }

        /// <summary>
        /// This is for immediate or address value.
        /// </summary>
        public UInt16? Operand { get; set; } = null;

        public bool OperandFirst { get; set; } = false;

        public bool AddSP { get; set; } = false;

        public Register? Register1 { get; set; } = null;

        public Register? Register2 { get; set; } = null;

        public JumpCondition? JumpCondition { get; set; } = null;

        public RSTp? RST { get; set; } = null;

        public CBInstr? CBInstr { get; set; } = null;

        public override string ToString()
        {
            string ret = Instruction.ToString();

            if (JumpCondition != null)
                ret += "\t " + JumpCondition.ToString();

            if (OperandFirst)
                if (Operand != null)
                    ret += "\t 0x" + Operand.Value.ToString("X");

            if (Register1 != null)
                ret += "\t " + GetStringRegisterName(Register1.Value);

            if (Register2 != null)
                ret += "\t " + GetStringRegisterName(Register2.Value);

            if (!OperandFirst)
                if (Operand != null)
                {
                    ret += "\t ";
                    if (AddSP) ret += "SP + ";
                    ret += "0x" + Operand.Value.ToString("X");
                }

            if (RST != null)
                ret += "\t " + RST.Value.ToString().Substring(1);

            if (CBInstr != null)
                ret += Environment.NewLine + CBInstr.ToString();

            return ret;
        }

        private static string GetStringRegisterName(Register reg)
        {
            return reg.ToString()
                .Replace("HLPlus", "HL+")
                .Replace("HLMinus", "HL-")
                ;
        }
    }

    public enum Instruction
    { 
        NOP,
        STOP,
        JR,
        LD,
        ADD,
        SUB,
        AND,
        OR,
        RET,
        LDH,
        POP,
        JP,
        INC,
        DEC,
        DI,
        CALL,
        PUSH,
        HALT,
        RLCA,
        RLA,
        DAA,
        SCF,
        RST,
        ADC,
        SBC,
        XOR,
        CP,
        RETI,
        CB, // Prefix CB
        EI,
        RRCA,
        RRA,
        CPL,
        CCF,
        //  === CB
        RLC,
        RRC,
        RL,
        RR,
        SLA,
        SRA,
        SWAP,
        SRL,
        BIT,
        RES,
        SET,
    }

    public enum Register {
        // Based on:
        // - http://my-web-site.iobb.net/~yuki/2017-06/mpu/8080op/
        A,
        F, // Flag
        B,
        C,
        D,
        E,
        H,
        L,
        BC,
        DE,
        HL,
        SP, // Stack Pointer
        PC, // Program Counter
        AF,
        HLPlus,
        HLMinus
    }

    public enum JumpCondition {
        // Based on:
        // - http://www.yamamo10.jp/yamamoto/comp/Z80/instructions/index.php
        C,
        NC,
        Z,
        NZ
    }

    public enum RSTp { 
        p00H,
        p08H,
        p10H,
        p18H,
        p20H,
        p28H,
        p30H,
        p38H,
    }
}
