#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Logic.Actions;
using Game.Setup;

#endregion

namespace Game.Logic.Formulas
{
    public partial class Formula
    {
        protected Formula()
		{
		}
	
        public Formula(IObjectTypeFactory objectTypeFactory, UnitFactory unitFactory, IStructureCsvFactory structureFactory, ISystemVariableManager systemVariableManager)
        {
            SystemVariableManager = systemVariableManager;
            ObjectTypeFactory = objectTypeFactory;
            UnitFactory = unitFactory;
	    	StructureCsvFactory = structureFactory;
        }

        public virtual IObjectTypeFactory ObjectTypeFactory { get; set; }

        public virtual UnitFactory UnitFactory { get; set; }

        public virtual IStructureCsvFactory StructureCsvFactory { get; set; }

        public virtual ISystemVariableManager SystemVariableManager { get; set; }

        public virtual Error CityMaxConcurrentBuildActions(ushort structureType, uint currentActionId, ICity city, IObjectTypeFactory objectTypeFactory)
        {
            int maxConcurrentUpgrades = ConcurrentBuildUpgrades(city.MainBuilding.Lvl);

            if (!objectTypeFactory.IsObjectType("UnlimitedBuilding", structureType) &&
                city.Worker.ActiveActions.Values.Count(action =>
                    {
                        if (action.ActionId == currentActionId)
                        {
                            return false;
                        }

                        if (action.Type == ActionType.StructureUpgradeActive)
                        {
                            return true;
                        }

                        if (action.Type != ActionType.StructureBuildActive)
                        {
                            return false;
                        }

                        return !objectTypeFactory.IsObjectType("UnlimitedBuilding", ((StructureBuildActiveAction)action).BuildType);
                    }) >= maxConcurrentUpgrades)
            {
                return Error.ActionTotalMaxReached;
            }

            return Error.Ok;
        }

        /// <summary>
        ///     Applies the specified effects to the specified radius. This is used by AwayFromLayout for building validation.
        /// </summary>
        /// <param name="effects"></param>
        /// <param name="radius"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual int GetAwayFromRadius(IEnumerable<Effect> effects, byte radius, ushort type)
        {
            return radius +
                   effects.DefaultIfEmpty()
                          .Min(x => (x != null && x.Id == EffectCode.AwayFromStructureMod && (int)x.Value[0] == type) ? (int)x.Value[1] : 0);
        }

        /// <summary>
        ///     Returns maximum number of concurrent build/upgrades allowed for the given structure level
        /// </summary>
        /// <param name="mainstructureLevel"></param>
        /// <returns></returns>
        public virtual int ConcurrentBuildUpgrades(int mainstructureLevel)
        {
            return mainstructureLevel >= 11 ? 3 : 2;
        }

        /// <summary>
        ///     Returns the crop cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public virtual int ResourceCropCap(byte lvl)
        {
            int[] cap =
            {
                    850, 850, 950, 1130, 1450, 1880, 2440, 3200, 4200, 5500, 7200, 9500, 12500, 16500, 21800, 25000
            };
            return cap[lvl];
        }

        /// <summary>
        ///     /// Returns the wood cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public virtual int ResourceWoodCap(byte lvl)
        {
            int[] cap =
            {
                    850, 850, 950, 1130, 1450, 1880, 2440, 3200, 4200, 5500, 7200, 9500, 12500, 16500, 21800, 25000
            };
            return cap[lvl];
        }

        /// <summary>
        ///     Returns the iron cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public virtual int ResourceIronCap(byte lvl)
        {
            int[] cap = {0, 100, 100, 100, 100, 100, 170, 380, 620, 900, 1240, 1630, 2090, 2620, 3260, 4000};
            return cap[lvl];
        }

        /// <summary>
        ///     Returns the amount of iron the user should get for the specified city
        /// </summary>
        /// <param name="city">City to recalculate resources for.</param>
        /// <returns></returns>
        public virtual int GetIronRate(ICity city)
        {
            int[] multiplier = {int.MaxValue, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 5, 5, 5, 5, 4};

            return city.Sum(x => ObjectTypeFactory.IsStructureType("Iron", x) ? x.Stats.Labor / multiplier[x.Lvl] : 0);
        }

        /// <summary>
        ///     Returns the amount of crop the user should get for the specified city
        /// </summary>
        /// <param name="city">City to recalculate resources for</param>
        /// <returns></returns>
        public virtual int GetCropRate(ICity city)
        {
            double[] lvlBonus = {1, 1, 1, 1, 1, 1, 1, 1.1, 1.1, 1.2, 1.2, 1.3, 1.3, 1.4, 1.4, 1.5};
            return 60 + city.Lvl * 5 +
                   (int)
                   city.Sum(x => ObjectTypeFactory.IsStructureType("Crop", x) ? x.Stats.Labor * lvlBonus[x.Lvl] : 0);
        }

        /// <summary>
        ///     Returns the amount of wood the user should get for the specified city.
        ///     Notice: This function looks at all the Forest Camps Rate property and adds them up.
        /// </summary>
        /// <param name="city">City to recalculate resources for</param>
        /// <returns></returns>
        public virtual int GetWoodRate(ICity city)
        {
            return 60 + city.Lvl * 5 + city.Sum(x =>
                {
                    object rate;
                    if (!ObjectTypeFactory.IsStructureType("ForestCamp", x) || x.Lvl == 0 ||
                        !x.Properties.TryGet("Rate", out rate))
                    {
                        return 0;
                    }

                    return (int)rate;
                });
        }

        /// <summary>
        ///     Returns the amount of gold the user should get for the specified city
        /// </summary>
        /// <param name="city">City to recalculate resources for</param>
        /// <returns></returns>
        public virtual int GetGoldRate(ICity city)
        {
            int value = 0;
            
            var weaponExportMax =
                            city.Technologies.GetEffects(EffectCode.WeaponExport)
                                .DefaultIfEmpty()
                                .Max(x => x == null ? 0 : (int)x.Value[0]);
            if(weaponExportMax>0)
            {
                value += GetWeaponExportLaborProduce(weaponExportMax, city.Resource.Labor.Value, city.Resource.Gold.Value);
            }

            foreach (Structure structure in city.Where(x => ObjectTypeFactory.IsStructureType("Market", x)))
            {
                if (structure.Lvl >= 10)
                {
                    value += 50;
                }
                else if (structure.Lvl >= 6)
                {
                    value += 18;
                }
                else if (structure.Lvl >= 4)
                {
                    value += 4;
                }
            }
            return value;
        }

        /// <summary>
        ///     Returns the rate that the specified structure should gather from the given forest.
        /// </summary>
        /// <param name="forestCamp"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        public virtual int GetWoodRateForForestCamp(IStructure forestCamp, float efficiency)
        {
            var lumbermill = forestCamp.City.FirstOrDefault(s => ObjectTypeFactory.IsStructureType("Lumbermill", s));
            if (lumbermill == null)
                return 0;

            double[] rate = {0, .75, .75, 1, 1, 1, 1, 1.25, 1.25, 1.25, 1.25, 1.25, 1.5, 1.5, 1.5, 1.5};
            return (int)(forestCamp.Stats.Labor * rate[lumbermill.Lvl] * (1f + efficiency));
        }

        /// <summary>
        ///     Returns the total unit for the price of paidFor
        /// </summary>
        /// <param name="tech"></param>
        /// <param name="paidFor"></param>
        /// <returns></returns>
        public virtual int GetXForYTotal(ITechnologyManager tech, int paidFor)
        {
            var effects = tech.GetEffects(EffectCode.XFor1, EffectInheritance.Invisible);

            if (effects.Count == 0)
            {
                return paidFor;
            }

            var effect = effects.OrderByDescending(x => (decimal)(int)x.Value[0] / (int)x.Value[1]).First();
            return paidFor * (int)effect.Value[0] / (int)effect.Value[1];
        }

        /// <summary>
        ///     Returns the total unit for the price of paidFor
        /// </summary>
        /// <param name="tech"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public virtual int GetXForYPaidFor(ITechnologyManager tech, int total)
        {
            var effects = tech.GetEffects(EffectCode.XFor1, EffectInheritance.Invisible);

            if (effects.Count == 0)
            {
                return total;
            }

            var effect = effects.OrderByDescending(x => (decimal)(int)x.Value[0] / (int)x.Value[1]).First();
            return (int)Math.Ceiling((decimal)total / (int)effect.Value[0] * (int)effect.Value[1]);
        }

        public virtual Resource GetInitialCityResources()
        {            
            return new Resource(crop: ResourceCropCap(1), gold: 0, iron: 0, wood: ResourceWoodCap(1), labor: 80);
        }

        public virtual byte GetInitialCityRadius()
        {
            return 5;
        }

        public virtual decimal GetInitialAp()
        {
            return 50m;
        }
    }
}