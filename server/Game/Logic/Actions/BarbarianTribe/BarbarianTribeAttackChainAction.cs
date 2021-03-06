#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Troop;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;

#endregion

namespace Game.Logic.Actions
{
    public class BarbarianTribeAttackChainAction : ChainAction
    {
        private readonly IActionFactory actionFactory;

        private readonly BattleProcedure battleProcedure;

        private uint cityId;

        private uint troopObjectId;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly ILocker locker;

        private readonly Procedure procedure;

        private uint targetObjectId;

        private readonly ITroopObjectInitializer troopObjectInitializer;

        public BarbarianTribeAttackChainAction(IActionFactory actionFactory,
                                               Procedure procedure,
                                               ILocker locker,
                                               IGameObjectLocator gameObjectLocator,
                                               BattleProcedure battleProcedure)
        {
            this.actionFactory = actionFactory;
            this.procedure = procedure;
            this.locker = locker;
            this.gameObjectLocator = gameObjectLocator;
            this.battleProcedure = battleProcedure;
        }

        public BarbarianTribeAttackChainAction(uint cityId,
                                               uint targetObjectId,
                                               ITroopObjectInitializer troopObjectInitializer,
                                               IActionFactory actionFactory,
                                               Procedure procedure,
                                               ILocker locker,
                                               IGameObjectLocator gameObjectLocator,
                                               BattleProcedure battleProcedure)
            : this(actionFactory, procedure, locker, gameObjectLocator, battleProcedure)
        {
            this.troopObjectInitializer = troopObjectInitializer;
            this.cityId = cityId;
            this.targetObjectId = targetObjectId;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
            cityId = uint.Parse(properties["city_id"]);
            troopObjectId = uint.Parse(properties["troop_object_id"]);
            targetObjectId = uint.Parse(properties["target_object_id"]);
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.BarbarianTribeAttackChain;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                        {
                                new XmlKvPair("city_id", cityId), 
                                new XmlKvPair("troop_object_id", troopObjectId),                                
                                new XmlKvPair("target_object_id", targetObjectId)
                        });
            }
        }

        public override ActionCategory Category
        {
            get
            {
                return ActionCategory.Attack;
            }
        }

        public override Error Execute()
        {
            ICity city;
            
            IBarbarianTribe barbarianTribe;

            if (!gameObjectLocator.TryGetObjects(cityId, out city) || !gameObjectLocator.TryGetObjects(targetObjectId, out barbarianTribe))
            {
                return Error.ObjectNotFound;
            }

            if (battleProcedure.HasTooManyAttacks(city))
            {
                return Error.TooManyTroops;
            }

            if (barbarianTribe.CampRemains == 0 || !barbarianTribe.InWorld)
            {
                return Error.BarbarianTribeNoCampsRemaining;
            }

            ITroopObject troopObject;
            var troopInitializeResult = troopObjectInitializer.GetTroopObject(out troopObject);
            if (troopInitializeResult != Error.Ok)
            {
                return troopInitializeResult;
            }

            troopObjectId = troopObject.ObjectId;
            
            city.References.Add(troopObject, this);
            city.Notifications.Add(troopObject, this);

            var moveAction = actionFactory.CreateTroopMovePassiveAction(cityId,
                                                                        troopObject.ObjectId,
                                                                        barbarianTribe.PrimaryPosition.X,
                                                                        barbarianTribe.PrimaryPosition.Y,
                                                                        isReturningHome: false,
                                                                        isAttacking: true);

            ExecuteChainAndWait(moveAction, AfterTroopMoved);

            return Error.Ok;
        }

        private void AfterTroopMoved(ActionState state)
        {
            if (state == ActionState.Fired)
            {
                IBarbarianTribe targetBarbarianTribe;
                // Verify the target is still good, otherwise we walk back immediately
                if (!gameObjectLocator.TryGetObjects(targetObjectId, out targetBarbarianTribe) || targetBarbarianTribe.CampRemains == 0)
                {
                    CancelCurrentChain();
                }

                return;
            }
            
            if (state == ActionState.Failed)
            {
                // If TroopMove failed it's because we cancelled it and the target is invalid. Walk back home
                ICity city;
                ITroopObject troopObject;

                locker.Lock(cityId, troopObjectId, out city, out troopObject).Do(() =>
                {
                    TroopMovePassiveAction tma = actionFactory.CreateTroopMovePassiveAction(city.Id, troopObject.ObjectId, city.PrimaryPosition.X, city.PrimaryPosition.Y, true, true);
                    ExecuteChainAndWait(tma, AfterTroopMovedHome);
                });

                return;
            }
            
            if (state == ActionState.Completed)
            {
                ICity city;
                IBarbarianTribe targetBarbarianTribe;
                ITroopObject troopObject;

                if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
                {
                    throw new Exception("City or troop object is missing");
                }

                if (!gameObjectLocator.TryGetObjects(targetObjectId, out targetBarbarianTribe))
                {
                    //If the target is missing, walk back
                    locker.Lock(city).Do(() =>
                    {
                        TroopMovePassiveAction tma = actionFactory.CreateTroopMovePassiveAction(city.Id,
                                                                                                troopObject.ObjectId,
                                                                                                city.PrimaryPosition.X,
                                                                                                city.PrimaryPosition.Y,
                                                                                                true,
                                                                                                true);
                        ExecuteChainAndWait(tma, AfterTroopMovedHome);                        
                    });

                    return;
                }

                locker.Lock(city, targetBarbarianTribe).Do(() =>
                {
                    var bea = actionFactory.CreateBarbarianTribeEngageAttackPassiveAction(cityId, troopObject.ObjectId, targetObjectId);
                    ExecuteChainAndWait(bea, AfterBattle);
                });
            }
        }

        private void AfterBattle(ActionState state)
        {
            if (state != ActionState.Completed)
            {
                return;
            }

            ICity city;
            locker.Lock(cityId, out city).Do(() =>
            {
                ITroopObject troopObject;
                if (!city.TryGetTroop(troopObjectId, out troopObject))
                {
                    throw new Exception("Troop object should still exist");
                }

                // Check if troop is still alive
                if (troopObject.Stub.TotalCount > 0)
                {
                    // Send troop back home
                    var tma = actionFactory.CreateTroopMovePassiveAction(city.Id,
                                                                         troopObject.ObjectId,
                                                                         city.PrimaryPosition.X,
                                                                         city.PrimaryPosition.Y,
                                                                         true,
                                                                         true);
                    ExecuteChainAndWait(tma, AfterTroopMovedHome);
                }
                else
                {
                    city.References.Remove(troopObject, this);

                    // Remove troop since he's dead
                    city.BeginUpdate();
                    procedure.TroopObjectDelete(troopObject, false);
                    city.EndUpdate();

                    StateChange(ActionState.Completed);
                }
            });
        }

        private void AfterTroopMovedHome(ActionState state)
        {
            if (state != ActionState.Completed)
            {
                return;
            }

            ICity city;
            ITroopObject troopObject;
            locker.Lock(cityId, troopObjectId, out city, out troopObject).Do(() =>
            {
                // If city is not in battle then add back to city otherwise join local battle
                if (city.Battle == null)
                {
                    city.References.Remove(troopObject, this);
                    city.Notifications.Remove(this);
                    procedure.TroopObjectDelete(troopObject, true);
                    StateChange(ActionState.Completed);
                }
                else
                {
                    var eda = actionFactory.CreateCityEngageDefensePassiveAction(cityId, troopObject.ObjectId, FormationType.Attack);
                    ExecuteChainAndWait(eda, AfterEngageDefense);
                }
            });
        }

        private void AfterEngageDefense(ActionState state)
        {
            if (state != ActionState.Completed)
            {
                return;
            }

            ICity city;
            ITroopObject troopObject;
            locker.Lock(cityId, troopObjectId, out city, out troopObject).Do(() =>
            {
                city.References.Remove(troopObject, this);
                city.Notifications.Remove(this);

                procedure.TroopObjectDelete(troopObject, troopObject.Stub.TotalCount != 0);

                StateChange(ActionState.Completed);
            });
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }
    }
}