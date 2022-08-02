using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LR35902_BinASM
{
    internal class Convert
    {
        /// <summary>
        /// Check mask's all of `1` bits are also `1` in `a`
        /// For 16 bit
        /// </summary>
        internal static bool AndBitEqual(UInt16 a, UInt16 mask)
            => (a & mask) == mask;

        /// <summary>
        /// Check mask's all of `1` bits are also `1` in `a`
        /// For 8 bit
        /// </summary>
        internal static bool AndBitEqual(byte a, byte mask)
            => (a & mask) == mask;

        internal static bool AndBitEqual(UInt16 a, params UInt16[] masks)
        { 
            foreach (var mask in masks)
                if (!AndBitEqual(a, mask))
                    return false;
            return true;
        }

        internal static bool AndBitEqual(byte a, params byte[] masks)
        {
            foreach (var mask in masks)
                if (!AndBitEqual(a, mask))
                    return false;
            return true;
        }

        internal static UInt16 LE2BytesToUInt16(IEnumerable<byte> bytes)
            => BitConverter.ToUInt16(bytes.Take(2).ToArray());
    }
}
