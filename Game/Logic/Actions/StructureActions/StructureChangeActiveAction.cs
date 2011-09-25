#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Logic.Formulas;
using Game.Setup;
using Game.Util;
using Ninject;

#endregion

namespace Game.Logic.Actions
{
    class StructureChangeActiveAction : ScheduledActiveAction
    {
        private readonly uint cityId;
        private readonly byte lvl;
        private readonly uint structureId;
        private readonly uint type;
        private Resource cost;

        public StructureChangeActiveAction(uint cityId, uint structureId, uint type, byte lvl)
        {
            this.cityId = cityId;
            this.structureId = structureId;
            this.type = type;
            this.lvl = lvl;
        }

        public StructureChangeActiveAction(uint id,
                                     DateTime beginTime,
                                     DateTime nextTime,
                                     DateTime endTime,
                                     int workerType,
                                     byte workerIndex,
                                     ushort actionCount,
                                     IDictionary<string, string> properties) : base(id, beginTime, nextTime, endTime, workerType, workerIndex, actionCount)
        {
            cityId = uint.Parse(properties["city_id"]);
            structureId = uint.Parse(properties["structure_id"]);
            lvl = byte.Parse(properties["lvl"]);
            type = uint.Parse(properties["type"]);
            cost = new Resource(int.Parse(properties["crop"]),
                                int.Parse(properties["gold"]),
                                int.Parse(properties["iron"]),
                                int.Parse(properties["wood"]),
                                int.Parse(properties["labor"]));
        }

        public override ConcurrencyType Concurrency
        {
            get
            {
                return ConcurrencyType.StandAlone;
            }
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.StructureChangeActive;
            }
        }

        public override Error Validate(string[] parms)
        {
            if (type == uint.Parse(parms[0]) && lvl == uint.Parse(parms[1]))
                return Error.Ok;
            return Error.ActionInvalid;
        }

        public override Error Execute()
        {
            City city;
            Structure structure;
            if (!Global.World.TryGetObjects(cityId, structureId, out city, out structure))
                return Error.ObjectNotFound;

            cost = Formula.StructureCost(structure.City, type, lvl);
            if (cost == null)
                return Error.ObjectNotFound;

            if (!structure.City.Resource.HasEnough(cost))
                return Error.ResourceNotEnough;

            structure.City.BeginUpdate();
            structure.City.Resource.Subtract(cost);
            structure.City.EndUpdate();

            endTime = DateTime.UtcNow.AddSeconds(CalculateTime(Formula.BuildTime(Ioc.Kernel.Get<StructureFactory>().GetTime((ushort)type, lvl), city, structure.Technologies)));
            BeginTime = DateTime.UtcNow;

            return Error.Ok;
        }

        private void InterruptCatchAll(bool wasKilled)
        {
            City city;
            using (new MultiObjectLock(cityId, out city))
            {
                if (!IsValid())
                    return;

                if (!wasKilled)
                {
                    city.BeginUpdate();
                    city.Resource.Add(Formula.GetActionCancelResource(BeginTime, cost));
                    city.EndUpdate();
                }

                StateChange(ActionState.Failed);
            }
        }

        public override void UserCancelled()
        {
            InterruptCatchAll(false);
        }

        public override void WorkerRemoved(bool wasKilled)
        {
            InterruptCatchAll(wasKilled);
        }

        public override void Callback(object custom)
        {
            City city;
            Structure structure;

            // Block structure
            using (new MultiObjectLock(cityId, structureId, out city, out structure))
            {
                if (!IsValid())
                    return;

                structure.BeginUpdate();
                structure.IsBlocked = true;
                structure.EndUpdate();
            }

            structure.City.Worker.Remove(structure, new GameAction[] {this});

            using (new MultiObjectLock(cityId, out city))
            {
                if (!IsValid())
                    return;

                if (!city.TryGetStructure(structureId, out structure))
                {
                    StateChange(ActionState.Failed);
                    return;
                }

                structure.BeginUpdate();
                Procedures.Procedure.StructureChange(structure, (ushort)type, lvl);
                structure.EndUpdate();

                StateChange(ActionState.Completed);
            }
        }

        #region IPersistable

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                                                {
                                                        new XmlKvPair("type", type), new XmlKvPair("lvl", lvl), new XmlKvPair("city_id", cityId),
                                                        new XmlKvPair("structure_id", structureId), new XmlKvPair("wood", cost.Wood), new XmlKvPair("crop", cost.Crop),
                                                        new XmlKvPair("iron", cost.Iron), new XmlKvPair("gold", cost.Gold), new XmlKvPair("labor", cost.Labor),
                                                });
            }
        }

        #endregion
    }
}