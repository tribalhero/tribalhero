﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Data;
using Game.Database;
using Game.Logic;
using Game.Util;

namespace Game.Module {
    public class IldeChecker : ISchedule {
        const double ILDE_HOURS = 14 * 24;

        public void Start()
        {
            Callback(null);
        }

        #region ISchedule Members

        public DateTime Time { get; private set; }
        public bool IsScheduled { get; set; }

        public void Callback(object custom) {

            using (var reader =
                    Global.DbManager.ReaderQuery(
                                                 string.Format(
                                                               "SELECT * FROM `{0}` WHERE TIMEDIFF(NOW(), `last_login`) > '{1}:00:00.000000'",
                                                               Player.DB_TABLE, ILDE_HOURS),
                                                 new DbColumn[] { })) {
                while (reader.Read()) {
                    Player player;
                    using (new MultiObjectLock((uint)reader["id"], out player))
                    {
                        foreach (City city in player.GetCityList())
                        {
                            if (!city.IsDeleting)
                            {
                                city.IsDeleting = true;
                                CityRemover cr = new CityRemover(city.Id);
                                cr.Start();
                            }
                        }
                    }
                }
            }
            Time = DateTime.UtcNow.AddHours(ILDE_HOURS);
            Global.Scheduler.Put(this);
        }

        #endregion
    }
}
