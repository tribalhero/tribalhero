package src.Objects.Stronghold 
{
    import src.Constants;
    import src.Objects.SimpleGameObject;
    import src.Objects.SimpleObject;
    import src.Objects.States.GameObjectState;
    import src.UI.Components.RichLabel;
    import src.Util.StringHelper;

    public class Stronghold extends SimpleGameObject
	{
        public static const SIZE: int = 2;

		public static const BATTLE_STATE_NONE: int = 0;
		public static const BATTLE_STATE_GATE: int = 1;
		public static const BATTLE_STATE_MAIN: int = 2;
		
		public var level: int;
		public var tribeId: int;
        public var gateMax: int;
		public var strongholdName: String;
        public var theme: String;
		public var id: int;
        
		public function Stronghold(type: int, state: GameObjectState, objX: int, objY: int, size: int, groupId: int, objectId: int, level: int, tribeId: int, gateMax: int, theme: String) {
			super(type, state, objX, objY, size, groupId, objectId);
			this.id = objectId;
			this.level = level;
			this.tribeId = tribeId;
            this.theme = theme;
            this.gateMax = gateMax;
            mapPriority = Constants.mapObjectPriority.stronghold;
		}
		
		override public function copy(obj:SimpleObject):void
		{
			super.copy(obj);
			var strongholdObj: Stronghold = obj as Stronghold;
			id = strongholdObj.id;
			level = strongholdObj.level;
            theme = strongholdObj.theme;
			tribeId = strongholdObj.tribeId;
		}
		
		public static function gateToString(limit: int, value: int) : String {
			return value.toString() + "/" + limit.toString();
		}		
		
		public static function getBattleStateString(strongholdInfo: *, rows: int = 0, cols: int = 0): RichLabel {			
			if (strongholdInfo.battleState == BATTLE_STATE_MAIN) {
				return new RichLabel(StringHelper.localize("STRONGHOLD_STATE_MAIN_ATTACK", strongholdInfo.gateOpenTo.id, strongholdInfo.gateOpenTo.name, strongholdInfo.battleId), rows, cols);				
			}
			
			if (strongholdInfo.battleState == BATTLE_STATE_GATE) {
				return new RichLabel(StringHelper.localize("STRONGHOLD_STATE_GATE_ATTACK"), rows, cols);
			}
			
			return new RichLabel("", rows, cols);
		}
	}
}