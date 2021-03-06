using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amib.Threading;
using Dawn.Net.Sockets;
using Game.Battle;
using Game.Battle.CombatObjects;
using Game.Battle.Reporting;
using Game.Comm;
using Game.Comm.Channel;
using Game.Comm.ProcessorCommands;
using Game.Comm.QueueCommands;
using Game.Comm.Thrift;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Forest;
using Game.Data.Store;
using Game.Data.Stronghold;
using Game.Data.Tribe;
using Game.Logic;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Logic.Triggers;
using Game.Map;
using Game.Module;
using Game.Setup;
using Game.Setup.DependencyInjection;
using Game.Util;
using Game.Util.Locking;
using Persistance;
using Persistance.Managers;
using SimpleInjector;
using Thrift.Server;
using Thrift.Transport;

namespace Game
{
    public class GameModule : IKernelModule
    {
        public void Load(Container container)
        {
            container.Options.ConstructorResolutionBehavior = new MostResolvableConstructorBehavior(container);

            container.Register<IKernel>(() => new Ioc(container), Lifestyle.Singleton);

            #region World/Map

            var worldRegistration = Lifestyle.Singleton.CreateRegistration<World>(container);
            container.AddRegistration(typeof(IWorld), worldRegistration);
            container.AddRegistration(typeof(IGameObjectLocator), worldRegistration);

            container.Register<IGlobal, Global>(Lifestyle.Singleton);
            container.Register<IRegionManager, RegionManager>(Lifestyle.Singleton);
            container.Register<IRegionLocator, RegionLocator>(Lifestyle.Singleton);
            container.Register<RegionObjectList>();
            container.Register<ICityManager, CityManager>(Lifestyle.Singleton);
            container.Register<IForestManager, ForestManager>(Lifestyle.Singleton);
            container.Register<IRoadManager, RoadManager>(Lifestyle.Singleton);
            container.Register<IRoadPathFinder, RoadPathFinder>();

            container.Register<IThemeManager, ThemeManager>(Lifestyle.Singleton);
            container.Register<IStoreManager, StoreManager>(Lifestyle.Singleton);

            container.Register<StoreSync>(Lifestyle.Singleton);

            #endregion

            #region General Comms

            container.Register<BlockingBufferManager>(() => new BlockingBufferManager(2048, 7500), Lifestyle.Singleton);
            container.Register<SocketAwaitablePool>(() => new SocketAwaitablePool(100), Lifestyle.Singleton);
            container.Register<IChannel, Channel>(Lifestyle.Singleton);
            container.Register<IPolicyServer, PolicyServer>(Lifestyle.Singleton);
            container.Register<INetworkServer, AsyncTcpServer>(Lifestyle.Singleton);
            container.Register<TServer>(() =>
            {
                var logger = LoggerFactory.Current.GetLogger<TSimpleServer>();
                return new TSimpleServer(new Notification.Processor(container.GetInstance<NotificationHandler>()), new TServerSocket(46000), logger.Warn);
            }, Lifestyle.Singleton);

            container.Register<Chat>(Lifestyle.Singleton);
            container.Register<ICityTriggerManager, CityTriggerManager>(Lifestyle.Singleton);

            container.Register<IQueueListener, QueueListener>(Lifestyle.Singleton);
            container.Register<IQueueCommandProcessor>(() => new QueueCommandProcessor(container.GetInstance<PlayerQueueCommandsModule>()), Lifestyle.Singleton);

            if (Config.database_load_players)
            {
                container.Register<ILoginHandler, MainSiteLoginHandler>(Lifestyle.Singleton);
            }
            else
            {
                container.Register<ILoginHandler, DummyLoginHandler>(Lifestyle.Singleton);
            }

            #endregion
            
            #region Tribes

            container.Register<ITribeManager, TribeManager>(Lifestyle.Singleton);
            container.Register<ITribeLogger, TribeLogger>();

            #endregion

            #region Locking

            container.Register<DefaultMultiObjectLock.Factory>(() => () => new DefaultMultiObjectLock(container.GetInstance<IDbManager>()), Lifestyle.Singleton);

            container.Register<ILocker>(() =>
            {
                DefaultMultiObjectLock.Factory multiObjectLockFactory = () => new DefaultMultiObjectLock(container.GetInstance<IDbManager>());

                return new DefaultLocker(multiObjectLockFactory,
                                         () => new CallbackLock(multiObjectLockFactory),
                                         container.GetInstance<IGameObjectLocator>());
            }, Lifestyle.Singleton);

            #endregion

            #region Formulas

            container.Register<Formula>(Lifestyle.Singleton);
            container.Register<RequirementFormula>(Lifestyle.Singleton);

            #endregion

            #region CSV Factories

            container.Register<FactoriesInitializer>(Lifestyle.Singleton);
            container.Register<ActionRequirementFactory>(Lifestyle.Singleton);
            container.Register<IStructureCsvFactory, StructureCsvFactory>(Lifestyle.Singleton);
            container.Register<EffectRequirementFactory>(Lifestyle.Singleton);
            container.Register<InitFactory>(Lifestyle.Singleton);            
            container.Register<PropertyFactory>(Lifestyle.Singleton);
            container.Register<IRequirementCsvFactory, RequirementCsvFactory>(Lifestyle.Singleton);
            container.Register<TechnologyFactory>(Lifestyle.Singleton);
            container.Register<UnitFactory>(Lifestyle.Singleton);
            container.Register<IObjectTypeFactory, ObjectTypeFactory>(Lifestyle.Singleton);
            container.Register<UnitModFactory>(Lifestyle.Singleton);
            container.Register<MapFactory>(Lifestyle.Singleton);

            #endregion

            #region Database

            container.Register<IDbManager>(() => new MySqlDbManager(LoggerFactory.Current.GetLogger<IDbManager>(),
                                      Config.database_host,
                                      Config.database_username,
                                      Config.database_password,
                                      Config.database_database,
                                      Config.database_timeout,
                                      Config.database_max_connections,
                                      Config.database_verbose), Lifestyle.Singleton);

            #endregion

            #region Battle

            container.Register<IBattleReport, BattleReport>();

            container.Register<IBattleManagerFactory, BattleManagerFactory>();

            container.Register<IBattleReportWriter, SqlBattleReportWriter>();

            container.Register<ICombatUnitFactory, CombatUnitFactory>(Lifestyle.Singleton);
            
            container.Register<IBattleFormulas, BattleFormulas>(Lifestyle.Singleton);

            #endregion

            #region Processor

            container.Register<CommandLineProcessor>(() => new CommandLineProcessor(container.GetInstance<AssignmentCommandLineModule>(),
                                                                                    container.GetInstance<PlayerCommandLineModule>(),
                                                                                    container.GetInstance<CityCommandLineModule>(),
                                                                                    container.GetInstance<ResourcesCommandLineModule>(),
                                                                                    container.GetInstance<TribeCommandLineModule>(),
                                                                                    container.GetInstance<StrongholdCommandLineModule>(),
                                                                                    container.GetInstance<RegionCommandsLineModule>(),
                                                                                    container.GetInstance<BarbarianTribeCommandsLineModule>(),
                                                                                    container.GetInstance<SystemCommandLineModule>(),
                                                                                    container.GetInstance<BarbarianTribeCommandsLineModule>()),
                                                     Lifestyle.Singleton);

            container.Register<IProcessor>(() => new Processor(container.GetInstance<AssignmentCommandsModule>(),
                                                               container.GetInstance<BattleCommandsModule>(),
                                                               container.GetInstance<EventCommandsModule>(),
                                                               container.GetInstance<ChatCommandsModule>(),
                                                               container.GetInstance<CommandLineCommandsModule>(),
                                                               container.GetInstance<LoginCommandsModule>(),
                                                               container.GetInstance<MarketCommandsModule>(),
                                                               container.GetInstance<MiscCommandsModule>(),
                                                               container.GetInstance<PlayerCommandsModule>(),
                                                               container.GetInstance<RegionCommandsModule>(),
                                                               container.GetInstance<StructureCommandsModule>(),
                                                               container.GetInstance<TribeCommandsModule>(),
                                                               container.GetInstance<TribesmanCommandsModule>(),
                                                               container.GetInstance<StrongholdCommandsModule>(),
                                                               container.GetInstance<ProfileCommandsModule>(),
                                                               container.GetInstance<TroopCommandsModule>(),
                                                               container.GetInstance<StoreCommandsModule>()),
                                           Lifestyle.Singleton);

            #endregion
            
            #region Utils

            container.Register<SmartThreadPool>(() => new SmartThreadPool(), Lifestyle.Singleton);

            container.Register<IScheduler>(() =>
            {
                ITaskScheduler taskScheduler;
                if (Config.scheduler_use_smartthreadpoool)
                {
                    var threadPool = container.GetInstance<SmartThreadPool>();
                    taskScheduler = new SmartThreadPoolTaskScheduler(threadPool.CreateWorkItemsGroup(Config.scheduler_threads));
                }
                else
                {
                    taskScheduler = new LimitedConcurrencyLevelTaskScheduler.TaskFactory(new LimitedConcurrencyLevelTaskScheduler(Config.scheduler_threads));
                }

                return new ThreadedScheduler(taskScheduler);
            }, Lifestyle.Singleton);
            container.Register<ITileLocator, TileLocator>();
            container.Register<GetRandom>(() => new Random().Next);
            container.Register<Procedure>(Lifestyle.Singleton);
            container.Register<BattleProcedure>(Lifestyle.Singleton);
            container.Register<StrongholdBattleProcedure>(Lifestyle.Singleton);
            container.Register<CityBattleProcedure>(Lifestyle.Singleton);
            container.Register<BarbarianTribeBattleProcedure>(Lifestyle.Singleton);
            container.Register<CallbackProcedure>(Lifestyle.Singleton);
            container.Register<Random>(() => new Random());
            container.Register<ISystemVariableManager, SystemVariableManager>(Lifestyle.Singleton);
            container.Register<SystemVariablesUpdater>(Lifestyle.Singleton);            

            #endregion

            #region Stronghold

            container.Register<IStrongholdManager, StrongholdManager>(Lifestyle.Singleton);
            
            container.Register<IStrongholdConfigurator>(() => new StrongholdConfigurator(
                                                                      new NameGenerator(Path.Combine(Config.maps_folder, "strongholdnames.txt")),
                                                                      container.GetInstance<MapFactory>(),
                                                                      container.GetInstance<ITileLocator>(),
                                                                      container.GetInstance<IRegionManager>()), Lifestyle.Singleton);            
            if (Config.stronghold_bypass_activation)
            {
                container.Register<IStrongholdActivationCondition, DummyActivationCondition>();
            }
            else
            {
                container.Register<IStrongholdActivationCondition, StrongholdActivationCondition>();
            }
            container.Register<StrongholdActivationChecker>(Lifestyle.Singleton);
            container.Register<StrongholdChecker>(Lifestyle.Singleton);

            container.Register<IStrongholdFactory, StrongholdFactory>();
            container.Register<IStrongholdManagerLogger, StrongholdManagerLogger>();

            #endregion

            #region Barbarian Tribe

            container.Register<IBarbarianTribeManager, BarbarianTribeManager>(Lifestyle.Singleton);
            container.Register<IBarbarianTribeConfigurator, BarbarianTribeWeightedConfigurator>(Lifestyle.Singleton);
            container.Register<IBarbarianTribeFactory, BarbarianTribeFactory>();
            container.Register<BarbarianTribeChecker>(Lifestyle.Singleton);

            #endregion

            #region City

            container.Register<ICityChannel, CityChannel>(Lifestyle.Singleton);
            container.Register<ICityFactory, CityFactory>(Lifestyle.Singleton);
            container.Register<IGameObjectFactory, GameObjectFactory>(Lifestyle.Singleton);

            #endregion

            var allTypes = this.GetType().Assembly.GetTypes();
            var allRegistrations = container.GetCurrentRegistrations().ToDictionary(p => p.ServiceType, p => p.Registration);
            foreach (var factoryInterfaceType in allTypes.Where(type => type.IsInterface && type.Name.EndsWith("Factory")))
            {
                var implementingTypes = allTypes.Where(type => type.IsInterface == false &&
                                                               type.IsAbstract == false &&
                                                               type.IsGenericTypeDefinition == false &&
                                                               factoryInterfaceType.IsAssignableFrom(type))
                                                .ToArray();

                if (implementingTypes.Length == 0)
                {
                    throw new Exception(string.Format("Factory without any implementations. Implement type {0}", factoryInterfaceType.Name));
                }

                // Check if already registered
                if (allRegistrations.ContainsKey(factoryInterfaceType))
                {
                    continue;
                }

                if (implementingTypes.Length > 1)
                {
                    throw new Exception(string.Format("Found interface factory that has more than 1 implementation and was not registered {0}", factoryInterfaceType.Name));
                }

                container.Register(factoryInterfaceType, implementingTypes.First(), Lifestyle.Singleton);
            }                
        }
    }
}