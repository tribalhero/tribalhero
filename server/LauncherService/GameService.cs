﻿#region

using System;
using System.ServiceProcess;
using System.Threading;
using CSVToXML;
using Game;
using Game.Setup;
using NDesk.Options;
using log4net;
using log4net.Config;

#endregion

namespace LauncherService
{
    public partial class GameService : ServiceBase
    {
        private Engine engine;

        private static readonly ILog Log = LogManager.GetLogger(typeof(GameService));

        public GameService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            XmlConfigurator.Configure();

            var settingsFile = string.Empty;

            try
            {
                var p = new OptionSet {{"settings=", v => settingsFile = v}};
                p.Parse(Environment.GetCommandLineArgs());
            }
            catch(Exception)
            {
                Environment.Exit(0);
            }            

            ThreadPool.QueueUserWorkItem(o =>
                {
                    Log.Info("The game has begun");

                    Engine.AttachExceptionHandler();

                    Config.LoadConfigFile(settingsFile);
                    var kernel = Engine.CreateDefaultKernel();
                    kernel.Get<FactoriesInitializer>().CompileAndInit();
                    Converter.Go(Config.data_folder, Config.csv_compiled_folder, Config.csv_folder);

                    engine = kernel.Get<Engine>();

                    engine.Start();
                });
        }

        protected override void OnStop()
        {
            if (engine != null)
            {
                engine.Stop();
            }
        }
    }
}