#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.Stronghold;
using Game.Data.Tribe;
using Game.Data.Troop;
using Game.Logic.Actions;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util.Locking;

#endregion

namespace Game.Comm.ProcessorCommands
{
    class AssignmentCommandsModule : CommandModule
    {
        private readonly BattleProcedure battleProcedure;

        private readonly ICityManager cityManager;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly ILocker locker;

        private readonly IStrongholdManager strongholdManager;

        public AssignmentCommandsModule(BattleProcedure battleProcedure,
                                        IGameObjectLocator gameObjectLocator,
                                        ILocker locker,
                                        IStrongholdManager strongholdManager,
                                        ICityManager cityManager)
        {
            this.battleProcedure = battleProcedure;
            this.gameObjectLocator = gameObjectLocator;
            this.locker = locker;
            this.strongholdManager = strongholdManager;
            this.cityManager = cityManager;
        }

        public override void RegisterCommands(IProcessor processor)
        {
            processor.RegisterCommand(Command.TribeCityAssignmentCreate, CreateCityAssignment);
            processor.RegisterCommand(Command.TribeStrongholdAssignmentCreate, CreateStrongholdAssignment);
            processor.RegisterCommand(Command.TribeAssignmentJoin, Join);
            processor.RegisterCommand(Command.TribeAssignmentEdit, Edit);
            processor.RegisterCommand(Command.TribeAssignmentRemoveTroop, RemoveTroop);
        }

        private void RemoveTroop(Session session, Packet packet)
        {
            int assignmentId;
            uint cityId;
            ushort stubId;

            try
            {
                assignmentId = packet.GetInt32();
                cityId = packet.GetUInt32();
                stubId = packet.GetUInt16();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            if (!session.Player.IsInTribe)
            {
                ReplyError(session, packet, Error.TribesmanNotPartOfTribe);
                return;                
            }

            ITribe tribe = session.Player.Tribesman.Tribe;
            locker.Lock(session.Player, tribe).Do(() =>
            {
                var city = session.Player.GetCity(cityId);
                if (city == null)
                {
                    ReplyError(session, packet, Error.CityNotFound);
                    return;
                }

                ITroopStub stub;
                if (!city.Troops.TryGetStub(stubId, out stub))
                {
                    ReplyError(session, packet, Error.ObjectNotFound);
                    return;
                }

                Error result = tribe.RemoveFromAssignment(assignmentId, session.Player, stub);
                ReplyWithResult(session, packet, result);
            });
        }

        private void CreateStrongholdAssignment(Session session, Packet packet)
        {
            uint cityId;
            uint strongholdId;
            AttackMode mode;
            DateTime time;
            ISimpleStub simpleStub;
            string description;
            bool isAttack;
            try
            {
                mode = (AttackMode)packet.GetByte();
                cityId = packet.GetUInt32();
                strongholdId = packet.GetUInt32();
                time = DateTime.UtcNow.AddSeconds(packet.GetInt32());
                isAttack = packet.GetByte() == 1;
                simpleStub = PacketHelper.ReadStub(packet, isAttack ? FormationType.Attack : FormationType.Defense);
                description = packet.GetString();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            IStronghold stronghold;
            if (!strongholdManager.TryGetStronghold(strongholdId, out stronghold))
            {
                ReplyError(session, packet, Error.StrongholdNotFound);
                return;
            }

            ICity city;
            if (!cityManager.TryGetCity(cityId, out city))
            {
                ReplyError(session, packet, Error.StrongholdNotFound);
                return;
            }

            // First need to find all the objects that should be locked
            locker.Lock(city, stronghold).Do(() =>
            {
                if (city == null || stronghold == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                // Make sure city belongs to player and he is in a tribe
                if (city.Owner != session.Player || city.Owner.Tribesman == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                // Make sure this player is ranked high enough
                if (city.Owner.Tribesman == null ||
                    !city.Owner.Tribesman.Tribe.HasRight(city.Owner.PlayerId, TribePermission.AssignmentCreate))
                {
                    ReplyError(session, packet, Error.TribesmanNotAuthorized);
                    return;
                }

                int id;
                Error ret = session.Player.Tribesman.Tribe.CreateAssignment(city,
                                                                            simpleStub,
                                                                            stronghold.PrimaryPosition.X,
                                                                            stronghold.PrimaryPosition.Y,
                                                                            stronghold,
                                                                            time,
                                                                            mode,
                                                                            description,
                                                                            isAttack,
                                                                            out id);
                ReplyWithResult(session, packet, ret);
            });
        }

        private void CreateCityAssignment(Session session, Packet packet)
        {
            uint cityId;
            uint targetCityId;
            Position targetPosition;
            AttackMode mode;
            DateTime time;
            ISimpleStub simpleStub;
            string description;
            bool isAttack;
            try
            {
                mode = (AttackMode)packet.GetByte();
                cityId = packet.GetUInt32();
                targetCityId = packet.GetUInt32();
                targetPosition = new Position(packet.GetUInt32(), packet.GetUInt32());
                time = DateTime.UtcNow.AddSeconds(packet.GetInt32());
                isAttack = packet.GetByte() == 1;
                simpleStub = PacketHelper.ReadStub(packet, isAttack ? FormationType.Attack : FormationType.Defense);
                description = packet.GetString();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            // First need to find all the objects that should be locked
            uint[] playerIds = null;
            Dictionary<uint, ICity> cities;
            locker.Lock(out cities, cityId, targetCityId).Do(() =>
            {
                if (cities == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                var city = cities[cityId];

                // Make sure city belongs to player and he is in a tribe
                if (city.Owner != session.Player || !city.Owner.IsInTribe)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ICity targetCity = cities[targetCityId];

                // Make sure they are not in newbie protection
                if (battleProcedure.IsNewbieProtected(targetCity.Owner))
                {
                    ReplyError(session, packet, Error.PlayerNewbieProtection);
                    return;
                }

                playerIds = new[] {city.Owner.PlayerId, city.Owner.Tribesman.Tribe.Owner.PlayerId, targetCity.Owner.PlayerId};                
            });

            if (playerIds == null)
            {
                return;
            }

            Dictionary<uint, IPlayer> players;
            locker.Lock(out players, playerIds).Do(() =>
            {
                ICity city;
                ICity targetCity;
                if (players == null || !gameObjectLocator.TryGetObjects(cityId, out city) ||
                    !gameObjectLocator.TryGetObjects(targetCityId, out targetCity))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                // Make sure this player is ranked high enough
                if (city.Owner.Tribesman == null || !city.Owner.Tribesman.Tribe.HasRight(city.Owner.PlayerId, TribePermission.AssignmentCreate))
                {
                    ReplyError(session, packet, Error.TribesmanNotAuthorized);
                    return;
                }

                int id;
                var ret = session.Player.Tribesman.Tribe.CreateAssignment(city,
                                                                          simpleStub,
                                                                          targetPosition.X,
                                                                          targetPosition.Y,
                                                                          targetCity,
                                                                          time,
                                                                          mode,
                                                                          description,
                                                                          isAttack,
                                                                          out id);
                ReplyWithResult(session, packet, ret);
            });
        }

        private void Join(Session session, Packet packet)
        {
            uint cityId;
            int assignmentId;
            ISimpleStub stub;
            try
            {
                cityId = packet.GetUInt32();
                assignmentId = packet.GetInt32();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }
            
            if (!session.Player.IsInTribe)
            {
                ReplyError(session, packet, Error.TribesmanNotPartOfTribe);
                return;                
            }

            ITribe tribe = session.Player.Tribesman.Tribe;
            locker.Lock(session.Player, tribe).Do(() =>
            {
                ICity city = session.Player.GetCity(cityId);
                if (city == null)
                {
                    ReplyError(session, packet, Error.CityNotFound);
                    return;
                }

                // TODO: Clean this up
                Assignment assignment = tribe.Assignments.FirstOrDefault(x => x.Id == assignmentId);
                if (assignment == null)
                {
                    ReplyError(session, packet, Error.AssignmentDone);
                    return;
                }

                try
                {
                    stub = PacketHelper.ReadStub(packet, assignment.IsAttack ? FormationType.Attack : FormationType.Defense);
                }
                catch(Exception)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                Error result = tribe.JoinAssignment(assignmentId, city, stub);

                ReplyWithResult(session, packet, result);
            });
        }

        private void Edit(Session session, Packet packet)
        {
            int assignmentId;
            string description;

            try
            {
                assignmentId = packet.GetInt32();
                description = packet.GetString();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }
            
            if (!session.Player.IsInTribe)
            {
                ReplyError(session, packet, Error.TribesmanNotPartOfTribe);
                return;                
            }

            ITribe tribe = session.Player.Tribesman.Tribe;
            locker.Lock(session.Player, tribe).Do(() =>
            {
                Error result = tribe.EditAssignment(session.Player, assignmentId, description);
                ReplyWithResult(session, packet, result);
            });
        }
    }
}