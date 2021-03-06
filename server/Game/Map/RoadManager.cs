﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Comm;
using Game.Data;
using Game.Setup;
using Game.Util;

#endregion

namespace Game.Map
{
    public class RoadManager : IRoadManager
    {
        public static readonly Dictionary<string, ushort> ThemeRoadIndexes = new Dictionary<string, ushort>
        {
            // If editing this list also make sure to edit the road_start_tile_id/road_end_tile_id
            // in both server and client
            {Theme.DEFAULT_THEME_ID, 60},
            {"COBBLESTONE", 96}
        };

        [Flags]
        private enum RoadPositions
        {
            None = 0,

            TopLeft = 1,

            BottomLeft = 2,

            TopRight = 4,

            BottomRight = 8
        }

        private readonly IRegionManager regionManager;

        private readonly IObjectTypeFactory objectTypeFactory;

        private readonly IChannel channel;

        private readonly IRegionLocator regionLocator;

        private readonly ITileLocator tileLocator;

        public RoadManager(IRegionManager regionManager,
                           IObjectTypeFactory objectTypeFactory,
                           IChannel channel,
                           IRegionLocator regionLocator,
                           ITileLocator tileLocator)
        {
            this.regionManager = regionManager;
            this.objectTypeFactory = objectTypeFactory;
            this.channel = channel;
            this.regionLocator = regionLocator;
            this.tileLocator = tileLocator;

            regionManager.ObjectAdded += RegionManagerOnObjectAdded;
            regionManager.ObjectRemoved += RegionManagerOnObjectRemoved;
        }

        private void RegionManagerOnObjectAdded(object sender, ObjectEvent e)
        {
            var structure = e.GameObject as IStructure;

            if (structure == null || objectTypeFactory.IsObjectType("NoRoadRequired", structure.Type))
            {
                return;
            }

            foreach (var position in tileLocator.ForeachMultitile(structure))
            {
                CreateRoad(position.X, position.Y, structure.City.RoadTheme);
            }
        }

        private void RegionManagerOnObjectRemoved(object sender, ObjectEvent e)
        {
            var structure = e.GameObject as IStructure;
            if (structure == null)
            {
                return;
            }

            foreach (var position in tileLocator.ForeachMultitile(structure))
            {
                DestroyRoad(position.X, position.Y, structure.City.RoadTheme);
            }
        }

        private void SendUpdate(Dictionary<ushort, List<TileUpdate>> updates)
        {
            foreach (var list in updates)
            {
                var packet = new Packet(Command.RegionSetTile);
                packet.AddUInt16((ushort)list.Value.Count);
                foreach (var update in list.Value)
                {
                    packet.AddUInt32(update.X);
                    packet.AddUInt32(update.Y);
                    packet.AddUInt16(update.TileType.Value);
                }

                channel.Post("/WORLD/" + list.Key, packet);
            }
        }

        public void ChangeRoadTheme(ICity city, string oldTheme, string newTheme)
        {
            var updates = new Dictionary<ushort, List<TileUpdate>>();
            var oldThemeIndex = ThemeRoadIndexes[oldTheme];
            var newThemeIndex = ThemeRoadIndexes[newTheme];

            foreach (var position in tileLocator.ForeachTile(city.PrimaryPosition.X, city.PrimaryPosition.Y, city.Radius))
            {
                var tileType = regionManager.GetTileType(position.X, position.Y);
                if (!IsRoad(tileType))
                {
                    continue;
                }

                var newRoadType = (ushort)(tileType - oldThemeIndex + newThemeIndex);
                regionManager.SetTileType(position.X, position.Y, newRoadType, false);

                var regionId = regionLocator.GetRegionIndex(position.X, position.Y);                
                var update = new TileUpdate(position.X, position.Y, newRoadType);                                
                List<TileUpdate> list;
                if (!updates.TryGetValue(regionId, out list))
                {
                    list = new List<TileUpdate> {update};
                    updates.Add(regionId, list);
                }
                else
                {
                    updates[regionId].Add(update);
                }
            }

            SendUpdate(updates);
        }

        public void CreateRoad(uint x, uint y, string theme)
        {
            var tilePosition = new Position(x, y);
            var tiles = new List<Position>(5)
            {
                tilePosition,
                tilePosition.TopLeft(),
                tilePosition.TopRight(),
                tilePosition.BottomLeft(),
                tilePosition.BottomRight()
            };

            var updates = new Dictionary<ushort, List<TileUpdate>>();

            for (int i = 0; i < tiles.Count; i++)
            {
                var newRoadType = CalculateRoad(tiles[i].X, tiles[i].Y, i == 0, ThemeRoadIndexes[theme]);                
                if (!newRoadType.HasValue)
                {
                    continue; // Not a road here
                }
                
                ushort regionId = regionLocator.GetRegionIndex(tiles[i].X, tiles[i].Y);
                var update = new TileUpdate(tiles[i].X, tiles[i].Y, newRoadType);

                List<TileUpdate> list;
                if (!updates.TryGetValue(regionId, out list))
                {
                    list = new List<TileUpdate> {update};
                    updates.Add(regionId, list);
                }
                else
                {
                    updates[regionId].Add(update);
                }
            }

            SendUpdate(updates);
        }

        public void DestroyRoad(uint x, uint y, string themeId)
        {
            var tilePosition = new Position(x, y);
            var tiles = new List<Position>(5)
            {
                tilePosition,
                tilePosition.TopLeft(),
                tilePosition.TopRight(),
                tilePosition.BottomLeft(),
                tilePosition.BottomRight()
            };

            var updates = new Dictionary<ushort, List<TileUpdate>>();

            for (int i = 0; i < tiles.Count; i++)
            {
                ushort regionId = regionLocator.GetRegionIndex(tiles[i].X, tiles[i].Y);

                TileUpdate update;
                if (i == 0)
                {
                    update = new TileUpdate(tiles[i].X,
                                            tiles[i].Y,
                                            regionManager.RevertTileType(tiles[i].X, tiles[i].Y, false));
                }
                else
                {
                    update = new TileUpdate(tiles[i].X, tiles[i].Y, CalculateRoad(tiles[i].X, tiles[i].Y, false, ThemeRoadIndexes[themeId]));
                }

                if (!update.TileType.HasValue)
                {
                    continue;
                }

                List<TileUpdate> list;
                if (!updates.TryGetValue(regionId, out list))
                {
                    list = new List<TileUpdate> {update};
                    updates.Add(regionId, list);
                }
                else
                {
                    updates[regionId].Add(update);
                }
            }

            SendUpdate(updates);
        }

        private ushort? CalculateRoad(uint x, uint y, bool createHere, ushort startTileIndex)
        {
            if (x <= 1 || y <= 1 || x >= Config.map_width || y >= Config.map_height)
            {
                return null;
            }

            if (!createHere && !IsRoad(regionManager.GetTileType(x, y)))
            {
                return null;
            }

            var tilePosition = new Position(x, y);
            var structureAtRoadPosition = regionManager.GetObjectsInTile(x, y).OfType<IStructure>().FirstOrDefault();

            RoadPositions neighbors =
                    (ShouldConnectRoad(structureAtRoadPosition, tilePosition.TopLeft()) ? RoadPositions.TopLeft : RoadPositions.None) |
                    (ShouldConnectRoad(structureAtRoadPosition, tilePosition.BottomLeft()) ? RoadPositions.BottomLeft : RoadPositions.None) |
                    (ShouldConnectRoad(structureAtRoadPosition, tilePosition.TopRight()) ? RoadPositions.TopRight : RoadPositions.None) |
                    (ShouldConnectRoad(structureAtRoadPosition, tilePosition.BottomRight()) ? RoadPositions.BottomRight : RoadPositions.None);           

            // Select appropriate tile based on the neighbors around this tile
            ushort? roadType = null;

            if (neighbors == RoadPositions.None)
            {
                roadType = 15;
            }
            else if (structureAtRoadPosition == null)
            {
                if (neighbors == RoadPositions.TopLeft)
                {
                    roadType = 11;
                }
                else if (neighbors == RoadPositions.BottomLeft)
                {
                    roadType = 14;
                }
                else if (neighbors == RoadPositions.TopRight)
                {
                    roadType = 13;
                }
                else if (neighbors == RoadPositions.BottomRight)
                {
                    roadType = 12;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomLeft))
                {
                    roadType = 7;
                }
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.BottomRight))
                {
                    roadType = 8;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.TopRight))
                {
                    roadType = 9;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomRight))
                {
                    roadType = 0;
                }
                else if (neighbors == (RoadPositions.BottomLeft | RoadPositions.TopRight))
                {
                    roadType = 1;
                }
                else if (neighbors == (RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 10;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomLeft | RoadPositions.TopRight))
                {
                    roadType = 2;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 5;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.TopRight | RoadPositions.BottomRight))
                {
                    roadType = 3;
                }
                else if (neighbors == (RoadPositions.BottomLeft | RoadPositions.TopRight | RoadPositions.BottomRight))
                {
                    roadType = 4;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.TopRight | RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 6;
                }
            }
            else
            {
                if (neighbors == RoadPositions.TopLeft)
                {
                    roadType = 16;
                }
                else if (neighbors == RoadPositions.TopRight)
                {
                    roadType = 17;
                }                
                else if (neighbors == RoadPositions.BottomLeft)
                {
                    roadType = 18;
                }
                else if (neighbors == RoadPositions.BottomRight)
                {
                    roadType = 19;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomLeft))
                {
                    roadType = 20;
                }
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.BottomRight))
                {
                    roadType = 21;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.TopRight))
                {
                    roadType = 22;
                }
                else if (neighbors == (RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 23;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomRight))
                {
                    roadType = 24;
                }                
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.BottomLeft))
                {
                    roadType = 25;
                }                
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.TopLeft | RoadPositions.BottomLeft))
                {
                    roadType = 26;
                }
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.TopLeft | RoadPositions.BottomRight))
                {
                    roadType = 27;
                }
                else if (neighbors == (RoadPositions.TopRight | RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 28;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 29;
                }
                else if (neighbors == (RoadPositions.TopLeft | RoadPositions.TopRight | RoadPositions.BottomLeft | RoadPositions.BottomRight))
                {
                    roadType = 30;
                }
            }

            if (!roadType.HasValue)
            {
                return null;
            }

            roadType += startTileIndex;
            regionManager.SetTileType(x, y, roadType.Value, false);

            return roadType;
        }

        public bool IsRoad(uint x, uint y)
        {
            return IsRoad(regionManager.GetTileType(x, y));
        }

        private bool ShouldConnectRoad(IStructure sourceStructure, Position position)
        {
            if (!IsRoad(position.X, position.Y))
            {
                return false;
            }

            if (sourceStructure == null)
            {
                return true;
            }

            var structureAtNeighborRoad = regionManager.GetObjectsInTile(position.X, position.Y)
                                                       .OfType<IStructure>()
                                                       .FirstOrDefault();

            return structureAtNeighborRoad == null;
        }

        private bool IsRoad(ushort tileId)
        {
            return (tileId >= Config.road_start_tile_id && tileId <= Config.road_end_tile_id);
        }

        #region Nested type: TileUpdate

        /// <summary>
        ///     Simple wrapper to keep track of tiles that were updated so we can send it in one shot to the client.
        /// </summary>
        private class TileUpdate
        {
            public TileUpdate(uint x, uint y, ushort? tileType)
            {
                X = x;
                Y = y;
                TileType = tileType;
            }

            public uint X { get; set; }

            public uint Y { get; set; }

            public ushort? TileType { get; set; }
        }

        #endregion
    }
}