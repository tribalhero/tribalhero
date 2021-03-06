﻿using System.Collections.Generic;
using Game.Database;
using Persistance;

namespace Game.Module.Remover
{
    public class NewbieIdleSelector : IPlayerSelector
    {
        private readonly IDbManager dbManager;

        public NewbieIdleSelector(IDbManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public IEnumerable<uint> GetPlayerIds()
        {
            using (var reader = dbManager.ReaderQuery(string.Format(@"
                                                                                SELECT DISTINCT players.id player_id 
                                                                                FROM players 
                                                                                INNER JOIN cities ON cities.player_id = players.id 
                                                                                WHERE  
                                                                                    (
                                                                                        (cities.deleted = 0 AND cities.VALUE <= 9 AND players.last_login < DATE_SUB(Utc_timestamp(), INTERVAL 3 DAY))
                                                                                     OR (cities.deleted = 0 AND cities.VALUE <= 15 AND players.last_login < DATE_SUB(Utc_timestamp(), INTERVAL 7 DAY))
                                                                                    )
                                                                                    AND players.online = 0                                                                             
                                                                                    AND (SELECT COUNT(*) 
                                                                                         FROM cities c 
                                                                                         WHERE c.player_id = players.id AND c.deleted = 0
                                                                                         GROUP BY c.player_id) = 1 
                                                                                ORDER BY players.id ASC"), new DbColumn[] {}))
            {
                while (reader.Read())
                {
                    yield return (uint)reader["player_id"];
                }
            }
        }
    }
}