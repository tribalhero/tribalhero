﻿using System;
using System.Linq;
using Game.Data;
using Game.Logic.Actions;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;
using NDesk.Options;

namespace Game.Comm.CmdLine_Commands
{
    class CityCommandLineModule : CommandLineModule
    {
        public override void RegisterCommands(CommandLineProcessor processor)
        {
            processor.RegisterCommand("renamecity", RenameCity, true);
            processor.RegisterCommand("removestructure", RemoveStructure, true);
        }

        public string RemoveStructure(Session session, String[] parms)
        {
            bool help = false;
            uint x = 0;
            uint y = 0;

            try
            {
                var p = new OptionSet
                        {
                                { "?|help|h", v => help = true }, 
                                { "x=", v => x = uint.Parse(v.TrimMatchingQuotes()) },
                                { "y=", v => y = uint.Parse(v.TrimMatchingQuotes()) }
                        };
                p.Parse(parms);
            }
            catch (Exception)
            {
                help = true;
            }

            if (help || x == 0 || y == 0)
                return "removestructure --x=### --y=###";

            Region region = Global.World.GetRegion(x, y);
            if (region == null)
                return "Invalid coordinates";

            var structure = region.GetObjects(x, y).OfType<Structure>().FirstOrDefault();
            
            if (structure == null)
            {                
                return "No structures found at specified coordinates";
            }

            using (Concurrency.Current.Lock(structure.City))
            {
                var removeAction = new StructureSelfDestroyPassiveAction(structure.City.Id, structure.ObjectId);
                var result = structure.City.Worker.DoPassive(structure.City, removeAction, false);
                
                if (result != Error.Ok)
                {
                    return string.Format("Error: {0}", result);
                }
            }

            return "OK!";
        }

        public string RenameCity(Session session, String[] parms)
        {
            bool help = false;
            string cityName = string.Empty;
            string newCityName = string.Empty;

            try
            {
                var p = new OptionSet
                        {
                                { "?|help|h", v => help = true }, 
                                { "city=", v => cityName = v.TrimMatchingQuotes() },
                                { "newname=", v => newCityName = v.TrimMatchingQuotes() }
                        };
                p.Parse(parms);
            }
            catch (Exception)
            {
                help = true;
            }

            if (help || string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(newCityName))
                return "renamecity --cityr=city --newname=name";

            uint cityId;
            if (!Global.World.FindCityId(cityName, out cityId))
                return "City not found";

            City city;
            using (Concurrency.Current.Lock(cityId, out city))
            {
                if (city == null)
                    return "City not found";

                // Verify city name is valid
                if (!City.IsNameValid(newCityName))
                {
                    return "City name is invalid";
                }

                lock (Global.World.Lock)
                {
                    // Verify city name is unique
                    if (Global.World.CityNameTaken(newCityName))
                    {
                        return "City name is already taken";
                    }

                    city.BeginUpdate();
                    city.Name = newCityName;
                    city.EndUpdate();
                }
            }

            return "OK!";
        }
    }
}