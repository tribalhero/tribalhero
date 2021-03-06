﻿using System;
using Game.Data.Stronghold;

namespace Game.Data.Tribe.EventArguments
{
    public class TribeEventArgs : EventArgs
    {
        public ITribe Tribe { get; set; }
    }

    public class TribesmanEventArgs : EventArgs
    {
        public IPlayer Player { get; set; }

        public ITribe Tribe { get; set; }
    }

    public class TribesmanKickedEventArgs : EventArgs
    {
        public ITribe Tribe { get; set; }

        public IPlayer Kicker { get; set; }

        public IPlayer Kickee { get; set; }
    }

    public class TribesmanContributedEventArgs : EventArgs
    {
        public ITribe Tribe { get; set; }
        
        public IPlayer Player { get; set; }

        public Resource Resource { get; set; }
    }

    public class StrongholdGainedEventArgs :EventArgs
    {
        public ITribe Tribe { get; set; }
        public IStronghold Stronghold { get; set; }
        public ITribe OwnBy { get; set; }
    }

    public class StrongholdLostEventArgs : EventArgs
    {
        public ITribe Tribe { get; set; }
        public IStronghold Stronghold { get; set; }
        public ITribe AttackedBy { get; set; }
    }
}
