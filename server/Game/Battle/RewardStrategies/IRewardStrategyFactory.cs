using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Stronghold;

namespace Game.Battle.RewardStrategies
{
    public interface IRewardStrategyFactory
    {
        CityRewardStrategy CreateCityRewardStrategy(ICity city);

        StrongholdRewardStrategy CreateStrongholdRewardStrategy(IStronghold stronghold);

        BarbarianTribeRewardStrategy CreateBarbarianTribeRewardStrategy(IBarbarianTribe barbarianTribe);
    }
}