﻿using Game.Map;

namespace Game.Data
{
    public interface ICityFactory
    {
        ICity CreateCity(uint id, IPlayer owner, string name, Position position, Resource resource, byte radius, decimal ap, string defaultTheme, string roadTheme, string wallTheme, string troopTheme);

        ICity CreateCity(uint id, IPlayer owner, string name, Position position, ILazyResource resource, byte radius, decimal ap, string defaultTheme, string roadTheme, string wallTheme, string troopTheme);
    }
}
