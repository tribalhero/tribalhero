#region

using System.Collections.Generic;
using Game.Data;
using Game.Data.Troop;
using Game.Fighting;

#endregion

namespace Game.Logic.Procedures {
    public partial class Procedure {
        public static bool TroopObjectDelete(TroopObject troop) {
            return TroopObjectDelete(troop, true);
        }

        public static bool TroopObjectDelete(TroopObject troop, bool addBackToNormal) {
            if (addBackToNormal) {
                AddToNormal(troop.Stub, troop.City.DefaultTroop);

                troop.City.BeginUpdate();
                troop.City.Resource.Add(troop.Stats.Loot);
                troop.City.EndUpdate();
            }

            troop.City.Troops.Remove(troop.Stub.TroopId);
            Global.World.Remove(troop);
            troop.City.Remove(troop);

            return true;
        }

        private static void AddToNormal(TroopStub source, TroopStub target) {
            target.BeginUpdate();
            foreach (Formation formation in source) {
                foreach (KeyValuePair<ushort, ushort> unit in formation)
                    target.AddUnit(FormationType.NORMAL, unit.Key, unit.Value);
            }
            target.EndUpdate();
        }
    }
}