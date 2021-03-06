﻿#region

using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Util;

#endregion

namespace Game.Comm.Channel
{
    public class BattleChannel
    {
        private readonly string channelName;

        private IChannel channel;

        public BattleChannel(IBattleManager battleManager, IChannel channel)
        {
            this.channel = channel;
            channelName = string.Format("/BATTLE/{0}", battleManager.BattleId);

            battleManager.ActionAttacked += BattleActionAttacked;
            battleManager.SkippedAttacker += BattleSkippedAttacker;
            battleManager.ReinforceAttacker += BattleReinforceAttacker;
            battleManager.ReinforceDefender += BattleReinforceDefender;
            battleManager.ExitBattle += BattleExitBattle;
            battleManager.EnterRound += BattleEnterRound;
            battleManager.WithdrawAttacker += BattleWithdrawAttacker;
            battleManager.WithdrawDefender += BattleWithdrawDefender;
            battleManager.GroupUnitAdded += BattleManagerOnGroupUnitAdded;
            battleManager.GroupUnitRemoved += BattleManagerOnGroupUnitRemoved;
            battleManager.ExitTurn += BattleManagerOnExitTurn;
        }

        private void BattleManagerOnExitTurn(IBattleManager battle, ICombatList attackers, ICombatList defenders, uint turn)
        {
            var properties = battle.ListProperties();
            if (properties.Count == 0)
            {
                return;
            }

            var packet = CreatePacket(battle, Command.BattlePropertyUpdate);
            PacketHelper.AddBattleProperties(battle.ListProperties(), packet);
            channel.Post(channelName, packet);
        }

        private Packet CreatePacket(IBattleManager battle, Command command)
        {
            var packet = new Packet(command);
            packet.AddUInt32(battle.BattleId);

            return packet;
        }

        private void BattleManagerOnGroupUnitAdded(IBattleManager battle,
                                                   BattleManager.BattleSide combatObjectSide,
                                                   ICombatGroup combatGroup,
                                                   ICombatObject combatObject)
        {
            var packet = CreatePacket(battle, Command.BattleGroupUnitAdded);
            packet.AddUInt32(combatGroup.Id);
            PacketHelper.AddToPacket(combatObject, packet);
            channel.Post(channelName, packet);
        }

        private void BattleManagerOnGroupUnitRemoved(IBattleManager battle,
                                                     BattleManager.BattleSide combatObjectSide,
                                                     ICombatGroup combatGroup,
                                                     ICombatObject combatObject)
        {
            var packet = CreatePacket(battle, Command.BattleGroupUnitRemoved);
            packet.AddUInt32(combatGroup.Id);
            packet.AddUInt32(combatObject.Id);
            channel.Post(channelName, packet);
        }

        private void BattleEnterRound(IBattleManager battle, ICombatList atk, ICombatList def, uint round)
        {
            var packet = CreatePacket(battle, Command.BattleNewRound);
            packet.AddUInt32(round);
            channel.Post(channelName, packet);
        }

        private void BattleExitBattle(IBattleManager battle, ICombatList atk, ICombatList def)
        {
            var packet = CreatePacket(battle, Command.BattleEnded);
            channel.Post(channelName, packet);

            // Unsubscribe everyone from this channel
            channel.Unsubscribe(channelName);
        }

        private void BattleReinforceDefender(IBattleManager battle, ICombatGroup combatGroup)
        {
            var packet = CreatePacket(battle, Command.BattleReinforceDefender);
            PacketHelper.AddToPacket(combatGroup, packet);
            channel.Post(channelName, packet);
        }

        private void BattleReinforceAttacker(IBattleManager battle, ICombatGroup combatGroup)
        {
            var packet = CreatePacket(battle, Command.BattleReinforceAttacker);
            PacketHelper.AddToPacket(combatGroup, packet);
            channel.Post(channelName, packet);
        }

        private void BattleSkippedAttacker(IBattleManager battle,
                                           BattleManager.BattleSide objSide,
                                           ICombatGroup combatGroup,
                                           ICombatObject source)
        {
            // Don't inform client for objs that simply never attack
            if (source.Stats.Atk == 0)
            {
                return;
            }

            var packet = CreatePacket(battle, Command.BattleSkipped);
            packet.AddUInt32(combatGroup.Id);
            packet.AddUInt32(source.Id);
            channel.Post(channelName, packet);
        }

        private void BattleActionAttacked(IBattleManager battle,
                                          BattleManager.BattleSide attackingSide,
                                          ICombatGroup attackerGroup,
                                          ICombatObject source,
                                          ICombatGroup targetGroup,
                                          ICombatObject target,
                                          decimal damage,
                                          int attackerCount,
                                          int targetCount)
        {
            var packet = CreatePacket(battle, Command.BattleAttack);
            packet.AddByte((byte)attackingSide);
            packet.AddUInt32(attackerGroup.Id);
            packet.AddUInt32(source.Id);
            packet.AddUInt32(targetGroup.Id);
            packet.AddUInt32(target.Id);
            packet.AddFloat((float)damage);
            packet.AddInt32(attackerCount);
            packet.AddInt32(targetCount);
            channel.Post(channelName, packet);
        }

        private void BattleWithdrawDefender(IBattleManager battle, ICombatGroup group)
        {
            var packet = CreatePacket(battle, Command.BattleWithdrawDefender);
            packet.AddUInt32(group.Id);
            channel.Post(channelName, packet);
        }

        private void BattleWithdrawAttacker(IBattleManager battle, ICombatGroup group)
        {
            var packet = CreatePacket(battle, Command.BattleWithdrawAttacker);
            packet.AddUInt32(group.Id);
            channel.Post(channelName, packet);
        }
    }
}