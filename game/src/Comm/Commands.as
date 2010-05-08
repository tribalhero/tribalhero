/**
* ...
* @author Default
* @version 0.1
*/

package src.Comm {
	
	public class Commands {
		public static const CHANNEL_NOTIFICATION: String = "CHANNEL_NOTIFICATION";
		
		public static const INVALID: int = 1;
		
		public static const LOGIN: int = 10;
		public static const QUERY_XML: int = 11;
		public static const PLAYER_USERNAME_GET: int = 12;
		public static const CITY_USERNAME_GET: int = 13;
		
		public static const ACTION_CANCEL: int = 51;
		public static const ACTION_COMPLETE: int = 52;
		public static const ACTION_START: int = 53;
		public static const ACTION_RESCHEDULE: int = 54;
		
        public static const NOTIFICATION_ADD: int = 61;
        public static const NOTIFICATION_REMOVE: int = 62;
        public static const NOTIFICATION_UPDATE: int = 63;		
		public static const NOTIFICATION_LOCATE: int = 64;
		
		public static const REGION_GET: int = 105;
		public static const CITY_REGION_GET: int = 106;
		
		public static const OBJECT_ADD: int = 201;
		public static const OBJECT_UPDATE: int = 202;
		public static const OBJECT_REMOVE: int = 203;
		public static const OBJECT_MOVE: int = 204;
		
		public static const STRUCTURE_INFO: int = 300;
		public static const STRUCTURE_BUILD: int = 301;
		public static const STRUCTURE_UPGRADE: int = 302;
		public static const STRUCTURE_CHANGE: int = 303;
		public static const STRUCTURE_LABOR_MOVE: int = 304;
		
		public static const TECHNOLOGY_ADDED: int = 311;
		public static const TECHNOLOGY_UPGRADE: int = 312;
		public static const TECHNOLOGY_REMOVED: int = 313;
		public static const TECHNOLOGY_UPGRADED: int = 314;
		
        public static const CITY_OBJECT_ADD: int  = 451;
        public static const CITY_OBJECT_UPDATE: int  = 452;
        public static const CITY_OBJECT_REMOVE: int  = 453;
        public static const CITY_RESOURCES_UPDATE: int  = 462;
		public static const CITY_UNIT_LIST: int = 463;		
		public static const CITY_RADIUS_UPDATE: int = 465;
		public static const CITY_CREATE_INITIAL: int = 499;
		
		public static const FOREST_INFO: int = 350;
        public static const FOREST_CAMP_CREATE: int = 351;
		public static const FOREST_CAMP_REMOVE: int = 352;
		
		public static const UNIT_TRAIN: int = 501;
        public static const UNIT_UPGRADE: int = 502;
        public static const UNIT_TEMPLATE_UPGRADED: int = 503;
		
		public static const TROOP_INFO: int = 600;
		public static const TROOP_ATTACK: int = 601;
		public static const TROOP_REINFORCE: int = 602;
		public static const TROOP_RETREAT: int = 603;		
		public static const TROOP_ADDED: int = 611;
		public static const TROOP_UPDATED: int = 612;
		public static const TROOP_REMOVED: int = 613;
		public static const LOCAL_TROOP_MOVE: int = 621;
		
		public static const	MARKET_BUY: int = 901;
        public static const	MARKET_SELL: int = 902;
        public static const	MARKET_PRICES: int = 903;
		
        public static const BATTLE_SUBSCRIBE: int = 700;
        public static const BATTLE_UNSUBSCRIBE: int = 701;
        public static const BATTLE_ATTACK: int = 702;
        public static const BATTLE_REINFORCE_ATTACKER: int = 703;
        public static const BATTLE_REINFORCE_DEFENDER: int = 704;
        public static const BATTLE_ENDED: int = 705;
		public static const BATTLE_SKIPPED: int = 706;
		
	}
	
}
