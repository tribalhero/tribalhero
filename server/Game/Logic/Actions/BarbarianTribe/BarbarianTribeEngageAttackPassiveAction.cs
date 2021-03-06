#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Troop;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using Persistance;

#endregion

namespace Game.Logic.Actions
{
    public class BarbarianTribeEngageAttackPassiveAction : PassiveAction
    {
        private readonly IBattleFormulas battleFormula;

        private uint cityId;

        private readonly IDbManager dbManager;

        private readonly IStaminaMonitorFactory staminaMonitorFactory;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly BarbarianTribeBattleProcedure barbarianTribeBattleProcedure;

        private readonly Formula formula;

        private uint targetObjectId;

        private uint troopObjectId;

        private uint groupId;

        private int originalUnitCount;

        public BarbarianTribeEngageAttackPassiveAction(IBattleFormulas battleFormula,
                                                       IGameObjectLocator gameObjectLocator,
                                                       BarbarianTribeBattleProcedure barbarianTribeBattleProcedure,
                                                       Formula formula,
                                                       IDbManager dbManager,
                                                       IStaminaMonitorFactory staminaMonitorFactory)
        {
            this.battleFormula = battleFormula;
            this.gameObjectLocator = gameObjectLocator;
            this.barbarianTribeBattleProcedure = barbarianTribeBattleProcedure;
            this.formula = formula;
            this.dbManager = dbManager;
            this.staminaMonitorFactory = staminaMonitorFactory;
        }

        public BarbarianTribeEngageAttackPassiveAction(uint cityId,
                                                       uint troopObjectId,
                                                       uint targetObjectId,
                                                       IBattleFormulas battleFormula,
                                                       IGameObjectLocator gameObjectLocator,
                                                       BarbarianTribeBattleProcedure barbarianTribeBattleProcedure,
                                                       Formula formula,
                                                       IDbManager dbManager,
                                                       IStaminaMonitorFactory staminaMonitorFactory)
            : this(battleFormula, gameObjectLocator, barbarianTribeBattleProcedure, formula, dbManager, staminaMonitorFactory)
        {
            this.cityId = cityId;
            this.troopObjectId = troopObjectId;
            this.targetObjectId = targetObjectId;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
            cityId = uint.Parse(properties["troop_city_id"]);
            troopObjectId = uint.Parse(properties["troop_object_id"]);
            groupId = uint.Parse(properties["group_id"]);
            originalUnitCount = int.Parse(properties["original_count"]);

            targetObjectId = uint.Parse(properties["target_object_id"]);

            IBarbarianTribe targetBarbarianTribe;
            gameObjectLocator.TryGetObjects(targetObjectId, out targetBarbarianTribe);
            RegisterBattleListeners(targetBarbarianTribe);

            var combatGroup = targetBarbarianTribe.Battle.GetCombatGroup(groupId);
            ITroopObject troopObject;
            ICity city;
            gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject);

            StaminaMonitor = staminaMonitorFactory.CreateStaminaMonitor(targetBarbarianTribe.Battle, combatGroup, short.Parse(properties["stamina"]));
            StaminaMonitor.PropertyChanged += (sender, args) => dbManager.Save(this);

            AttackModeMonitor = new AttackModeMonitor(targetBarbarianTribe.Battle, combatGroup, troopObject.Stub);
        }

        private StaminaMonitor StaminaMonitor { get; set; }

        private AttackModeMonitor AttackModeMonitor { get; set; }

        public override ActionType Type
        {
            get
            {
                return ActionType.BarbarianTribeEngageAttackPassive;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                        {
                                new XmlKvPair("target_object_id", targetObjectId), 
                                new XmlKvPair("troop_city_id", cityId),
                                new XmlKvPair("troop_object_id", troopObjectId), 
                                new XmlKvPair("group_id", groupId), 
                                new XmlKvPair("stamina", StaminaMonitor.Stamina),
                                new XmlKvPair("original_count", originalUnitCount)
                        });
            }
        }

        private void RegisterBattleListeners(IBarbarianTribe barbarianTribe)
        {
            barbarianTribe.Battle.WithdrawAttacker += BattleWithdrawAttacker;
            barbarianTribe.Battle.GroupKilled += BattleGroupKilled;
        }

        private void DeregisterBattleListeners(IBarbarianTribe barbarianTribe)
        {
            barbarianTribe.Battle.GroupKilled -= BattleGroupKilled;
            barbarianTribe.Battle.WithdrawAttacker -= BattleWithdrawAttacker;
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        public override Error Execute()
        {
            ICity city;
            IBarbarianTribe barbarianTribe;
            ITroopObject troopObject;

            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject) ||
                !gameObjectLocator.TryGetObjects(targetObjectId, out barbarianTribe))
            {
                return Error.ObjectNotFound;
            }

            // Save original unit count to know when to bail out of battle
            originalUnitCount = troopObject.Stub.TotalCount;

            // Create the group in the battle
            uint battleId;
            ICombatGroup combatGroup;
            barbarianTribeBattleProcedure.JoinOrCreateBarbarianTribeBattle(barbarianTribe, troopObject, out combatGroup, out battleId);
            groupId = combatGroup.Id;

            // Register the battle listeners
            RegisterBattleListeners(barbarianTribe);

            // Create stamina monitor
            StaminaMonitor = staminaMonitorFactory.CreateStaminaMonitor(barbarianTribe.Battle, combatGroup, battleFormula.GetStamina(troopObject.Stub, barbarianTribe));
            StaminaMonitor.PropertyChanged += (sender, args) => dbManager.Save(this);

            // Create attack mode monitor
            AttackModeMonitor = new AttackModeMonitor(barbarianTribe.Battle, combatGroup, troopObject.Stub);

            // Set the attacking troop object to the correct state and stamina
            troopObject.BeginUpdate();
            troopObject.State = GameObjectStateFactory.BattleState(battleId);
            troopObject.EndUpdate();

            // Set the troop stub to the correct state
            troopObject.Stub.BeginUpdate();
            troopObject.Stub.State = TroopState.Battle;
            troopObject.Stub.EndUpdate();

            return Error.Ok;
        }

        private void BattleWithdrawAttacker(IBattleManager battle, ICombatGroup group)
        {
            IBarbarianTribe barbarianTribe;
            ICity city;
            ITroopObject troopObject;
            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject) ||
                !gameObjectLocator.TryGetObjects(targetObjectId, out barbarianTribe))
            {
                throw new ArgumentException();
            }

            if (group.Id != groupId)
            {
                return;
            }

            DeregisterBattleListeners(barbarianTribe);

            troopObject.BeginUpdate();
            SetLootedResources(barbarianTribe, barbarianTribe.Battle, troopObject, group);
            troopObject.Stub.BeginUpdate();
            troopObject.State = GameObjectStateFactory.NormalState();
            troopObject.Stub.State = TroopState.Idle;
            troopObject.Stub.EndUpdate();
            troopObject.EndUpdate();

            StateChange(ActionState.Completed);
        }

        /// <summary>
        ///     Takes care of finishing this action up if all our units are killed
        /// </summary>
        private void BattleGroupKilled(IBattleManager battle, ICombatGroup group)
        {
            // Ignore if not our group
            if (group.Id != groupId)
            {
                return;
            }

            IBarbarianTribe barbarianTribe;
            ICity city;
            ITroopObject troopObject;
            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject) ||
                !gameObjectLocator.TryGetObjects(targetObjectId, out barbarianTribe))
            {
                throw new ArgumentException();
            }

            DeregisterBattleListeners(barbarianTribe);

            troopObject.BeginUpdate();
            troopObject.State = GameObjectStateFactory.NormalState();
            troopObject.EndUpdate();

            StateChange(ActionState.Completed);
        }

        private void SetLootedResources(IBarbarianTribe barbarianTribe, IBattleManager battle, ITroopObject troopObject, ICombatGroup combatGroup)
        {
            if (!battle.BattleStarted)
            {
                return;
            }

            // Calculate bonus
            Resource resource = battleFormula.GetBonusResources(troopObject,
                                                                originalUnitCount,
                                                                troopObject.Stub.TotalCount);
            
            // Add barbarian tribe bonus
            resource.Add(formula.BarbarianTribeBonus(barbarianTribe.Lvl, battle, combatGroup, barbarianTribe));

            // Copy looted resources since we'll be modifying the troop's loot variable
            var looted = new Resource(troopObject.Stats.Loot);

            // Add bonus to troop object            
            Resource returning;
            Resource actual;
            Resource cap = new Resource(troopObject.Stub.Carry / 1,
                                        troopObject.Stub.Carry / 2,
                                        troopObject.Stub.Carry / Config.battle_loot_resource_iron_ratio,
                                        troopObject.Stub.Carry / 1,
                                        troopObject.Stub.Carry / Config.battle_loot_resource_labor_ratio);

            troopObject.Stats.Loot.Add(resource, cap, out actual, out returning);

            // Update battle report view with actual received bonus            
            battle.BattleReport.SetLootedResources(combatGroup.Owner, combatGroup.Id, battle.BattleId, looted, actual);
        }
        
        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
        }
    }
}