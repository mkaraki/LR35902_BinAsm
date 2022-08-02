using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LR35902_BinASM
{
    public class MapSearch
    {
        private Dictionary<string, List<(UInt16, UInt16, string)>> store = new();

        private async Task AppendIntoStoreAsync(string id, string fname)
        {
            string fpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps", fname);

            if (!File.Exists(fpath)) throw new IOException("No map file");
            string[] lines = await File.ReadAllLinesAsync(fpath, Encoding.UTF8);

            if (!store.ContainsKey(id))
                store.Add(id, new());

            var map = store[id];

            foreach (var l in lines)
            {
                if (l.Trim() == string.Empty) continue;

                // Comment
                if (l.StartsWith("#")) continue;

                // Import Statement
                if (l.StartsWith("/="))
                {
                    string impfile = l.Substring(2).Trim();

                    await AppendIntoStoreAsync(id, impfile);
                }

                var cols = l.Split('\t');
                if (cols.Length != 3) continue;

                map.Add((
                    ushort.Parse(cols[0], System.Globalization.NumberStyles.HexNumber),
                    ushort.Parse(cols[1], System.Globalization.NumberStyles.HexNumber),
                    cols[2]
                    ));
            }
        }

        public async Task<string?> GetMapAsync(UInt16 addr, string type, string profile)
        {
            string fname = $"{type}/{profile}";

            if (!store.ContainsKey(fname)) // If profile not loaded
            {
                await AppendIntoStoreAsync(fname, fname + ".map");
            }

            var res = store[fname].Where(v => addr >= v.Item1 && addr <= v.Item2);
            if (!res.Any()) return null;
            else return res.OrderBy(v => v.Item2 - v.Item1).First().Item3; // Use smallest memory range
        }
    }
}
