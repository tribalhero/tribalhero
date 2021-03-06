using Game.Comm;
using Game.Data;
using Game.Data.Stats;
using Game.Map;
using Game.Util.Locking;
using Persistance;

namespace Game.Battle.CombatObjects
{
    public interface ICombatObject : IPersistableObject, ILockable
    {
        ushort MaxDmgRecv { get; set; }

        ushort MinDmgRecv { get; set; }

        ushort MaxDmgDealt { get; set; }

        ushort MinDmgDealt { get; set; }

        ushort HitRecv { get; set; }

        ushort HitDealt { get; set; }

        uint HitDealtByUnit { get; set; }

        decimal DmgRecv { get; set; }

        decimal DmgDealt { get; set; }

        int RoundsParticipated { get; set; }

        uint LastRound { get; set; }

        uint Id { get; }

        uint GroupId { get; set; }

        bool Disposed { get; }

        int Upkeep { get; }

        bool IsDead { get; }

        BattleStats Stats { get; }

        ushort Type { get; }

        Resource Loot { get; }

        BattleClass ClassType { get; }

        ushort Count { get; }

        decimal Hp { get; }

        uint Visibility { get; }

        byte Lvl { get; }

        byte Size { get; }

        bool IsWaitingToJoinBattle { get; set; }

        void ExitBattle();

        decimal AttackBonus(ICombatObject target);

        decimal DefenseBonus(ICombatObject target);

        void TakeDamage(decimal dmg, out Resource returning, out int attackPoints);

        void CalcActualDmgToBeTaken(ICombatList attackers,
                                    ICombatList defenders,
                                    IBattleRandom random,
                                    decimal baseDmg,
                                    int attackIndex,
                                    out decimal actualDmg);

        bool InRange(ICombatObject obj);

        Position Location();

        byte AttackRadius();

        void ReceiveReward(int attackPoints, Resource resource);

        int LootPerRound();

        bool CanSee(ICombatObject obj, uint lowestSteath);

        void ParticipatedInRound(uint round);

        void JoinBattle(uint round);

        bool HasAttacked(uint round);
		
        void AddPacketInfo(Packet packet);
    }
}