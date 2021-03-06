﻿
package src.UI.Cursors {
    import System.Linq.Enumerable;

    import flash.display.*;
    import flash.events.*;
    import flash.geom.*;

    import src.*;
    import src.Map.*;
    import src.Objects.*;
    import src.Objects.Factories.*;
    import src.Objects.Prototypes.*;
    import src.UI.Components.*;
    import src.UI.Sidebars.CursorCancel.*;
    import src.Util.*;
    import src.Util.BinaryList.BinaryList;

    public class BuildStructureCursor extends MovieClip implements IDisposable
	{		
		private var objPosition: ScreenPosition = new ScreenPosition();

		private var city: City;

		private var originPoint: Point;

		private var cursor: SimpleObject;
		private var rangeCursor: GroundCircle;
		private var buildableArea: GroundCallbackCircle;
		private var structPrototype: StructurePrototype;
		private var parentObj: SimpleGameObject;
		private var type: int;
		private var level: int;
		private var tilerequirement: String;

        private var requiresRoad: Boolean;
		private var hasRoadNearby: Boolean;

        private var buildableTiles: BinaryList = new BinaryList(Position.sort, Position.compare);

		public function BuildStructureCursor(theme: String, type: int, level: int, tilerequirement: String, parentObject: SimpleGameObject):void
		{
			doubleClickEnabled = true;

			this.parentObj = parentObject;
			this.type = type;
			this.level = level;
			this.tilerequirement = tilerequirement;

			city = Global.map.cities.get(parentObj.groupId);

			Global.gameContainer.setOverlaySprite(this);
			Global.map.selectObject(null);

            requiresRoad = !ObjectFactory.isType("NoRoadRequired", type);
			structPrototype = StructureFactory.getPrototype(type, level);
			cursor = StructureFactory.getSimpleObject(theme, type, level, 0, 0, structPrototype.size);

			if (cursor == null)
			{
				Util.log("Missing building cursor " + type);
				return;
			}

            cursor.mapPriority = 0;
            cursor.alpha = 0.7;

            // Validate all tiles
            var size: int = city.radius - 1;
            for each (var position: Position in TileLocator.foreachTile(city.primaryPosition.x, city.primaryPosition.y, size)) {
                validateTile(position);
            }

			rangeCursor = new GroundCircle(structPrototype.radius, true, new ColorTransform(1.0, 1.0, 1.0, 1.0, 236, 88, 0));
			rangeCursor.alpha = 0.6;

			buildableArea = new GroundCallbackCircle(size, validateTileCallback);
			buildableArea.alpha = 0.3;

			var pos: ScreenPosition = city.primaryPosition.toScreenPosition();
            buildableArea.x = buildableArea.primaryPosition.x = pos.x;
            buildableArea.y = buildableArea.primaryPosition.y = pos.y;

			Global.map.objContainer.addObject(buildableArea, ObjectContainer.LOWER);

			var sidebar: CursorCancelSidebar = new CursorCancelSidebar(parentObj);
			Global.gameContainer.setSidebar(sidebar);

			addEventListener(MouseEvent.DOUBLE_CLICK, onMouseDoubleClick);
			addEventListener(MouseEvent.CLICK, onMouseStop, true);
			addEventListener(MouseEvent.MOUSE_MOVE, onMouseMove);
			addEventListener(MouseEvent.MOUSE_OVER, onMouseStop);
			addEventListener(MouseEvent.MOUSE_DOWN, onMouseDown);

			if (requiresRoad && !hasRoadNearby) {
				Global.gameContainer.message.showMessage("This building must be connected to a road and there are no roads available. Build roads first by using your Town Center then try again.");
			} else if (buildableTiles.size() == 0) {
				Global.gameContainer.message.showMessage("There are no spaces available to build on.");
			} else {
				Global.gameContainer.message.showMessage("Double click on a green square to build a " + structPrototype.getName().toLowerCase() + ".");
			}
		}

		public function dispose():void
		{
			Global.gameContainer.message.hide();

			if (cursor != null)
			{
				if (cursor.stage != null) {
                    Global.map.objContainer.removeObject(cursor);
                }

				if (rangeCursor.stage != null) {
                    Global.map.objContainer.removeObject(rangeCursor, ObjectContainer.LOWER);
                }
				if (buildableArea.stage != null) {
                    Global.map.objContainer.removeObject(buildableArea, ObjectContainer.LOWER);
                }
			}
		}

		private function showCursors() : void {
			if (cursor) {
                cursor.visible = true;
            }

			if (rangeCursor) {
                rangeCursor.visible = true;
            }
		}

		private function hideCursors() : void {
			if (cursor) cursor.visible = false;
			if (rangeCursor) rangeCursor.visible = false;
		}

		public function onMouseStop(event: MouseEvent):void
		{
			event.stopImmediatePropagation();
		}

		public function onMouseDoubleClick(event: MouseEvent):void
		{
			if (Point.distance(TileLocator.getPointWithZoomFactor(event.stageX, event.stageY), originPoint) > city.radius) return;

			event.stopImmediatePropagation();

			var mapPos: Position = objPosition.toPosition();
			Global.mapComm.Objects.buildStructure(parentObj.groupId, parentObj.objectId, type, level, mapPos.x, mapPos.y);

			Global.gameContainer.setOverlaySprite(null);
			Global.gameContainer.setSidebar(null);
			Global.map.selectObject(parentObj, false);
		}

		public function onMouseDown(event: MouseEvent):void
		{
			originPoint = TileLocator.getPointWithZoomFactor(event.stageX, event.stageY);
		}

		public function onMouseMove(event: MouseEvent) : void
		{
			if (event.buttonDown) return;

            // Take the mouse position but center the structure to it by aligning the cursor w/ the left most side of the structure
			var mousePos: Point = TileLocator.getPointWithZoomFactor(
                    Math.max(0, event.stageX - ((structPrototype.size-1) * Constants.tileW)/2),
                    event.stageY
            );

			var pos: ScreenPosition = TileLocator.getActualCoord(
                    Global.gameContainer.camera.currentPosition.x + mousePos.x,
                    Global.gameContainer.camera.currentPosition.y + mousePos.y);

			if (!pos.equals(objPosition))
			{
				objPosition = pos;
			
				if (rangeCursor.stage != null) {
                    Global.map.objContainer.removeObject(rangeCursor, ObjectContainer.LOWER);
                }

				if (cursor.stage != null) {
                    Global.map.objContainer.removeObject(cursor);
                }

                rangeCursor.x = rangeCursor.primaryPosition.x = pos.x;
                rangeCursor.y = rangeCursor.primaryPosition.y = pos.y;

                cursor.x = cursor.primaryPosition.x = pos.x;
                cursor.y = cursor.primaryPosition.y = pos.y;
				
				if (validateBuilding()) {
					Global.map.objContainer.addObject(rangeCursor, ObjectContainer.LOWER);
					Global.map.objContainer.addObject(cursor);
				}
			}
		}

		private function validateTile(mapPosition: Position) : Boolean {
            // Check if tile is taken
            for each (var tilePosition: Position in TileLocator.foreachMultitile(mapPosition.x, mapPosition.y, structPrototype.size)) {
                var existingObjects: Array = Global.map.regions.getObjectsInTile(tilePosition, StructureObject);
                if (existingObjects.length > 0) {
                    return false;
                }

                var tileType: int = Global.map.regions.getTileAt(tilePosition);

                // Check for tile requirement
                if (tilerequirement != "" && !ObjectFactory.isType(tilerequirement, tileType)) {
                    return false;
                }

                // Only allow buildings can require the resource tile to be built on resource tiles
                if (tilerequirement != "TileResource" && ObjectFactory.isType("TileResource", tileType)) {
                    return false;
                }

                // Within city walls?
                if (TileLocator.distance(city.primaryPosition.x, city.primaryPosition.y, 1, tilePosition.x, tilePosition.y, 1) >= city.radius) {
                    return false;
                }
            }

			// Validate any layouts
			var builder: CityObject = city.objects.get(parentObj.objectId);
			if (!structPrototype.validateLayout(builder, city, mapPosition)) {
				return false;
			}

            var canBuild: Boolean = RoadPathFinder.CanBuild(mapPosition, structPrototype.size, city, requiresRoad);

            if (!canBuild) {
                return false;
            }

            this.hasRoadNearby = true;

            // If this object can be built then we add all of its tiles as buildable
            for each (tilePosition in TileLocator.foreachMultitile(mapPosition.x, mapPosition.y, structPrototype.size)) {
                if (!buildableTiles.get(tilePosition)) {
                    buildableTiles.add(tilePosition);
                }
            }

			return true;
		}

        private function validateTileCallback(x: int, y: int): * {

            // Get the screen position of the main building then we'll add the current tile x and y to get the point of this tile on the screen
			var point: ScreenPosition = city.primaryPosition.toScreenPosition();
            var mapPosition:Position = new ScreenPosition(point.x + x, point.y + y).toPosition();

            if (buildableTiles.get(mapPosition) == null) {
                return new ColorTransform(1, 1, 1, 0.5, 255, 215);
            }

			return new ColorTransform(1.0, 1.0, 1.0, 1.0, 0, 100);
		}

		public function validateBuilding():Boolean
		{
            var city: City = Global.map.cities.get(parentObj.groupId);
			var mapPosition: Position = objPosition.toPosition();

			// Check if cursor is inside city walls
			if (city != null && TileLocator.distance(city.primaryPosition.x, city.primaryPosition.y, 1, mapPosition.x, mapPosition.y, 1) >= city.radius) {
				hideCursors();
				return false;
			}

            var allTilesAreValid: Boolean = Enumerable.from(TileLocator.foreachMultitile(mapPosition.x, mapPosition.y, structPrototype.size))
                    .all(buildableTiles.get);

            if (!allTilesAreValid) {
				hideCursors();
				return false;
			}
			
			showCursors();
			return true;
		}
	}
}

