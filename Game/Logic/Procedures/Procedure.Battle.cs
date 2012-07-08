#region

using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Data;
using Game.Data.Troop;
using System.Linq;
#endregion

namespace Game.Logic.Procedures
{
    public partial class Procedure
    {
        public virtual void MoveUnitFormation(ITroopStub stub, FormationType source, FormationType target)
        {
            stub[target].Add(stub[source]);
            stub[source].Clear();
        }

        public virtual void AddLocalToBattle(IBattleManager bm, ICity city, ReportState state)
        {
            if (city.DefaultTroop[FormationType.Normal].Count == 0)
                return;

            var list = new List<ITroopStub>(1) {city.DefaultTroop};

            city.DefaultTroop.BeginUpdate();
            city.DefaultTroop.State = TroopState.Battle;
            city.DefaultTroop.Template.LoadStats(TroopBattleGroup.Local);
            bm.AddToLocal(list, state);
            MoveUnitFormation(city.DefaultTroop, FormationType.Normal, FormationType.InBattle);
            city.DefaultTroop.EndUpdate();
        }

        /// <summary>
        /// Repairs all structures up to max HP but depends on percentage from sense of urgency effect
        /// </summary>
        /// <param name="city"></param>
        /// <param name="maxHp"></param>
        internal virtual void SenseOfUrgency(ICity city, uint maxHp)
        {
            // Prevent overflow, just to be safe
            maxHp = Math.Min(50000, maxHp);

            int healPercent = Math.Min(100, city.Technologies.GetEffects(EffectCode.SenseOfUrgency, EffectInheritance.All).Sum(x => (int)x.Value[0]));

            if (healPercent == 0)
                return;

            ushort restore = (ushort)(maxHp * (healPercent / 100f));

            foreach (IStructure structure in city) {
                if (structure.State.Type == ObjectState.Battle || structure.Stats.Hp == structure.Stats.Base.Battle.MaxHp)
                    continue;

                structure.BeginUpdate();
                structure.Stats.Hp += restore;
                structure.EndUpdate();
            }
        }
    }
}