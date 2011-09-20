﻿using System;
using System.IO;
using Game.Data;
using Game.Setup;

namespace Testing
{
    public class TestBase
    {
        public TestBase()
        {
            LoadConfigFile();
            Config.seconds_per_unit = 1;
        }

        protected void LoadConfigFile(string settingsFile = null)
        {
            if (string.IsNullOrEmpty(settingsFile))
            {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

                while (true)
                {
                    var name = dir.Name;

                    if ((name.Equals("TestResults", StringComparison.OrdinalIgnoreCase) && !dir.Parent.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) &&
                         !dir.Parent.Name.Equals("Release", StringComparison.OrdinalIgnoreCase)) || name.Equals("Testing", StringComparison.OrdinalIgnoreCase))
                    {
                        settingsFile = string.Format(@"{0}\conf\settings.ini", dir.Parent.FullName);
                        break;
                    }

                    dir = dir.Parent;
                    if (dir == null)
                        throw new Exception("Unable to locate testing project directory");
                }
            }

            Config.LoadConfigFile(settingsFile);
        }
    }
}