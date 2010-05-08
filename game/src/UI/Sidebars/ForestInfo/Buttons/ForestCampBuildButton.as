﻿
package src.UI.Sidebars.ForestInfo.Buttons {
	import flash.events.Event;
	import flash.events.MouseEvent;
	import src.Global;
	import src.Objects.Actions.ForestCampBuildAction;
	import src.Objects.Factories.*;
	import src.Objects.Forest;
	import src.Objects.GameObject;
	import src.Objects.Actions.ActionButton;
	import src.Objects.Troop.*;
	import src.UI.Cursors.*;
	import src.UI.Dialog.ForestLaborDialog;
	import src.UI.Sidebars.CursorCancel.CursorCancelSidebar;
	import src.UI.Tooltips.TextTooltip;

	public class ForestCampBuildButton extends ActionButton
	{
		private var tooltip: TextTooltip;

		public function ForestCampBuildButton(parentObj: GameObject)
		{
			super(new ForestCampBuildButton_base(), parentObj);

			tooltip = new TextTooltip("Gather Lumber");

			ui.addEventListener(MouseEvent.CLICK, onMouseClick);
			ui.addEventListener(MouseEvent.MOUSE_OVER, onMouseOver);
			ui.addEventListener(MouseEvent.MOUSE_MOVE, onMouseOver);
			ui.addEventListener(MouseEvent.MOUSE_OUT, onMouseOut);
		}

		public function onMouseOver(event: MouseEvent):void
		{
			tooltip.show(this);
		}

		public function onMouseOut(event: MouseEvent):void
		{
			tooltip.hide();
		}

		public function onMouseClick(event: Event):void
		{
			if (enabled)
			{
				// Check to see if this is being called from the forest or from the lumbermill. If this is from the forest, then the parent action will be null
				var forestCampBuildAction: ForestCampBuildAction = parentAction as ForestCampBuildAction;

				var campTypes: Array = ObjectFactory.getList("ForestCamp");
				if (forestCampBuildAction == null) {
					var laborDialog: ForestLaborDialog = new ForestLaborDialog(Global.gameContainer.selectedCity.id, parentObj as Forest, onSetLabor);
					laborDialog.show();
				} else {
					var cursor: GroundForestCursor = new GroundForestCursor(Global.gameContainer.selectedCity.id, function(forest: Forest) : void {
						var laborDialog: ForestLaborDialog = new ForestLaborDialog(Global.gameContainer.selectedCity.id, forest, onSetLabor);
						laborDialog.show();
					});

					var sidebar: CursorCancelSidebar = new CursorCancelSidebar();
					Global.gameContainer.setSidebar(sidebar);
				}
			}

			event.stopImmediatePropagation();
		}

		private function onSetLabor(dlg: ForestLaborDialog) : void {
			// This is kind of a hack since we need to know the campType.
			var campTypes: Array = ObjectFactory.getList("ForestCamp");

			Global.mapComm.Object.createForestCamp(dlg.getForest().objectId , Global.gameContainer.selectedCity.id, campTypes[0], dlg.getCount());
			
			dlg.getFrame().dispose();
		}
	}

}

