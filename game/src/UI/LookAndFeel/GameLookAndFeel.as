﻿package src.UI.LookAndFeel
{
	import org.aswing.*;
	import org.aswing.border.*;
	import org.aswing.geom.*;
	import org.aswing.colorchooser.*;
	import org.aswing.ext.*;
	import org.aswing.plaf.*;
	import org.aswing.plaf.basic.background.*;

	public class GameLookAndFeel extends GameGraphicLookAndFeel
	{
		public function GameLookAndFeel()
		{
			super();
		}

		public static function changeClass(obj: Component, classes: *) : void {
			var ui: ComponentUI = obj.getUI();

			var keys: Array = (classes is String) ? classes.split(" ") : classes;

			for each(var key: String in keys) {
				if (key == "") continue;

				var keyValueList: Array = UIManager.getDefaults().get("Class." + key);
				
				if (keyValueList == null) return;

				for (var i:int = 0; i < keyValueList.length; i += 2) {
					ui.putDefault(keyValueList[i], keyValueList[i + 1]);
				}
			}

			obj.setUI(ui);
		}

		public static function getClassAttribute(className: String, attribute: String) : * {
			var keyValueList: Array = UIManager.getDefaults().get("Class." + className);

			if (keyValueList == null) return null;

			for(var i:Number = 0; i < keyValueList.length; i += 2) {
				if (keyValueList[i] == attribute)
				return keyValueList[i + 1];
			}

			return null;
		}

		override protected function initComponentDefaults(table:UIDefaults):void{
			super.initComponentDefaults(table);

			var comDefaults:Array = [
			
			/* DEFAULT COMPONENTS */
			"LabelButton.foreground", new ASColorUIResource(0x0066cc), 
			"LabelButton.font", new ASFontUIResource("Arial", 11, false, false, true),

			/* CLASSES */			
			"Class.darkText", [
			"Label.font", new ASFontUIResource("Arial", 12, true),
			"Label.foreground", new ASColorUIResource(0x000000)				
			],
			
			"Class.header", [
			"Label.font", new ASFontUIResource("Arial", 13, true),
			"Label.foreground", new ASColorUIResource(0xFFFFFF)
			],

			"Class.darkHeader", [
			"Label.font", new ASFontUIResource("Arial", 13, true),
			"Label.foreground", new ASColorUIResource(0x000000),

			"LabelButton.font", new ASFontUIResource("Arial", 13, true, false, true),
			"LabelButton.foreground", new ASColorUIResource(0x000000)			
			],

			"Class.darkLargeText", [
			"Label.font", new ASFontUIResource("Arial", 13, false),
			"Label.foreground", new ASColorUIResource(0x000000)
			],

			"Class.Tooltip.text", [
			"Label.font", new ASFontUIResource("Arial", 13, false),
			"Label.foreground", new ASColorUIResource(0xFFFFFF),
			"TextArea.font", new ASFontUIResource("Arial", 13, false),
			"TextArea.foreground", new ASColorUIResource(0xFFFFFF),
			"TextField.font", new ASFontUIResource("Arial", 13, false),
			"TextField.foreground", new ASColorUIResource(0xFFFFFF)
			],
			
			"Class.Tooltip.italicsText", [
			"Label.font", new ASFontUIResource("Arial", 13, false, true),
			"Label.foreground", new ASColorUIResource(0xFFFFFF),
			"TextArea.font", new ASFontUIResource("Arial", 13, false, true),
			"TextArea.foreground", new ASColorUIResource(0xFFFFFF),
			"TextField.font", new ASFontUIResource("Arial", 13, false, true),
			"TextField.foreground", new ASColorUIResource(0xFFFFFF)
			],			
			
			"Class.Message", [
			"MultilineLabel.font", new ASFontUIResource("Arial", 13, false),
			"Label.font", new ASFontUIResource("Arial", 13, false),
			"TextArea.font", new ASFontUIResource("Arial", 13, false),
			],
			
			"Class.Console.text", [
			"Label.font", new ASFontUIResource("Courier New", 12, false),
			"Label.foreground", new ASColorUIResource(0xFFFFFF),
			"TextArea.font", new ASFontUIResource("Courier New", 12, false),
			"TextArea.foreground", new ASColorUIResource(0xFFFFFF),
			"TextField.font", new ASFontUIResource("Courier New", 12, false),
			"TextField.foreground", new ASColorUIResource(0xFFFFFF)
			],			

			"Class.Message.preview", [
			"TextField.foreground", new ASColorUIResource(0x707070),
			"Label.foreground", new ASColorUIResource(0x707070),
			],
			"Class.Message.unread", [
			"TextField.font", new ASFontUIResource("Arial", 12, true),
			"TextField.foreground", new ASColorUIResource(0x000000),
			"Label.font", new ASFontUIResource("Arial", 12, true),
			"Label.foreground", new ASColorUIResource(0x000000)
			],
			"Class.Message.read", [
			"TextField.font", new ASFontUIResource("Arial", 12, false),
			"TextField.foreground", new ASColorUIResource(0x000000),
			"Label.font", new ASFontUIResource("Arial", 12, false),
			"Label.foreground", new ASColorUIResource(0x000000)
			],

			"Class.Label.very_small", [
			"Label.font", new ASFontUIResource("Arial", 9, false),
			"TextArea.font", new ASFontUIResource("Arial", 9, false),
			],
			"Class.Label.small", [
			"Label.font", new ASFontUIResource("Arial", 11, false),
			"TextArea.font", new ASFontUIResource("Arial", 11, false),
			],
			"Class.Label.error", [
			"Label.font", new ASFontUIResource("Arial", 13, false),
			"Label.foreground", new ASColorUIResource(0xFF0000)
			],
			"Class.Label.success", [
			"Label.font", new ASFontUIResource("Arial", 13, false),
			"Label.foreground", new ASColorUIResource(0x00FF00)
			],

			"Class.Form.label", [
			"Label.font", new ASFontUIResource("Arial", 11, true)
			],

			"Class.Button.action", [
			"Button.background", new ASColorUIResource(0xe1dcbd),
			"Button.foreground", new ASColorUIResource(0x000000),
			"Button.mideground", table.get("controlMide"),
			"Button.colorAdjust", new UIStyleTune(0, -0.06, 1, 0.22, 5),
			"Button.opaque", true,
			"Button.focusable", true,
			"Button.font", new ASFontUIResource("Arial", 11, false, false, false),
			"Button.bg", ButtonBackground,
			"Button.margin", new InsetsUIResource(2, 3, 2, 3),
			"Button.textShiftOffset", 0,
			"Button.textFilters", new ArrayUIResource([])
			]
			];

			table.putDefaults(comDefaults);
		}

	}

}
