#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Map;
using Game.Util;
using Persistance;

#endregion

namespace Game.Battle
{
    /// <summary>
    ///     A list of combat objects and manages targetting.
    /// </summary>
    public class CombatList : PersistableObjectList<ICombatGroup>, ICombatList
    {
        private readonly IBattleFormulas battleFormulas;

        private readonly ITileLocator tileLocator;

        #region BestTargetResult enum

        public enum BestTargetResult
        {
            NoneInRange,

            Ok
        }

        #endregion

        public CombatList(IDbManager manager, ITileLocator tileLocator, IBattleFormulas battleFormulas)
                : base(manager)
        {
            this.tileLocator = tileLocator;
            this.battleFormulas = battleFormulas;

            ItemAdded += ObjectAdded;
        }

        private void ObjectAdded(PersistableObjectList<ICombatGroup> list, ICombatGroup item)
        {
            BackingList.Sort((combatGroup1, combatGroup2) => combatGroup1.Id.CompareTo(combatGroup2.Id));
        }

        public int UpkeepExcludingWaitingToJoinBattle
        {
            get
            {
                return AllCombatObjects()
                    .Where(obj => !obj.IsWaitingToJoinBattle)
                    .Sum(obj => obj.Upkeep);
            }
        }

        public int UpkeepTotal
        {
            get
            {
                return AllCombatObjects().Sum(obj => obj.Upkeep);
            }
        }

        public int UpkeepNotParticipatedInRound(uint round)
        {
            return AllAliveCombatObjects()
                    .Where(obj => !obj.IsWaitingToJoinBattle && !obj.HasAttacked(round))
                    .Sum(x => x.Upkeep);
        }

        public bool HasInRange(ICombatObject attacker)
        {
            return AllAliveCombatObjects().Any(obj => obj.InRange(attacker) && attacker.InRange(obj));
        }

        public BestTargetResult GetBestTargets(uint battleId, ICombatObject attacker, out List<Target> result, int maxCount, uint round)
        {
            result = new List<Target>();

            var objectsByScore = new List<CombatScoreItem>(Count);

            var objsInRange = (from combatGroup in this
                               from defender in combatGroup
                               where !defender.IsWaitingToJoinBattle &&
                                     !defender.IsDead &&
                                     defender.InRange(attacker) &&
                                     attacker.InRange(defender)
                               select new Target {Group = combatGroup, CombatObject = defender}).ToList();

            if (objsInRange.Count == 0)
            {
                return BestTargetResult.NoneInRange;
            }

            uint lowestRow = objsInRange.Min(target => target.CombatObject.Stats.Stl);

            Target bestTarget = null;
            decimal bestTargetScore = 0;
            foreach (var target in objsInRange)
            {
                if (!attacker.CanSee(target.CombatObject, lowestRow))
                {
                    continue;
                }

                // Calculate dmg against each target as base score
                decimal score = battleFormulas.GetAttackScore(attacker, target.CombatObject, round);

                if (bestTarget == null || score > bestTargetScore)
                {
                    bestTarget = target;
                    bestTargetScore = score;
                }

                objectsByScore.Add(new CombatScoreItem {Target = target, Score = score});
            }

            if (objectsByScore.Count == 0)
            {
                return BestTargetResult.Ok;                
            }

            // Shuffle to get some randomization in the attack order. We pass the battleId as a seed in order to 
            // make it so the attacker doesn't switch targets once it starts attacking them but this way
            // they won't attack the stacks in the order they joined the battle, which usually would mean
            // they will attack the same type of units one after another
            // then sort by score descending
            var shuffled = objectsByScore.Shuffle((int)battleId);
            shuffled.Sort(new CombatScoreItemComparer(attacker, tileLocator));
            
            var numberOfTargetsToHit = Math.Min(maxCount, objectsByScore.Count);
 
            // Get top results specified by the maxCount param
            result = shuffled.Take(numberOfTargetsToHit).Select(scoreItem => scoreItem.Target).ToList();

            return BestTargetResult.Ok;
        }

        public IEnumerable<ICombatObject> AllCombatObjects()
        {
            return BackingList.SelectMany(group => group.Select(combatObject => combatObject));
        }

        public IEnumerable<ICombatObject> AllAliveCombatObjects()
        {
            return
                    BackingList.SelectMany(
                                           group =>
                                           group.Where(combatObject => !combatObject.IsDead)
                                                .Select(combatObject => combatObject));
        }

        #region Nested type: CombatScoreItem

        private class CombatScoreItem
        {
            public decimal Score { get; set; }

            public Target Target { get; set; }
        }

        #endregion

        #region Nexted class: CombatComparer
        private class CombatScoreItemComparer : IComparer<CombatScoreItem>
        {
            private readonly ICombatObject attacker;
            private readonly ITileLocator tileLocator;

            public CombatScoreItemComparer(ICombatObject attacker, ITileLocator tileLocator)
            {
                this.attacker = attacker;
                this.tileLocator = tileLocator;
            }

            #region Implementation of IComparer<in CombatScoreItem>
            // return -1 if x is better target, 1 otherwise
            public int Compare(CombatScoreItem x, CombatScoreItem y)
            {
                var xArmorType = x.Target.CombatObject.Stats.Base.Armor;
                var yArmorType = y.Target.CombatObject.Stats.Base.Armor;
                if (x.Score == y.Score && (xArmorType == ArmorType.Building3 || yArmorType == ArmorType.Building3))
                {
                    if (xArmorType == ArmorType.Building3 && yArmorType == ArmorType.Building3)
                    {
                        var xDistance = tileLocator.RadiusDistance(attacker.Location(),
                                                                   attacker.Size,
                                                                   x.Target.CombatObject.Location(),
                                                                   x.Target.CombatObject.Size);

                        var yDistance = tileLocator.RadiusDistance(attacker.Location(),
                                                                   attacker.Size,
                                                                   y.Target.CombatObject.Location(),
                                                                   y.Target.CombatObject.Size);
                        return xDistance.CompareTo(yDistance);
                    }
                    return xArmorType == ArmorType.Building3 ? 1 : -1;
                }

                return x.Score.CompareTo(y.Score) * -1;
            }

            #endregion
        }
        #endregion

        #region Nested type: Target

        public class Target
        {
            public ICombatObject CombatObject { get; set; }

            public ICombatGroup Group { get; set; }

            public decimal? DamageCarryOverPercentage { get; set; } 
        }

        #endregion

        #region Nested type: NoneInRange

        public class NoneInRange : Exception
        {
        }

        #endregion

        #region Nested type: NoneVisible

        public class NoneVisible : Exception
        {
        }

        #endregion
    }
}