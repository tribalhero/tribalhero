#region

using Game.Data;
using Game.Setup;

#endregion

namespace Game.Logic.Formulas
{
    public partial class Formula
    {
        /// <summary>
        ///   Returns the maximum number of forests the lumbermill can be harvesting from at one time.
        /// </summary>
        /// <param name = "level"></param>
        public static int GetMaxForestCount(byte level)
        {
            if (level <= 3)
                return 1;

            if (level <= 8)
                return 2;

            if (level <= 14)
                return 3;

            return 4;
        }

        /// <summary>
        ///   Returns the maximum labors allowed in the forest
        /// </summary>
        /// <param name = "level"></param>
        /// <returns></returns>
        public static ushort GetForestMaxLabor(byte level)
        {
            return (ushort)(level*240);
        }

        public static ushort GetForestMaxLaborPerUser(Forest forest)
        {
            return (ushort)(GetForestMaxLabor(forest.Lvl)/6);
        }

        /// <summary>
        ///   Returns the maximum forest level the lumbermill is allowed to harvest from.
        /// </summary>
        /// <param name = "level">Level of the lumbermill</param>
        /// <returns></returns>
        public static byte GetMaxForestLevel(byte level)
        {
            if (level <= 2)
                return 1;

            if (level <= 6)
                return 2;

            if (level <= 11)
                return 3;

            return 4;
        }

        /// <summary>
        ///   Gets the maximum capacity of the forest
        /// </summary>
        /// <param name = "level"></param>
        /// <returns></returns>
        public static int GetMaxForestCapacity(byte level)
        {
            return (9000 + Config.Random.Next(-1000, 1000))*level;
        }

        /// <summary>
        ///   Returns the forest rate
        /// </summary>
        /// <param name = "level"></param>
        /// <returns></returns>
        public static double GetMaxForestRate(byte level)
        {
            return 0.75 + (level - 1)*0.25;
        }
    }
}