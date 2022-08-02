// Based on :
// - https://www.pastraiser.com/cpu/gameboy/gameboy_opcodes.html
// - http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf

using static LR35902_BinASM.Convert;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;
using JC = LR35902_BinASM.JumpCondition;
using static LR35902_BinASM.RSTp;

namespace LR35902_BinASM
{
    public partial struct AsmInstr
    {
        /// <summary>
        /// Get AsmInstr Object from binary.
        /// </summary>
        /// <param name="instr">Instraction binary (max 3byte)</param>
        /// <param name="skip">Bytes read</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="Exception"></exception>
        public static AsmInstr GetAsmInstr(byte[] instr, out int skip)
        {
            if (instr.Length < 0) throw new ArgumentException("Instruction bytes must be larger than 1");

            // Default skip byte is 1 byte, 8bit.
            skip = 1;

            byte opcode = instr[0];
            byte first4bit_opcode = (byte)(opcode >> 4);
            byte last4bit_opcode = (byte)((byte)(opcode << 4) >> 4);


            // 0xCB
            if (opcode == 0xCB)
            {
                AsmInstr cbd = new(CB)
                {
                    CBInstr = LR35902_BinASM.CBInstr.GetCBInstr(instr.Skip(1).ToArray(), out int addskip)
                };
                skip += addskip;
                return cbd;
            }


            // (0x00 ~ 0x3F)
            if (opcode >> 6 == 0b00)
            {
                // 0x00 NOP
                if (opcode == 0x00)
                    return new(NOP);

                // 0x10 STOP
                if (opcode == 0x10)
                {
                    skip += 1;
                    return new(STOP, instr[1]);
                }

                // 0x20, 0x30, 0x28, 0x38 (JR)
                if (first4bit_opcode >> 1 << 1 == 0b0010 // 0x2*, 0x3*
                    && (last4bit_opcode == 0 || last4bit_opcode == 0x8))
                {
                    skip += 1;
                    return new(JR,
                        ((byte)(opcode << 3) >> 6) switch
                        {
                            0b00 => JC.NZ,
                            0b10 => JC.NC,
                            0b01 => JC.Z,
                            0b11 => JC.C,
                            _ => throw new NotImplementedException(),
                        },
                        instr[1]
                    );
                }

                // 0x[0-3]1 LD (d16 imm)
                if (last4bit_opcode == 0x1)
                {
                    skip += 2;
                    return new(
                        LD,
                        Get16BitRegisterFrom2BitInstr((byte)(first4bit_opcode << 6 >> 6)),
                        LE2BytesToUInt16(instr.Skip(1))
                        );
                }
                // 0x08 LD a16, SP
                else if (opcode == 0x08)
                {
                    skip += 2;
                    return new(
                        LD,
                        SP,
                        LE2BytesToUInt16(instr.Skip(1)),
                        true // This LD a16, SP will write to a16, so Operand first.
                        );
                }

                // 0x6 LD (d8 imm)
                if (last4bit_opcode == 0x6)
                {
                    skip += 1;
                    return new(
                        LD,
                        ((byte)(first4bit_opcode << 6) >> 6) switch
                        {
                            0 => B,
                            1 => D,
                            2 => H,
                            3 => HL,
                            _ => throw new NotImplementedException(),
                        },
                        instr[1]
                        );
                }
                else if (last4bit_opcode == 0xE)
                {
                    skip += 1;
                    return new(
                        LD,
                        ((byte)(first4bit_opcode << 6) >> 6) switch
                        {
                            0 => C,
                            1 => E,
                            2 => L,
                            3 => A,
                            _ => throw new NotImplementedException(),
                        },
                        instr[1]
                        );
                }

                // 0x9 ADD
                if (last4bit_opcode == 0x9)
                {
                    return new(ADD,
                        HL,
                        Get16BitRegisterFrom2BitInstr(first4bit_opcode)
                        );
                }

                // 0x2, LD
                if (last4bit_opcode == 0x2)
                {
                    return new(LD,
                        Get16BitRegisterFrom2BitInstr(first4bit_opcode, HLMinus, HLPlus),
                        A);
                }
                // 0xA, LD A
                else if (last4bit_opcode == 0xA)
                {
                    return new(LD,
                        A,
                        Get16BitRegisterFrom2BitInstr(first4bit_opcode, HLMinus, HLPlus)
                        );
                }

                if ((byte)(last4bit_opcode << 5) >> 5 == 0b011) // 0x3 or 0xB (INC, DEC)
                {
                    return new(
                        last4bit_opcode == 0b0011 ? INC : DEC,
                        Get16BitRegisterFrom2BitInstr(first4bit_opcode)
                        );
                }
                if ((byte)(last4bit_opcode >> 1 << 6) >> 5 == 0b0100) // 0x4,5,c,d (INC, DEC)
                {
                    return new(
                        last4bit_opcode << 7 >> 7 == 0b1 ? DEC : INC, // If ends with 1, it's DEC
                        GetRegisterFromLast3Bits(opcode, 2, 5)
                        );
                }

                // 0x18, JR r8
                if (opcode == 0x18)
                {
                    skip += 1;
                    return new(
                        JR,
                        instr[1]
                        );
                }

                if (opcode == 0x07) return new(RLCA);
                if (opcode == 0x17) return new(RLA);
                if (opcode == 0x27) return new(DAA);
                if (opcode == 0x37) return new(SCF);

                if (opcode == 0x0F) return new(RRCA);
                if (opcode == 0x1F) return new(RRA);
                if (opcode == 0x2F) return new(CPL);
                if (opcode == 0x3F) return new(CCF);
            }
            // (0x40 ~ 0x7F) LD or HALT(0x76)
            // opcode (8bit)の上位2bitが`0b01`であれば`0x40`~`0x7F`と判定
            else if (opcode >> 6 == 0b01)
            {
                if (opcode == 0x76)
                    return new(HALT);
                else
                {
                    // Get destination register from opcode 5:2
                    Register dest_reg = GetRegisterFromLast3Bits(opcode, 2, 5);

                    return new(LD, dest_reg, GetRegisterFromLast3Bits(opcode));
                }
            }
            // (0x80 ~ 0xBF) ADD,SUB,AND,OR
            else if (opcode >> 6 == 0b10)
            {
                Instruction instrc = ((byte)(opcode << 2) >> 5) switch
                {
                    // 0b aab
                    // aa: opcode 5:4
                    // b : opcode 3:3
                    0b000 => ADD,
                    0b001 => ADC,
                    0b010 => SUB,
                    0b011 => SBC,
                    0b100 => AND,
                    0b101 => XOR,
                    0b110 => OR,
                    0b111 => CP,
                    _ => throw new NotImplementedException(),
                };

                if (instrc == ADD || instrc == ADC || instrc == SBC)
                    return new(instrc, A, GetRegisterFromLast3Bits(opcode));
                else
                    return new(instrc, GetRegisterFromLast3Bits(opcode));
            }
            // (0xC0 ~ 0xFF)
            else if (opcode >> 6 == 0b11)
            {
                // 0x1, 0x5 PUSH, POP
                if (last4bit_opcode == 0x1 || last4bit_opcode == 0x5)
                {
                    return new(
                        last4bit_opcode == 0x1 ? POP : PUSH,
                        Get16BitRegisterFrom2BitInstr(first4bit_opcode, AF)
                        );
                }

                // 0x6 ADD,SUB,AND,OR
                if (last4bit_opcode == 0x6)
                {
                    skip += 1;
                    return new(
                        first4bit_opcode switch
                        {
                            0xC => ADD,
                            0xD => SUB,
                            0xE => AND,
                            0xF => OR,
                            _ => throw new NotImplementedException(),
                        },
                        first4bit_opcode == 0xC ? A : null,
                        instr[1]
                        );
                }

                // 0xC* 0xD*
                if (first4bit_opcode >> 1 == 0b110)
                {

                    // 0xC0 D0, RET NZ,NC
                    if (last4bit_opcode == 0)
                    {
                        skip += 2;
                        return new(
                            RET,
                            ((byte)(first4bit_opcode << 6) >> 6 == 0) ? JC.NZ : JC.NC
                            );
                    }

                    // 0xC8 D8, RET Z,C
                    if (last4bit_opcode == 0 || last4bit_opcode == 0x8)
                    {
                        skip += 2;
                        return new(
                            RET,
                            ((byte)(first4bit_opcode << 6) >> 6 == 0) ? JC.Z : JC.C
                            );
                    }

                    // 0xC2 D2 C4 D4, JP CALL
                    if (last4bit_opcode == 0x2 || last4bit_opcode == 0x4)
                    {
                        skip += 2;
                        return new(
                            last4bit_opcode == 0x2 ? JP : CALL,
                            ((byte)(first4bit_opcode << 6) >> 6 == 0) ? JC.NZ : JC.NC,
                            LE2BytesToUInt16(instr.Skip(1))
                            );
                    }

                    // 0xCA DC CA DC, JP CALLS
                    if (last4bit_opcode == 0xA || last4bit_opcode == 0xC)
                    {
                        skip += 2;
                        return new(
                            last4bit_opcode == 0xA ? JP : CALL,
                            ((byte)(first4bit_opcode << 6) >> 6 == 0) ? JC.Z : JC.C,
                            LE2BytesToUInt16(instr.Skip(1))
                            );
                    }


                    // 0xC9 RET
                    if (opcode == 0xC9) return new(RET);

                    // 0xD9 RETI
                    if (opcode == 0xD9) return new(RETI);
                }

                // 0xCD C3, CALL a16 | JP a16
                if (opcode == 0xCD || opcode == 0xC3)
                {
                    skip += 2;
                    return new(
                        last4bit_opcode == 0xD ? CALL : JP,
                        LE2BytesToUInt16(instr.Skip(1))
                        );
                }

                // 0x7, 0xF: RST
                if (last4bit_opcode == 0x7 || last4bit_opcode == 0xF)
                {
                    return new(
                        Instruction.RST,
                        ((byte)(opcode << 2) >> 5) switch
                        {
                            0b00_0 => p00H,
                            0b01_0 => p10H,
                            0b10_0 => p20H,
                            0b11_0 => p30H,
                            0b00_1 => p08H,
                            0b01_1 => p18H,
                            0b10_1 => p28H,
                            0b11_1 => p38H,
                            _ => throw new NotImplementedException(),
                        }
                        );
                }

                // 0xE0 F0, LDH
                if (opcode == 0xE0 || opcode == 0xF0)
                {
                    skip += 1;
                    return new(
                        LDH,
                        A,
                        instr[1],
                        opcode == 0xE0);
                }

                // 0xE2 F2, LD
                if (opcode == 0xE2 || opcode == 0xF2)
                {
                    skip += 1;
                    return new(
                        LD,
                        C,
                        instr[1],
                        opcode == 0xE0);
                }

                // 0xF3 DI
                if (opcode == 0xF3) return new(DI);

                // 0xFB EI
                if (opcode == 0xFB) return new(EI);

                // 0xEA FA, LD
                if (opcode == 0xEA || opcode == 0xFA)
                {
                    skip += 2;
                    return new(
                        LD,
                        A,
                        LE2BytesToUInt16(instr.Skip(1)),
                        opcode == 0xEA
                        );
                }

                // 0x[C-F]E : ADC SBC XOR CP
                if (last4bit_opcode == 0xE)
                {
                    skip += 1;
                    return new(((byte)(first4bit_opcode << 6) >> 6) switch
                    {
                        1 => ADC,
                        2 => SBC,
                        3 => XOR,
                        4 => CP,
                        _ => throw new NotImplementedException()
                    },
                        first4bit_opcode > 2 ? null : A,
                        instr[1]
                        );
                }

                if (opcode == 0xD8)
                {
                    skip += 1;
                    return new(ADD, SP, instr[1]);
                }

                if (opcode == 0xF8)
                {
                    skip += 1;
                    return new(LD, HL, instr[1], addSP: true);
                }

                if (opcode == 0xD9)
                    return new(JP, HL);

                if (opcode == 0xF9)
                    return new(LD, SP, HL);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// This will get Register from last 3 bit of opcode.
        /// It can use for `0x40` ~ `0xBF` (except `0x76`, HALT)
        /// 
        /// It also used in `0x40` ~ `0x7F` LD's destination calcurate.
        /// </summary>
        internal static Register GetRegisterFromLast3Bits(byte opcode, int first_shift_amount = 5, int back_shift_amount = 5)
        {
            return ((byte)(opcode << first_shift_amount) >> back_shift_amount) switch
            {
                0x0 => B,
                0x1 => C,
                0x2 => D,
                0x3 => E,
                0x4 => H,
                0x5 => L,
                0x6 => HL,
                0x7 => A,
                _ => throw new NotImplementedException(),
            };
        }

        private static Register Get16BitRegisterFrom2BitInstr(byte opcode, Register last = SP, Register last2 = HL)
        {
            opcode = (byte)((byte)(opcode << 6) >> 6);
            return opcode switch
            {
                0 => BC,
                1 => DE,
                2 => last2,
                3 => last,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
