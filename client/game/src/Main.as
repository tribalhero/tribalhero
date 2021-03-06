﻿package src
{
    import System.Linq.EnumerationExtender;

    import com.greensock.OverwriteManager;

    import com.greensock.plugins.TransformMatrixPlugin;
    import com.greensock.plugins.TweenPlugin;

    import fl.lang.*;

    import flash.display.*;
    import flash.events.*;
    import flash.net.*;
    import flash.system.Security;
    import flash.ui.*;

    import org.aswing.*;

    import src.Comm.*;
    import src.Map.*;
    import src.Map.MiniMap.MiniMap;
    import src.Objects.Factories.*;
    import src.UI.Dialog.*;
    import src.UI.LookAndFeel.*;
    import src.UI.TweenPlugins.DynamicPropsPlugin;
    import src.UI.TweenPlugins.TransformAroundCenterPlugin;
    import src.UI.TweenPlugins.TransformAroundPointPlugin;
    import src.Util.*;

    CONFIG::debug {
        import com.sociodox.theminer.TheMiner;
    }

    public class Main extends MovieClip
	{
		private var gameContainer: GameContainer;

		private var map:Map;
		private var miniMap: MiniMap;
		public var packetCounter:GeneralCounter;
		private var session:TcpSession;
		private var password: String;
		private var parms: Object;
        
		private var loginDialog: LoginDialog;
        
		private var pnlLoading: InfoDialog;
		public var errorAlreadyTriggered: Boolean;
        
		private var siteVersion: String;	
		
		public function Main()
		{
            Security.loadPolicyFile(Constants.mainWebsite + "crossdomain.xml?m=" + Constants.version + "." + Constants.revision);

			name = "Main";
			trace("TribalHero v" + Constants.version + "." + Constants.revision);
			
			addEventListener(Event.ADDED_TO_STAGE, init);		                       
		}        
		
		public function init(e: Event = null) : void {
			removeEventListener(Event.ADDED_TO_STAGE, init);							
            
            stage.showDefaultContextMenu = false;

            Global.musicPlayer = new MusicPlayer();

			CONFIG::debug {
			    stage.addChild(new TheMiner());
            }
			
			//Init actionLinq
			EnumerationExtender.Initialize();
			
			//Init ASWING			
			AsWingManager.initAsStandard(this);				
			UIManager.setLookAndFeel(new GameLookAndFeel());
			
			//Init TweenLite
			TweenPlugin.activate([DynamicPropsPlugin, TransformMatrixPlugin, TransformAroundCenterPlugin, TransformAroundPointPlugin]);
            OverwriteManager.init(OverwriteManager.AUTO);

			//Init stage options
			stage.stageFocusRect = false;
			stage.scaleMode = StageScaleMode.NO_SCALE;
			stage.align = StageAlign.TOP_LEFT;
			
			Global.main = this;

			//Init right context menu for debugging
			CONFIG::debug {
				var fm_menu:ContextMenu = new ContextMenu();
				var dump:ContextMenuItem = new ContextMenuItem("Dump stage");
				dump.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, function(e:Event):void { Util.dumpDisplayObject(stage); } );
				var dumpRegionQueryInfo:ContextMenuItem = new ContextMenuItem("Dump region query info");
				dumpRegionQueryInfo.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, function(e:Event):void {
					if (!Global.map) return;
					Util.log("Pending regions:" + Util.implode(',', Global.map.pendingRegions));
				} );
				fm_menu.customItems.push(dump);
				fm_menu.customItems.push(dumpRegionQueryInfo);
				contextMenu = fm_menu;
			}

			//Flash params
			parms = loaderInfo.parameters;

			//GameContainer
			Global.gameContainer = gameContainer = new GameContainer();
			addChild(gameContainer);

			//Packet Counter
			if (Constants.debug > 0) {
				packetCounter = new GeneralCounter("pkts");
				packetCounter.y = Constants.screenH - 64;
				addChild(packetCounter);
			}

            Constants.mainWebsite = parms.mainWebsite || Constants.mainWebsite;

            if (parms.playerName) {
                Constants.session.playerName = parms.playerName;
            }

            if (parms.hostname) {
                Constants.session.hostname = parms.hostname;
            }

			//Define login type and perform login action
			if (parms.lsessid)
			{
				siteVersion = parms.siteVersion;
				Constants.session.loginKey = parms.lsessid;
				loadData();
			}
			else
			{
				siteVersion = new Date().getTime().toString();
				showLoginDialog();
			}			                                         
		}

		private function loadData(): void
		{
			pnlLoading = InfoDialog.showMessageDialog("TribalHero", "Launching the game...", null, null, true, false, 0);
			
			if (Constants.queryData) {
				var loader: URLLoader = new URLLoader();
				loader.addEventListener(Event.COMPLETE, function(e: Event) : void { 
					Constants.objData = XML(e.target.data);
					loadLanguages();
				});
				loader.addEventListener(IOErrorEvent.IO_ERROR, function(e: Event): void {
					onDisconnected();
					showConnectionError(true);
				});
				loader.load(new URLRequest("http://" + Constants.session.hostname + "/data.xml?m=" + new Date().getTime().toString() + "&v=" + siteVersion));
			} 
			else
				loadLanguages();
		}
		
		private function loadLanguages():void
		{					
			Locale.setLoadCallback(function(success: Boolean) : void {
				if (!success) {
					onDisconnected();
					showConnectionError(true);
				}
				else doConnect();
			});
			Locale.addXMLPath(Constants.defLang, "http://" + Constants.session.hostname + "/Game_" + Constants.defLang + ".xml?m=" + new Date().getTime().toString() + "&v=" + siteVersion);
			Locale.setDefaultLang(Constants.defLang);				
			Locale.loadLanguageXML(Constants.defLang);
		}

		public function doConnect():void
		{
			errorAlreadyTriggered = false;
			
			session = new TcpSession();
			session.setConnect(onConnected);
			session.setLogin(onLogin);
			session.setDisconnect(onDisconnected);
			session.setSecurityErrorCallback(onSecurityError);
			session.connect(Constants.session.hostname);
		}

		public function showLoginDialog():void
		{
			gameContainer.closeAllFrames();
			loginDialog = new LoginDialog(onConnect);
			loginDialog.show();
		}

		public function onConnect(sender: LoginDialog):void
		{
			Constants.session.username = sender.getTxtUsername().getText();
			password = sender.getTxtPassword().getText();
			Constants.session.hostname = sender.getTxtAddress().getText();

			loadData();
		}

		public function onSecurityError(event: SecurityErrorEvent):void
		{
			Util.log("Security error " + event.toString());
			
			if (session && session.hasLoginSuccess()) 
				return;
	
			onDisconnected();
		}

		public function onDisconnected(event: Event = null):void
		{			
            Util.triggerJavascriptEvent("clientDisconnect");
            
			var wasStillLoading: Boolean = session == null || !session.hasLoginSuccess();
			
			if (pnlLoading)
				pnlLoading.getFrame().dispose();
		
			gameContainer.dispose();			
			if (Global.mapComm) Global.mapComm.dispose();

            if (Global.musicPlayer) Global.musicPlayer.stop();

			Global.mapComm = null;
			Global.map = null;
			session = null;

			if (!errorAlreadyTriggered) {
				showConnectionError(wasStillLoading);
			}
		}
		
		public function showConnectionError(wasStillLoading: Boolean) : void {
			var unableToConnectMsg: String = "Unable to connect to server.\nIf you continue to have problems, try the following:\n\n1. Clear your browser cache.\n2. Make sure your firewall and/or antivirus programs allow access to ports 843 and 443. You may be unable to connect if you are behind a shared connection such as an office or school.\n3. Update to the latest version of Flash player.\n4. Check our main page to see if a server maintenance is in progress.\n\nIf none of these solved your problem, contact us at feedback@tribalhero.com for individual help.";
			
			if (parms.hostname) InfoDialog.showMessageDialog("Connection Lost", (wasStillLoading ? unableToConnectMsg : "Connection to Server Lost") + ". Refresh the page to rejoin the battle.", null, null, true, false, 1, true);
			else InfoDialog.showMessageDialog("Connection Lost", (wasStillLoading ? unableToConnectMsg : "Connection to Server Lost."), function(result: int):void { showLoginDialog(); }, null, true, false, 1, true);			
		}

		public function onConnected(event: Event, connected: Boolean):void
		{
			if (pnlLoading) pnlLoading.getFrame().dispose();

			if (!connected) 
				showConnectionError(true);		
			else
			{
                Util.triggerJavascriptEvent("clientConnect");                            
                
				Global.mapComm = new MapComm(session);

				if (Constants.session.loginKey) session.login(true, Constants.session.playerName, Constants.session.loginKey);
				else session.login(false, Constants.session.username, password);
			}

			password = '';
		}

		public function onLogin(packet: Packet):void
		{
			if (MapComm.tryShowError(packet, function(result: int) : void { showLoginDialog(); } , true)) {				
				errorAlreadyTriggered = true;
				return;
			}

			session.setLoginSuccess(true);

			if (loginDialog != null) loginDialog.getFrame().dispose();

			var newPlayer: Boolean = Global.mapComm.General.onLogin(packet);

			if (!newPlayer) {
				completeLogin(packet, false);
			}
			else {
                var createCityDialog: InitialCityDialog = new InitialCityDialog(function(sender: InitialCityDialog): void {
					Global.mapComm.General.createInitialCity(sender.getCityName(),
															 sender.getLocationParameter(),
															 function(packet: Packet):void {						
						completeLogin(packet, true);
					});
				});

				createCityDialog.show();
			}

            Global.musicPlayer.setMuted(Constants.session.soundMuted, true);
            if (!Constants.session.soundMuted) {
                Global.musicPlayer.play(newPlayer);
            }
		}

        private function completeLogin(packet: Packet, newPlayer: Boolean):void
		{
			Global.map = map = new Map();
			miniMap = new MiniMap(Constants.miniMapScreenW, Constants.miniMapScreenH);
			
			map.usernames.players.add(new Username(Constants.session.playerId, Constants.session.playerName));
			map.setTimeDelta(Constants.session.timeDelta);
			
			EffectReqFactory.init(map, Constants.objData);
			PropertyFactory.init(map, Constants.objData);
			StructureFactory.init(map, Constants.objData);
			TechnologyFactory.init(map, Constants.objData);
			UnitFactory.init(map, Constants.objData);
			WorkerFactory.init(map, Constants.objData);
			ObjectFactory.init(map, Constants.objData);
			
			Constants.objData = <Data></Data>;

			gameContainer.show();
			Global.mapComm.General.readLoginInfo(packet);
			gameContainer.setMap(map, miniMap);
		}
    }
}
