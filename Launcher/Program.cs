#region

using System;
using CSVToXML;
using Game;
using Game.Setup;
using NDesk.Options;
using log4net;
using log4net.Config;

#endregion

namespace Launcher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            ILog logger = LogManager.GetLogger(typeof(Program));
            logger.Info("#######################################");

            bool help = false;
            string settingsFile = string.Empty;

            try
            {                
                var p = new OptionSet { { "?|help|h", v => help = true }, { "settings=", v => settingsFile = v }, };
                p.Parse(Environment.GetCommandLineArgs());
            }
            catch (Exception e)
            {
                logger.Error(e);
                Environment.Exit(0);
            }

            if (help)
            {
                logger.Info("[--settings=settings.ini]");
                Environment.Exit(0);
            }

            Config.LoadConfigFile(settingsFile);
            Factory.CompileConfigFiles();
 
#if DEBUG
            if (Config.database_empty)
            {
                Console.Out.Write("Are you sure you want to empty the database?(Y/N):");
                if (!Console.ReadKey().Key.ToString().ToLower().Equals("y"))
                    return;
            }
#endif

            if (!Engine.Start())
                throw new Exception("Failed to load server");

            Converter.Go(Config.data_folder, Config.csv_compiled_folder, Config.csv_folder);

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key != ConsoleKey.Q || key.Modifiers != ConsoleModifiers.Alt)
                    continue;

                Engine.Stop();
                return;
            }
        }
    }
}