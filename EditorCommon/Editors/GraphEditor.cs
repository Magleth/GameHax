﻿using System;

using Gtk;

using MG.EditorCommon.HaxWidgets;

using MonoDevelop.Components.PropertyGrid;

using Curve = MG.Framework.Numerics.Curve;

namespace MG.EditorCommon.Editors
{
	public class GraphEditor : PropertyEditorCell
	{
		private HaxGraph graph;

		protected override void Initialize()
		{
			base.Initialize();

			var declarationParameter = ((ParticleParameterDescriptor)Property).DeclarationParameter;

			graph = new HaxGraph();
			graph.Curve = (Curve)Property.GetValue(Instance);
			graph.MinValueY = declarationParameter.CurveMin;
			graph.MaxValueY = declarationParameter.CurveMax;
		}

		public override void GetSize(int availableWidth, out int width, out int height)
		{
			base.GetSize(availableWidth, out width, out height);
			height = 80;
		}

		public override void Render(Gdk.Drawable window, Cairo.Context ctx, Gdk.Rectangle bounds, StateType state)
		{
			graph.Draw(window, ctx, bounds, state);
			//base.Render(window, ctx, bounds, state);
		}

		protected override IPropertyEditor CreateEditor(Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new GraphPropertyEditor();
		}
	}

	public class GraphPropertyEditor : Gtk.Bin, IPropertyEditor
	{
		private HaxGraph graph;
		private int pad = 0;
		
		public GraphPropertyEditor()
		{
			graph = new HaxGraph();
			graph.Changed += GraphOnChanged;

			KeyPressEvent += OnKeyPressEvent;

			Add(graph);
			ShowAll();
			
			SizeRequested += OnSizeRequested;
			SizeAllocated += OnSizeAllocated;
		}

		private void OnKeyPressEvent(object o, KeyPressEventArgs args)
		{
			graph.KeyPress(args.Event);
		}

		private void GraphOnChanged()
		{
			ValueChanged.Invoke(this, EventArgs.Empty);
		}

		void OnSizeRequested(object o, SizeRequestedArgs args)
		{
			if (graph != null)
			{
				int width = args.Requisition.Width;
				int height = args.Requisition.Height;

				graph.GetSizeRequest(out width, out height);
				if (width == -1 || height == -1)
					width = height = 80;
				SetSizeRequest(width + pad * 2, height + pad * 2);
			}
		}

		void OnSizeAllocated(object o, SizeAllocatedArgs args)
		{
			if (graph != null)
			{
				Gdk.Rectangle mine = args.Allocation;
				Gdk.Rectangle his = mine;

				his.X += pad;
				his.Y += pad;
				his.Width -= pad * 2;
				his.Height -= pad * 2;
				graph.SizeAllocate(his);
			}
		}
		
		public void Initialize(EditSession session)
		{
			if (session.Property.PropertyType != typeof(Curve))
				throw new ApplicationException("Graph editor does not support editing values of type " + session.Property.PropertyType);

			var declarationParameter = ((ParticleParameterDescriptor)session.Property).DeclarationParameter;
			graph.MaxValueY = declarationParameter.CurveMax;
			graph.MinValueY = declarationParameter.CurveMin;
		}
		
		object IPropertyEditor.Value
		{
			get { return graph.Curve; }
			set { graph.Curve = (Curve)value; }
		}

		public event EventHandler ValueChanged;
	}
}
