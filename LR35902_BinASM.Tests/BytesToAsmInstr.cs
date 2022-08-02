// Based on : https://github.com/lmmendes/game-boy-opcodes (MIT License)

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static LR35902_BinASM.AsmInstr;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;

namespace LR35902_BinASM.Tests
{
    [TestClass]
    public class BytesToAsmInstr_Hmn
    {
        private static byte[] b(params byte[] b) => b;

        [TestMethod]
        public void NOP()
        {
            var e = GetAsmInstr(b(0x00), out int i);

            Assert.AreEqual(1, i);
            Assert.AreEqual(new AsmInstr() { 
                Instruction = Instruction.NOP,
            }, e);
        }

        [TestMethod]
        public void STOP_NoBrank()
        {
            var e = GetAsmInstr(b(0x10, 0xFF), out int i);

            Assert.AreEqual(1, i);
            Assert.AreEqual(new AsmInstr()
            {
                Instruction = STOP,
            }, e);
        }

        [TestMethod]
        public void STOP_Brank()
        {
            var e = GetAsmInstr(b(0x10, 0x00), out int i);

            Assert.AreEqual(2, i);
            Assert.AreEqual(new AsmInstr()
            {
                Instruction = STOP,
            }, e);
        }
    }
}