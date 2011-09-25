#region

using System;
using System.Linq;
using System.Collections.Generic;
using Game.Data;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Setup;
using Game.Util;
using Ninject;

#endregion

namespace Game.Logic.Actions
{
    class StructureUpgradeActiveAction : ScheduledActiveAction
    {
        private readonly uint cityId;
        private readonly uint structureId;
        private Resource cost;
        private ushort type;

        public StructureUpgradeActiveAction(uint cityId, uint structureId)
        {
            this.cityId = cityId;
            this.structureId = structureId;
        }

        public StructureUpgradeActiveAction(uint id,
                                      DateTime beginTime,
                                      DateTime nextTime,
                                      DateTime endTime,
                                      int workerType,
                                      byte workerIndex,
                                      ushort actionCount,
                                      Dictionary<string, string> properties)
            : base(id, beginTime, nextTime, endTime, workerType, workerIndex, actionCount)
        {
            cityId = uint.Parse(properties["city_id"]);
            structureId = uint.Parse(properties["structure_id"]);
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
                return ActionType.StructureUpgradeActive;
            }
        }

        public override Error Execute()
        {
            City city;
            Structure structure;

            if (!Global.World.TryGetObjects(cityId, structureId, out city, out structure))
                return Error.ObjectNotFound;

            if (!Ioc.Kernel.Get<ObjectTypeFactory>().IsStructureType("UnlimitedBuilding", type) &&
                    city.Worker.ActiveActions.Values.Count(
                                                           action =>
                                                           action.ActionId != ActionId &&
                                                           (action.Type == ActionType.StructureUpgradeActive ||
                                                            (action.Type == ActionType.StructureBuildActive &&
                                                             !Ioc.Kernel.Get<ObjectTypeFactory>().IsStructureType("UnlimitedBuilding",
                                                                                                ((StructureBuildActiveAction)action).BuildType)))) >= 2)
                return Error.ActionTotalMaxReached;

            cost = Formula.StructureCost(city, structure.Type, (byte)(structure.Lvl + 1));
            type = structure.Type;

            if (cost == null)
                return Error.ObjectStructureNotFound;

            // layout requirement
            if (
                    !Ioc.Kernel.Get<RequirementFactory>().GetLayoutRequirement(structure.Type, (byte)(structure.Lvl + 1)).Validate(structure,
                                                                                                                 structure.Type,
                                                                                                                 structure.X,
                                                                                                                 structure.Y))
                return Error.LayoutNotFullfilled;

            if (!structure.City.Resource.HasEnough(cost))
                return Error.ResourceNotEnough;

            structure.City.BeginUpdate();
            structure.City.Resource.Subtract(cost);
            structure.City.EndUpdate();

            endTime =
                    DateTime.UtcNow.AddSeconds(
                                               CalculateTime(Formula.BuildTime(Ioc.Kernel.Get<StructureFactory>().GetTime(structure.Type, (byte)(structure.Lvl + 1)),
                                                                               city,
                                                                               structure.Technologies)));
            BeginTime = DateTime.UtcNow;

            return Error.Ok;
        }

        public override void Callback(object custom)
        {
            City city;
            Structure structure;
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
                Ioc.Kernel.Get<StructureFactory>().GetUpgradedStructure(structure, structure.Type, (byte)(structure.Lvl + 1));
                Ioc.Kernel.Get<InitFactory>().InitGameObject(InitCondition.OnUpgrade, structure, structure.Type, structure.Lvl);
                structure.EndUpdate();

                Procedure.OnStructureUpgrade(structure);

                StateChange(ActionState.Completed);
            }
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        private void InterruptCatchAll(bool wasKilled)
        {
            City city;
            Structure structure;
            using (new MultiObjectLock(cityId, out city))
            {
                if (!IsValid())
                    return;

                if (!city.TryGetStructure(structureId, out structure))
                {
                    StateChange(ActionState.Failed);
                    return;
                }

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

        #region IPersistable

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                                                {
                                                        new XmlKvPair("city_id", cityId), new XmlKvPair("structure_id", structureId), new XmlKvPair("wood", cost.Wood),
                                                        new XmlKvPair("crop", cost.Crop), new XmlKvPair("iron", cost.Iron), new XmlKvPair("gold", cost.Gold),
                                                        new XmlKvPair("labor", cost.Labor)
                                                });
            }
        }

        #endregion
    }
}