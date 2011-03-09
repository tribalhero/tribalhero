#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle;
using Game.Data;
using Game.Data.Troop;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Setup;
using Game.Util;

#endregion

namespace Game.Logic.Actions
{
    class EngageAttackPassiveAction : PassiveAction
    {
        private readonly Resource bonus;
        private readonly uint cityId;
        private readonly AttackMode mode;
        private readonly byte stubId;
        private readonly uint targetCityId;
        private int originalUnitCount;
        private int remainingUnitCount;

        public EngageAttackPassiveAction(uint cityId, byte stubId, uint targetCityId, AttackMode mode)
        {
            this.cityId = cityId;
            this.stubId = stubId;
            this.targetCityId = targetCityId;
            this.mode = mode;
            bonus = new Resource();
        }

        public EngageAttackPassiveAction(uint id, bool isVisible, IDictionary<string, string> properties) : base(id, isVisible)
        {
            cityId = uint.Parse(properties["troop_city_id"]);
            stubId = byte.Parse(properties["troop_id"]);

            mode = (AttackMode)(byte.Parse(properties["mode"]));
            originalUnitCount = int.Parse(properties["original_count"]);

            targetCityId = uint.Parse(properties["target_city_id"]);

            bonus = new Resource(int.Parse(properties["crop"]),
                                 int.Parse(properties["gold"]),
                                 int.Parse(properties["iron"]),
                                 int.Parse(properties["wood"]),
                                 int.Parse(properties["labor"]));
            City targetCity;
            Global.World.TryGetObjects(targetCityId, out targetCity);
            RegisterBattleListeners(targetCity);
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.EngageAttackPassive;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                                                {
                                                        new XmlKvPair("target_city_id", targetCityId), new XmlKvPair("troop_city_id", cityId),
                                                        new XmlKvPair("troop_id", stubId), new XmlKvPair("mode", (byte)mode),
                                                        new XmlKvPair("original_count", originalUnitCount), new XmlKvPair("crop", bonus.Crop),
                                                        new XmlKvPair("gold", bonus.Gold), new XmlKvPair("iron", bonus.Iron), new XmlKvPair("wood", bonus.Wood),
                                                        new XmlKvPair("labor", bonus.Labor),
                                                });
            }
        }

        private void RegisterBattleListeners(City targetCity)
        {
            targetCity.Battle.ActionAttacked += BattleActionAttacked;
            targetCity.Battle.ExitBattle += BattleExitBattle;
            targetCity.Battle.WithdrawAttacker += BattleWithdrawAttacker;
            targetCity.Battle.EnterRound += BattleEnterRound;
            targetCity.Battle.ExitTurn += BattleExitTurn;
        }

        private void DeregisterBattleListeners(City targetCity)
        {
            targetCity.Battle.ActionAttacked -= BattleActionAttacked;
            targetCity.Battle.ExitBattle -= BattleExitBattle;
            targetCity.Battle.WithdrawAttacker -= BattleWithdrawAttacker;
            targetCity.Battle.EnterRound -= BattleEnterRound;
            targetCity.Battle.ExitTurn -= BattleExitTurn;
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        private static IEnumerable<Structure> GetStructuresInRadius(IEnumerable<Structure> structures, TroopObject obj)
        {
            return
                    structures.Where(
                                     structure =>
                                     SimpleGameObject.RadiusToPointFiveStyle(structure.RadiusDistance(obj)) <=
                                     SimpleGameObject.RadiusToPointFiveStyle(obj.Stats.AttackRadius) +
                                     SimpleGameObject.RadiusToPointFiveStyle(structure.Stats.Base.Radius));
        }

        public override Error Execute()
        {
            City city;
            City targetCity;
            TroopStub stub;

            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub) || !Global.World.TryGetObjects(targetCityId, out targetCity))
                return Error.ObjectNotFound;

            var list = new List<TroopStub> {stub};
            originalUnitCount = stub.TotalCount;

            if (targetCity.Battle != null)
            {
                RegisterBattleListeners(targetCity);

                Procedure.AddLocalToBattle(targetCity.Battle, targetCity, ReportState.Reinforced);

                targetCity.Battle.AddToLocal(GetStructuresInRadius(targetCity, stub.TroopObject));

                targetCity.Battle.AddToAttack(list);
            }
            else
            {
                targetCity.Battle = new BattleManager(targetCity);

                RegisterBattleListeners(targetCity);

                var ba = new BattlePassiveAction(targetCityId);

                targetCity.Battle.AddToLocal(GetStructuresInRadius(targetCity, stub.TroopObject));

                targetCity.Battle.AddToAttack(list);
                targetCity.Worker.DoPassive(targetCity, ba, false);
            }

            stub.TroopObject.BeginUpdate();
            stub.TroopObject.State = GameObjectState.BattleState(targetCity.Id);
            stub.TroopObject.Stats.Stamina = BattleFormulas.GetStamina(stub, targetCity);
            stub.TroopObject.EndUpdate();

            stub.TroopObject.Stub.BeginUpdate();
            stub.TroopObject.Stub.State = TroopState.Battle;
            stub.TroopObject.Stub.EndUpdate();

            return Error.Ok;
        }

        private void BattleWithdrawAttacker(IEnumerable<CombatObject> list)
        {
            TroopStub stub;
            City targetCity;
            City city;
            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub) || !Global.World.TryGetObjects(targetCityId, out targetCity))
                throw new ArgumentException();

            bool retreat = list.Any(co => co is AttackCombatUnit && ((AttackCombatUnit)co).TroopStub == stub);

            if (!retreat)
                return;

            DeregisterBattleListeners(targetCity);

            stub.TroopObject.BeginUpdate();
            stub.TroopObject.State = GameObjectState.NormalState();
            SetLootedResources(targetCity.Battle, stub);
            stub.TroopObject.EndUpdate();

            StateChange(ActionState.Completed);
        }

        private void SetLootedResources(BattleManager battle, TroopStub stub)
        {
            if (!battle.BattleStarted)
                return;

            // Calculate bonus
            Resource resource = BattleFormulas.GetBonusResources(stub.TroopObject);

            // Destroyed Structure bonus
            resource.Add(bonus);

            // Copy looted resources since we'll be modifying the troop's loot variable
            var looted = new Resource(stub.TroopObject.Stats.Loot);

            // Add bonus to troop object            
            Resource returning;
            Resource actual;
            stub.TroopObject.Stats.Loot.Add(resource, stub.Carry, out actual, out returning);

            // Update battle report view with actual received bonus            
            battle.BattleReport.SetLootedResources(stub.City.Id, stub.TroopId, battle.BattleId, looted, actual);
        }

        private void BattleExitTurn(CombatList atk, CombatList def, int turn)
        {
            City city;            
            TroopStub stub;
            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub))
                throw new ArgumentException();

            // Remove troop from battle if he is out of stamina, we need to check here because he might have lost
            // some stamina after knocking down a building
            if (stub.TroopObject.Stats.Stamina == 0)
            {
                City targetCity;
                if (!Global.World.TryGetObjects(targetCityId, out targetCity))
                    throw new ArgumentException();

                targetCity.Battle.RemoveFromAttack(new List<TroopStub> {stub}, ReportState.OutOfStamina);
            }
        }

        private void BattleActionAttacked(CombatObject source, CombatObject target, ushort damage)
        {
            City city;
            City targetCity;
            TroopStub stub;

            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub) || !Global.World.TryGetObjects(targetCityId, out targetCity))
                throw new ArgumentException();

            var unit = target as AttackCombatUnit;
            if (unit == null)
            {
                if (target.ClassType == BattleClass.Structure && target.IsDead)
                {
                    // if our troop knocked down a building, we get the bonus.
                    if (((AttackCombatUnit)source).TroopStub == stub)
                    {
                        bonus.Add(StructureFactory.GetCost(target.Type, target.Lvl)/2);
                        Global.DbManager.Save(this);
                    }

                    ReduceStamina(stub, BattleFormulas.GetStaminaStructureDestroyed(stub.TroopObject.Stats.Stamina));
                }                
            }
            // Check if this troop belongs to us
            else if (unit.TroopStub == stub && unit.TroopStub.TroopObject == stub.TroopObject)
            {
                // Check to see if player should retreat
                remainingUnitCount = stub.TotalCount;

                // Don't return if we haven't fulfilled the minimum rounds or not below the threshold
                if (unit.RoundsParticipated < Config.battle_min_rounds || remainingUnitCount == 0 || remainingUnitCount > Formula.GetAttackModeTolerance(originalUnitCount, mode))
                    return;

                targetCity.Battle.RemoveFromAttack(new List<TroopStub> {stub}, ReportState.Retreating);
            }
        }

        private void BattleExitBattle(CombatList atk, CombatList def)
        {
            City city;
            City targetCity;
            TroopStub stub;

            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub) || !Global.World.TryGetObjects(targetCityId, out targetCity))
                throw new ArgumentException();

            DeregisterBattleListeners(targetCity);

            stub.TroopObject.BeginUpdate();
            SetLootedResources(targetCity.Battle, stub);
            stub.TroopObject.Stub.BeginUpdate();
            stub.TroopObject.State = GameObjectState.NormalState();
            stub.TroopObject.Stub.State = TroopState.Idle;
            stub.TroopObject.Stub.EndUpdate();
            stub.TroopObject.EndUpdate();

            StateChange(ActionState.Completed);
        }
        
        private void BattleEnterRound(CombatList atk, CombatList def, uint round)
        {
            City city;
            TroopStub stub;
            City targetCity;

            if (!Global.World.TryGetObjects(cityId, stubId, out city, out stub) || !Global.World.TryGetObjects(targetCityId, out targetCity))
                throw new ArgumentException();

            // Reduce stamina and check if we need to remove this stub
            ReduceStamina(stub, (short)(stub.TroopObject.Stats.Stamina - 1));

            if (stub.TroopObject.Stats.Stamina == 0)
                targetCity.Battle.RemoveFromAttack(new List<TroopStub> { stub }, ReportState.OutOfStamina);
        }

        private static void ReduceStamina(TroopStub stub, short stamina)
        {
            stub.TroopObject.BeginUpdate();
            stub.TroopObject.Stats.Stamina = Math.Max((short)0, stamina);
            stub.TroopObject.EndUpdate();
        }

        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
        }
    }
}