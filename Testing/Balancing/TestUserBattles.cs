﻿#region

using System;
using System.Linq;
using ConsoleSimulator;
using Game.Data;
using Game.Logic.Procedures;
using Game.Setup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Game.Battle;
using Testing.Balancing;

#endregion

namespace Testing.Troop
{
    /// <summary>
    ///   Summary description for TroopProcedureTest
    /// </summary>
    [TestClass]
    public class TestUserBattles : TestBase
    {

        [TestInitialize]
        public void TestInitialize()
        {
            Global.FireEvents = false;
            Factory.CompileConfigFiles();
            Factory.InitAll();
            Global.DbManager.Pause();
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public void TestBrennenGotOwned()
        {
            Group defender = new Group();
            defender.AddToLocal(UnitType.Swordsman, 10, 144);
            defender.AddToLocal(UnitType.Archer, 10, 100);
            defender.AddToLocal(UnitType.Pikeman, 10, 100);
            defender.AddToLocal(UnitType.Gladiator, 10, 105);
            defender.AddToLocal(UnitType.Cavalry, 3, 9);
            defender.AddStructure(StructureType.Barrack, 5);
            defender.AddStructure(StructureType.Farm, 12);
            int defUpkeep = defender.Upkeep();

            Group attacker = new Group();
            attacker.AddToAttack(UnitType.Archer, 10, 40);
            attacker.AddToAttack(UnitType.Gladiator, 10, 20);
            attacker.AddToAttack(UnitType.Cavalry, 4, 10);
            attacker.AddToAttack(UnitType.Knight, 7, 10);
            int atkUpkeep = attacker.Upkeep();

            Simulation sim = new Simulation(attacker, defender);
            sim.RunTill(20);

            double actualAdvantage;
            if (attacker.Upkeep() == 0) {
                actualAdvantage = ((double)defender.Upkeep() / defUpkeep);
            } else {
                actualAdvantage = -1 * ((double)attacker.Upkeep() / atkUpkeep);
            }
            if (double.IsNaN(actualAdvantage)) actualAdvantage = 0;
            Assert.IsTrue(actualAdvantage == 0,
                                        "Def[{0}/{1}] Structures[{4}/{5}] vs Atk[{2}/{3}]",
                                                    defender.Upkeep(),
                                                    defUpkeep,
                                                    attacker.Upkeep(),
                                                    atkUpkeep,
                                                    defender.Structures.Sum(x => x.Stats.Hp),
                                                    defender.Structures.Sum(x => x.Stats.Base.Battle.MaxHp));
        }

    }
}