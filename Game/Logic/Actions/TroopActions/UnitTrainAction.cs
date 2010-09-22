#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Fighting;
using Game.Setup;
using Game.Util;

#endregion

namespace Game.Logic.Actions {
    class UnitTrainAction : ScheduledActiveAction {
        private ushort type;
        private uint cityId;
        private uint structureId;
        private Resource cost;
        private ushort count;

        public UnitTrainAction(uint cityId, uint structureId, ushort type, ushort count) {
            this.cityId = cityId;
            this.structureId = structureId;
            this.type = type;
            this.count = count;
        }

        public UnitTrainAction(uint id, DateTime beginTime, DateTime nextTime, DateTime endTime, int workerType,
                               byte workerIndex, ushort actionCount, Dictionary<string, string> properties)
            : base(id, beginTime, nextTime, endTime, workerType, workerIndex, actionCount) {
            type = ushort.Parse(properties["type"]);
            cityId = uint.Parse(properties["city_id"]);
            structureId = uint.Parse(properties["structure_id"]);
            cost = new Resource(int.Parse(properties["crop"]), int.Parse(properties["gold"]), int.Parse(properties["iron"]), int.Parse(properties["wood"]), int.Parse(properties["labor"]));
            count = ushort.Parse(properties["count"]);
        }

        #region IAction Members

        public override ActionType Type {
            get { return ActionType.UNIT_TRAIN; }
        }

        public override Error Execute() {
            City city;
            Structure structure;
            if (!Global.World.TryGetObjects(cityId, structureId, out city, out structure))
                return Error.OBJECT_STRUCTURE_NOT_FOUND;

            cost = Formula.UnitTrainCost(structure.City, type, structure.City.Template[type].Lvl);
            Resource totalCost = cost * count;
            ActionCount = (ushort)(count + count / Formula.GetXForOneCount(structure.Technologies));

            if (!structure.City.Resource.HasEnough(totalCost))
                return Error.RESOURCE_NOT_ENOUGH;

            structure.City.BeginUpdate();
            structure.City.Resource.Subtract(totalCost);
            structure.City.EndUpdate();

            int buildtime = Formula.TrainTime(UnitFactory.GetTime(type, 1), structure.Lvl, structure.Technologies);

            // add to queue for completion
            nextTime = DateTime.UtcNow.AddSeconds(Config.actions_instant_time ? 0.1 : buildtime);
            beginTime = DateTime.UtcNow;
            endTime = DateTime.UtcNow.AddSeconds(Config.actions_instant_time ? 3 * ActionCount : (double) buildtime * ActionCount);

            return Error.OK;
        }

        public override Error Validate(string[] parms) {
            if (ushort.Parse(parms[0]) != type)
                return Error.ACTION_INVALID;

            return Error.OK;
        }

        #endregion

        #region ISchedule Members

        public override void Callback(object custom) {
            City city;
            Structure structure;
            using (new MultiObjectLock(cityId, out city)) {
                if (!IsValid())
                    return;

                if (!city.TryGetStructure(structureId, out structure)) {
                    StateChange(ActionState.FAILED);
                    return;
                }

                structure.City.DefaultTroop.BeginUpdate();
                structure.City.DefaultTroop.AddUnit(city.HideNewUnits ? FormationType.GARRISON : FormationType.NORMAL, type, 1);
                structure.City.DefaultTroop.EndUpdate();

                --ActionCount;
                if (ActionCount == 0) {
                    StateChange(ActionState.COMPLETED);
                    return;
                }

                int buildtime = Formula.TrainTime(UnitFactory.GetTime(type, 1), structure.Lvl, structure.Technologies);
                nextTime = nextTime.AddSeconds(Config.actions_instant_time ? 0.1 : buildtime);
                StateChange(ActionState.RESCHEDULED);
            }
        }

        #endregion

        private void InterruptCatchAll(bool wasKilled) {
            City city;
            Structure structure;
            using (new MultiObjectLock(cityId, out city)) {
                if (!IsValid())
                    return;

                if (!city.TryGetStructure(structureId, out structure)) {
                    StateChange(ActionState.FAILED);
                    return;
                }

                if (!wasKilled) {
                    int totalcount = Math.Max(0, count-((count + count / Formula.GetXForOneCount(structure.Technologies))-ActionCount));
                    Resource totalCost = cost * totalcount;

                    structure.City.BeginUpdate();
                    structure.City.Resource.Add(Formula.GetActionCancelResource(BeginTime,totalCost));
                    structure.City.EndUpdate();
                }

                StateChange(ActionState.FAILED);
            }
        }

        public override void UserCancelled() {
            InterruptCatchAll(false);
        }

        public override void WorkerRemoved(bool wasKilled) {
            InterruptCatchAll(wasKilled);
        }

        #region IPersistable

        public override string Properties {
            get
            {
                return
                    XMLSerializer.Serialize(new[] {
                                                        new XMLKVPair("type", type), 
                                                        new XMLKVPair("city_id", cityId),
                                                        new XMLKVPair("structure_id", structureId),
                                                        new XMLKVPair("wood", cost.Wood),
                                                        new XMLKVPair("crop", cost.Crop),
                                                        new XMLKVPair("iron", cost.Iron),
                                                        new XMLKVPair("gold", cost.Gold),
                                                        new XMLKVPair("labor", cost.Labor),
                                                        new XMLKVPair("count",count),
                    });
            }
        }

        #endregion
    }
}