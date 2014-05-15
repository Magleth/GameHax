﻿using System;
using EditorCommon;
using Gtk;

namespace MG.ParticleEditorWindow
{
	public class MainWindow : IDisposable
	{
		public class ClosingEventArgs : EventArgs
		{
			public bool Cancel;
		}

		public delegate void ClosingEventHandler(object sender, ClosingEventArgs e);

		public event System.Action FileNew = delegate { };
		public event System.Action FileOpen = delegate { };
		public event System.Action<ClosingEventArgs> Closing = delegate { };
		public event System.Action Closed = delegate { };
		
		public readonly RenderView RenderView;
		public readonly TreeView TreeView;
		public readonly PropertyView PropertyView;

		private class GtkWindow : Gtk.Window
		{
			public GtkWindow(string title)
				: base(title)
			{

			}
		}

		private GtkWindow window;
		private AccelGroup accelerators;
		private Statusbar statusbar;

		public MainWindow(string title)
		{
			window = new GtkWindow(title);
			window.DeleteEvent += WindowOnDeleteEvent;
			window.Destroyed += (sender, args) => Closed.Invoke();
			
			window.Name = title;
			window.WindowPosition = WindowPosition.CenterOnParent;
			window.DefaultWidth = 1280;
			window.DefaultHeight = 720;

			// Subcomponents
			RenderView = new RenderView();
			TreeView = new TreeView();
			PropertyView = new PropertyView();

			accelerators = new AccelGroup();
			window.AddAccelGroup(accelerators);

			// Setup menu
			var menuBar = CreateMenu();
			var menuBox = new VBox(false, 0);
			menuBox.Spacing = 0;
			window.Add(menuBox);

			// Setup status bar
			statusbar = new Statusbar();
			statusbar.Name = "statusbar";
			statusbar.Spacing = 0;
			statusbar.Push(0, "");
			menuBox.PackEnd(statusbar, false, false, 0);
			menuBox.PackStart(menuBar, false, false, 0);

			// Setup left/right pane
			var hpaneMain = new HPaned();
			menuBox.PackStart(hpaneMain, true, true, 0);
			hpaneMain.Position = 200;

			// Setup gl widget and graph
			var vpaneRight = new VPaned();
			hpaneMain.Pack2(vpaneRight, true, true);
			vpaneRight.Position = 500;
			
			var f1 = new Frame();
			f1.Add(RenderView.Widget);
			f1.ShadowType = ShadowType.In;

			var f2 = new Frame();
			f2.Add(CreateGraph());
			f2.ShadowType = ShadowType.None;
			
			vpaneRight.Pack1(f1, true, true);
			vpaneRight.Pack2(f2, true, true);

			// Setup tree and properties
			var vpaneLeft = new VPaned();
			hpaneMain.Pack1(vpaneLeft, false, true);
			vpaneLeft.Position = 200;
			vpaneLeft.Pack1(TreeView.Widget, true, true);
			vpaneLeft.Pack2(PropertyView.Widget, true, true);

			window.ShowAll();
		}

		public string StatusText
		{
			set
			{
				statusbar.Pop(0);
				statusbar.Push(0, value);
			}
		}
		
		private void WindowOnDeleteEvent(object o, DeleteEventArgs args)
		{
			var eventArgs = new ClosingEventArgs();
			Closing.Invoke(eventArgs);

			if (eventArgs.Cancel) args.RetVal = true;
		}

		private MenuBar CreateMenu()
		{
			var menuBar = new MenuBar();
			var fileMenu = new Menu();
			var fileMenuItem = new MenuItem("_File");
			fileMenuItem.Submenu = fileMenu;
			menuBar.Append(fileMenuItem);
			
			var fileNew = new ImageMenuItem(Stock.New, accelerators);
			fileNew.Activated += (sender, args) => FileNew.Invoke();
			fileMenu.Append(fileNew);

			var fileOpen = new ImageMenuItem(Stock.Open, accelerators);
			fileOpen.Activated += (sender, args) => FileOpen.Invoke();
			fileMenu.Append(fileOpen);

			fileMenu.Append(new SeparatorMenuItem());

			var fileExit = new ImageMenuItem(Stock.Quit, accelerators);
			fileExit.Activated += (sender, args) => window.ProcessEvent(Gdk.EventHelper.New(Gdk.EventType.Delete));
			fileMenu.Append(fileExit);

			return menuBar;
		}
		
		private HaxGraph CreateGraph()
		{
			var graph = new HaxGraph();
			graph.Name = "Graph";
			return graph;
		}

		public void Dispose()
		{
			if (window != null)
			{
				window.Dispose();
				window = null;
			}
		}
	}
}
