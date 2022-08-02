using Microsoft.VisualStudio.TestTools.UnitTesting;

using static LR35902_BinASM.AsmInstr;
using static LR35902_BinASM.Instruction;
using static LR35902_BinASM.Register;

namespace LR35902_BinASM.Tests
{
    [TestClass]
    public class BytesToAsmInstr
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
    }
}