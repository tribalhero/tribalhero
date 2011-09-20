#region

using System;
using System.Collections.Generic;
using System.IO;
using Game.Battle;
using Game.Data;
using Game.Data.Stats;
using Game.Util;
using System.Linq;
#endregion

namespace Game.Setup
{
    public class UnitModFactory
    {
        private static Dictionary<int, double> dict;

        public static void Init(string filename)
        {
            if (dict != null)
                return;
            dict = new Dictionary<int, double>();
            using (var reader = new CsvReader(new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))))
            {
                String[] toks;
                var col = new Dictionary<string, int>();
                for (int i = 0; i < reader.Columns.Length; ++i)
                {
                    if (reader.Columns[i].Length == 0)
                        continue;
                    col.Add(reader.Columns[i], i);
                }
                while ((toks = reader.ReadRow()) != null)
                {
                    if (toks[0].Length <= 0)
                        continue;
                    var type = int.Parse(toks[col["Type"]]);

                    for(int i =0; i<reader.Columns.Length;++i)
                    {
                        int target;
                        if(reader.Columns[i].Length==0 || !int.TryParse(reader.Columns[i],out target)) continue;
                        var value = double.Parse(toks[i]);
                        dict[type*1000 + target] = value;
                    }
                }
            }
        }

        public static double GetModifier(int type, int target)
        {
            
            return dict[type * 1000 + target];
        }
    }
}