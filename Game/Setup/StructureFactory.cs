#region

using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.Data.Stats;
using Game.Logic;
using Game.Util;

#endregion

namespace Game.Setup {
    public enum ClassId : ushort {
        RESOURCE = 50,
        STRUCTURE = 100,
        UNIT = 200,
    }

    public class StructureFactory {
        private static Dictionary<int, StructureBaseStats> dict;

        public static void Init(string filename) {
            if (dict != null)
                return;
            dict = new Dictionary<int, StructureBaseStats>();
            Init("Game\\Setup\\CSV\\structure.csv");
            using (
                CSVReader reader =
                    new CSVReader(
                        new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                ) {
                String[] toks;
                Dictionary<string, int> col = new Dictionary<string, int>();
                for (int i = 0; i < reader.Columns.Length; ++i)
                    col.Add(reader.Columns[i], i);
                while ((toks = reader.ReadRow()) != null) {
                    if (toks[0].Length <= 0)
                        continue;
                    Resource resource = new Resource(int.Parse(toks[col["Crop"]]), int.Parse(toks[col["Gold"]]),
                                                     int.Parse(toks[col["Iron"]]), int.Parse(toks[col["Wood"]]),
                                                     int.Parse(toks[col["Labor"]]));

                    BaseBattleStats stats = new BaseBattleStats(ushort.Parse(toks[col["Type"]]),
                                                                byte.Parse(toks[col["Lvl"]]),
                                                                (WeaponType)
                                                                Enum.Parse(typeof (WeaponType), toks[col["Weapon"]].ToUpper()),
                                                                (WeaponClass)
                                                                Enum.Parse(typeof( WeaponClass), toks[col["WpnClass"]].ToUpper()),
                                                                (ArmorType)
                                                                Enum.Parse(typeof(ArmorType), toks[col["Armor"]].ToUpper()),
                                                                (ArmorClass)
                                                                Enum.Parse(typeof(ArmorClass), toks[col["ArmrClass"]].ToUpper()),
                                                                ushort.Parse(toks[col["Hp"]]),
                                                                ushort.Parse(toks[col["Atk"]]),
                                                                ushort.Parse(toks[col["Def"]]),
                                                                byte.Parse(toks[col["Rng"]]),
                                                                byte.Parse(toks[col["Stl"]]),
                                                                byte.Parse(toks[col["Spd"]]), 
                                                                0,
                                                                0);

                    StructureBaseStats basestats = new StructureBaseStats(toks[col["Name"]],
                                                                          toks[col["SpriteClass"]],
                                                                          ushort.Parse(toks[col["Type"]]),
                                                                          byte.Parse(toks[col["Lvl"]]),
                                                                          byte.Parse(toks[col["Radius"]]),
                                                                          resource, 
                                                                          stats,
                                                                          byte.Parse(toks[col["MaxLabor"]]),
                                                                          int.Parse(toks[col["Time"]]),
                                                                          int.Parse(toks[col["Worker"]]),
                                                                          (ClassId)
                                                                          Enum.Parse(typeof (ClassId),
                                                                                     (toks[col["Class"]]), true));

                    Global.Logger.Info(string.Format("{0}:{1}",
                                                     int.Parse(toks[col["Type"]])*100 + int.Parse(toks[col["Lvl"]]),
                                                     toks[col["Name"]]));
                    dict[int.Parse(toks[col["Type"]])*100 + int.Parse(toks[col["Lvl"]])] = basestats;
                }
            }
        }

        public static Resource GetCost(int type, int lvl) {
            if (dict == null)
                return null;
            StructureBaseStats tmp;
            return dict.TryGetValue(type*100 + lvl, out tmp) ? new Resource(tmp.Cost) : null;
        }

        public static int GetTime(ushort type, byte lvl) {
            if (dict == null)
                return -1;
            StructureBaseStats tmp;
            if (dict.TryGetValue(type*100 + lvl, out tmp))
                return tmp.BuildTime;
            return -1;
        }

        public static Structure GetNewStructure(ushort type, byte lvl) {
            if (dict == null)
                return null;

            StructureBaseStats baseStats;
            if (dict.TryGetValue(type*100 + lvl, out baseStats)) {
                return new Structure(new StructureStats(baseStats));
            } else {
                throw new Exception(String.Format("Structure not found in csv type[%d] lvl[%d]!", type, lvl));
            }
        }

        public static void GetUpgradedStructure(Structure structure, ushort type, byte lvl) {
            if (dict == null)
                return;
            StructureBaseStats baseStats;
            if (dict.TryGetValue(type * 100 + lvl, out baseStats)) {
                //Calculate the different in MAXHP between the new and old structures and Add it to the current hp if the new one is greater.
                StructureStats oldStats = structure.Stats;

                ushort newHp = oldStats.Hp;
                if (baseStats.Battle.MaxHp > oldStats.Base.Battle.MaxHp) {
                    newHp = (ushort) (oldStats.Hp + (baseStats.Battle.MaxHp - oldStats.Base.Battle.MaxHp));
                } else if (newHp > baseStats.Battle.MaxHp) {
                    newHp = baseStats.Battle.MaxHp;
                }

                byte newLabor = oldStats.Labor;
                if (baseStats.MaxLabor > 0 && newLabor > baseStats.MaxLabor) {
                    newLabor = baseStats.MaxLabor;
                }

                structure.Stats = new StructureStats(baseStats) { Hp = newHp, Labor = newLabor };
            } else {
                throw new Exception(String.Format("Structure not found in csv type[%d] lvl[%d]!", type, lvl));
            }
            return;
        }

        public static int GetActionWorkerType(Structure structure) {
            if (dict == null)
                return 0;
            StructureBaseStats tmp;
            return dict.TryGetValue(structure.Type*100 + structure.Lvl, out tmp) ? tmp.WorkerId : 0;
        }

        public static string GetName(Structure structure)
        {
            if (dict == null)
                return null;
            StructureBaseStats tmp;
            return dict.TryGetValue(structure.Type*100 + structure.Lvl, out tmp) ? tmp.Name : null;
        }

        public static StructureBaseStats GetBaseStats(ushort type, byte lvl)
        {
            if (dict == null)
                return null;
            StructureBaseStats tmp;
            return dict.TryGetValue(type * 100 + lvl, out tmp) ? tmp : null;
        }
    }
}