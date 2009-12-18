using System;
using System.Collections.Generic;
using System.Text;
using Game.Data;
using Game.Util;
using Game.Database;
using Game.Setup;

namespace Game.Logic.Actions {
    class CityAction : ScheduledPassiveAction {
        int INTERVAL = 1800;
        uint cityId;
        int laborRoundBeforeIncrements;
       
        public CityAction(uint cityId) {
            this.cityId = cityId;
        }

        public CityAction(ushort id, DateTime beginTime, DateTime nextTime, DateTime endTime, bool isVisible, Dictionary<string, string> properties)
            : base(id, beginTime, nextTime, endTime, isVisible) {
            cityId = uint.Parse(properties["city_id"]);
        }

        public override Error validate(string[] parms) {
            return Error.OK;
        }

        public override Error execute() {
            beginTime = DateTime.Now;
            endTime = DateTime.Now.AddSeconds(INTERVAL * Setup.Config.seconds_per_unit);
            return Error.OK;
        }

        public override void interrupt(ActionInterrupt state) {
            switch (state) {
                case ActionInterrupt.Abort:
                    Global.Scheduler.del(this);
                    break;
                case ActionInterrupt.KILLED:
                    Global.Scheduler.del(this);
                    break;
            }
        }

        public override ActionType Type {
            get { return ActionType.CITY; }
        }

        #region ISchedule Members
        public override void callback(object custom) {
            City city;
            using (new MultiObjectLock(cityId, out city)) {
                if (!isValid()) return;

                city.BeginUpdate();
/********************************** Pre Loop1 ****************************************/

                #region ResourceGet
                //Resource resource = new Resource(5*city.MainBuilding.Lvl,0,0,5*city.MainBuilding.Lvl);
                //Resource resource = new Resource(200, 200, 200, 200);
                Resource resource = new Resource();
                #endregion

                #region Repair
                ushort repairPower=0;
                #endregion

                #region Labor
                int laborTotal = 0;
                #endregion

 /*********************************** Loop1 *******************************************/
                foreach (Structure structure in city) {
                    #region ResourceGet
                    
                    #endregion

                    #region Repair
                    if (ObjectTypeFactory.IsStructureType("RepairBuilding", structure)) {
                        repairPower += (ushort)(structure.Stats.Base.Lvl * (50 + city.MainBuilding.Stats.Base.Lvl * 10));
                    }
                    #endregion

                    #region Labor
                    if (structure.Stats.Labor > 0) {
                        laborTotal += structure.Stats.Labor;
                    }
                    #endregion
                }

/********************************* Post Loop1 ****************************************/
                #region ResourceGet

                Resource Upkeep = city.Troops.Upkeep();
                if (Config.resource_upkeep) {
                    if (city.Resource.Crop.Value + resource.Crop < Upkeep.Crop) {
                        Upkeep.Crop = city.Resource.Crop.Value + resource.Crop;
                        city.Troops.Starve();
                    }
                } else {
                    Upkeep.Clear();
                }
                if (Config.resource_fast_income) {
                    resource += new Resource(500, 500, 500, 500,0);
                }
                city.Resource.Add(resource - Upkeep);
                #endregion

                #region Labor
                if (--laborRoundBeforeIncrements <= 0) {
                    city.Resource.Labor.Add(1);
                    laborTotal += city.Resource.Labor.Value;
                    if (laborTotal < 200) {
                        laborRoundBeforeIncrements = 1;
                    } else {
                        laborRoundBeforeIncrements = (int)(Math.Pow((laborTotal - 200 / 5), 2) / 500 + 1);
                    }
                    byte radius = Formula.GetRadius((uint)(laborTotal + city.Resource.Labor.Value));
                    if (radius > city.Radius) {
                        city.Radius = radius;
                    }
                } else {
                    laborTotal += city.Resource.Labor.Value;
                }
                
                #endregion

/********************************** Pre Loop2 ****************************************/
/*********************************** Loop2 *******************************************/
                foreach (Structure structure in city) {
                    #region Repair
                    if (repairPower > 0) {
                        if (structure.Stats.Battle.MaxHp > structure.Stats.Hp && 
                            !ObjectTypeFactory.IsStructureType("NonRepairable", structure) &&
                            structure.State.Type != ObjectState.BATTLE) {
                            if ((structure.Stats.Hp += repairPower) > structure.Stats.Battle.MaxHp) structure.Stats.Hp = structure.Stats.Battle.MaxHp;
                        }
                    }
                    #endregion

                    Global.dbManager.Save(structure);
                }
/********************************* Post Loop2 ****************************************/
                city.EndUpdate();

                beginTime = DateTime.Now;
                endTime = DateTime.Now.AddSeconds(INTERVAL * Setup.Config.seconds_per_unit);
                stateChange(ActionState.FIRED);
            }
        }

        #endregion

        #region IPersistable Members

        public override string Properties {
            get {
                return XMLSerializer.Serialize(new XMLKVPair[] {
                        new XMLKVPair("city_id", cityId)
                    }
                );
            }
        }

        #endregion
    }
}
