﻿using System.Collections.Generic;
using System.Linq;
using Game.Logic.Actions.ResourceActions;
using Game.Setup;
using Game.Util;

namespace Game.Data {
    public class ForestManager : ILockable {

        public static readonly object ForestLock = new object();

        Dictionary<uint, Forest> forests;        

        public int[] ForestDeletedCount { get; private set; }

        public ForestManager() {
            ForestDeletedCount = new int[3];
            forests = new Dictionary<uint, Forest>();            
        }

        public void StartForestCreator() {
            Global.Scheduler.Put(new ForestCreatorAction());
        }

        public void DbLoaderAdd(Forest forest) {
            forests.Add(forest.ObjectId, forest);
        }

        public void CreateForest(byte lvl, int capacity, int rate) {
            CreateForestAt(lvl, capacity, rate, 0, 0);
        }

        public void CreateForestAt(byte lvl, int capacity, int rate, uint x, uint y) {
            Forest forest = new Forest(lvl, capacity, rate);

            if (x == 0 || y == 0) {
                while (true) {
                    x = (uint) Config.Random.Next(15, (int) Config.map_width - 15);
                    y = (uint) Config.Random.Next(15, (int) Config.map_height - 15);

                    Global.World.LockRegion(x, y);

                    // check if near a city
                    if (Global.World.GetObjectsWithin(x, y, 4).Exists(obj => !(obj is TroopObject))) {
                        Global.World.UnlockRegion(x, y);
                        continue;
                    }

                    break;
                }
            } else {
                Global.World.LockRegion(x, y);
            }

            forest.X = x;
            forest.Y = y;

            Global.World.Add(forest);
            Global.World.UnlockRegion(x, y);

            forests.Add(forest.ObjectId, forest);

            forest.BeginUpdate();
            forest.RecalculateForest();
            forest.EndUpdate();
        }

        public void RemoveForest(Forest forest) {
            ForestDeletedCount[forest.Lvl]++;

            forests.Remove(forest.ObjectId);

            forest.BeginUpdate();
            Global.World.Remove(forest);
            forest.EndUpdate();

            Global.DbManager.Delete(forest);
        }

        /// <summary>
        /// Locks all cities participating in this forest. 
        /// Proper usage would be to lock the forest manager and the main city in the base objects. 
        /// The custom[0] parameter should a uint with the forestId.
        /// Once inside of the lock, a call to ForestManager.TryGetValue should be used to get the forest.
        /// </summary>
        /// <param name="custom">custom[0] should contain the forestId to lock</param>
        /// <returns>List of cities to lock for the forest.</returns>
        public ILockable[] CallbackLockHandler(object[] custom) {
            return GetListOfLocks((uint) custom[0]);
        }

        public ILockable[] GetListOfLocks(uint forestId) {
            MultiObjectLock.ThrowExceptionIfNotLocked(Global.Forests);

            Forest forest;

            return !forests.TryGetValue(forestId, out forest) ? new ILockable[] { } : forest.Select(obj => obj.City).ToArray();
        }

        public bool TryGetValue(uint id, out Forest forest) {
            return forests.TryGetValue(id, out forest);
        }

        public int Hash {
            get { return (int)Global.Locks.FOREST; }
        }

        public object Lock {
            get { return ForestLock; }
        }
    }
}
