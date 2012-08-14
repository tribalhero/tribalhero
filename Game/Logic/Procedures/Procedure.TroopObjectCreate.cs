#region

using System.Collections.Generic;
using System.Linq;

using Game.Data;
using Game.Data.Stats;
using Game.Data.Troop;
using Game.Logic.Formulas;
using Game.Map;

#endregion

namespace Game.Logic.Procedures
{
    public partial class Procedure
    {
        public virtual bool TroopStubCreate(out ITroopStub troopStub, ICity city, ISimpleStub stub, TroopState initialState = TroopState.Idle, params FormationType[] formations)
        {
            if (!RemoveFromNormal(city.DefaultTroop, stub, formations))
            {
                troopStub = null;
                return false;
            }
            troopStub = city.Troops.Create();
            troopStub.BeginUpdate();
            troopStub.Add(stub);
            troopStub.State = initialState;
            troopStub.EndUpdate();
            return true;
        }

        public virtual void TroopStubDelete(ICity city, ITroopStub stub)
        {
            AddToNormal(stub, city.DefaultTroop);
            city.Troops.Remove(stub.TroopId);
        }

        public virtual void TroopObjectCreate(ICity city, ITroopStub stub, out ITroopObject troopObject)
        {
            troopObject = new TroopObject(stub) { X = city.X, Y = city.Y };
            city.Add(troopObject);

            troopObject.BeginUpdate();
            troopObject.Stats = new TroopStats(Formula.Current.GetTroopRadius(stub, null), Formula.Current.GetTroopSpeed(stub));
            World.Current.Add(troopObject);
            troopObject.EndUpdate();            
        }

        public virtual bool TroopObjectCreateFromCity(ICity city, ISimpleStub stub, uint x, uint y, out ITroopObject troopObject)
        {            
            if (stub.TotalCount == 0 || !RemoveFromNormal(city.DefaultTroop, stub))
            {
                troopObject = null;
                return false;
            }

            var troopStub = city.Troops.Create();
            troopStub.BeginUpdate();
            troopStub.Add(stub);
            troopStub.EndUpdate();

            troopObject = new TroopObject(troopStub) { X = x, Y = y + 1 };
            city.Add(troopObject);

            troopObject.BeginUpdate();
            troopObject.Stats = new TroopStats(Formula.Current.GetTroopRadius(troopStub, null), Formula.Current.GetTroopSpeed(troopStub));
            World.Current.Add(troopObject);
            troopObject.EndUpdate();

            return true;
        }

        private bool RemoveFromNormal(ITroopStub source, IEnumerable<Formation> unitsToRemove, params FormationType[] formations)
        {
            if (!source.HasFormation(FormationType.Normal))
            {
                return false;
            }

            var acceptableUnits = unitsToRemove.Where(formation => formations == null || formations.Length == 0 || formations.Contains(formation.Type)).ToList();

            // Make sure there are enough units
            foreach (var unit in acceptableUnits.SelectMany(formation => formation))
            {
                ushort count;
                if (!source[FormationType.Normal].TryGetValue(unit.Key, out count) || count < unit.Value)
                {
                    return false;
                }
            }

            // Remove them, shouldnt fail since we've already checked
            source.BeginUpdate();
            foreach (var unit in acceptableUnits.SelectMany(formation => formation))
            {
                source[FormationType.Normal].Remove(unit.Key, unit.Value);
            }
            source.EndUpdate();

            return true;
        }
    }
}