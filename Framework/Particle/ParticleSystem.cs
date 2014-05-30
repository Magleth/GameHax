﻿using System.Collections.Generic;

using MG.Framework.Assets;
using MG.Framework.Graphics;
using MG.Framework.Numerics;
using MG.Framework.Utility;

namespace MG.Framework.Particle
{
	public abstract class ParticleEmitter
	{
		protected ParticleData particleData;
		protected ParticleDefinition particleDefinition;

		public ParticleEmitter(ParticleData particleData, ParticleDefinition particleDefinition)
		{
			this.particleData = particleData;
			this.particleDefinition = particleDefinition;
		}

		public abstract void Update(Time time);
		public abstract int Emit();
		public abstract void Clear();
		public abstract void Reload();
	}

	public abstract class BasicParticleEmitter : ParticleEmitter
	{
		private ParticleDefinition.Parameter paramLife;
		private ParticleDefinition.Parameter paramSpawnRate;
		private List<Vector2> particlePosition;
		private List<Vector2> particleVelocity;
		private List<float> particleLife;
		private List<float> particleAge;

		private float particleSpawnAccumulator;
		
		public BasicParticleEmitter(ParticleData particleData, ParticleDefinition particleDefinition)
			: base(particleData, particleDefinition)
		{
			particlePosition = particleData.Get<Vector2>("Position");
			particleVelocity = particleData.Get<Vector2>("Velocity");
			particleLife = particleData.Get<float>("Life");
			particleAge = particleData.Get<float>("Age");
		}

		public override void Reload()
		{
			paramLife = particleDefinition.Parameters["Life"];
			paramSpawnRate = particleDefinition.Parameters["SpawnRate"];
		}

		public override void Clear()
		{
			particleSpawnAccumulator = 0;
		}
		
		public override void Update(Time time)
		{
			particleSpawnAccumulator += time.ElapsedSeconds;

			var secondsPerParticle = 1.0f / ParticleHelpers.GetFloat(paramSpawnRate);
			while (particleSpawnAccumulator >= secondsPerParticle)
			{
				Emit();
				particleSpawnAccumulator -= secondsPerParticle;
			}
		}

		protected int EmitInternal(Vector2 position, Vector2 velocity, float life)
		{
			if (particleData.ActiveParticles + 1 >= particleData.MaxParticles) particleData.Resize();
			var index = particleData.ActiveParticles;
			particleData.ActiveParticles++;
			
			particlePosition[index] = position;
			particleVelocity[index] = velocity;
			particleAge[index] = 0;
			particleLife[index] = ParticleHelpers.GetFloat(paramLife);
			return index;
		}
	}

	public class PointEmitter : BasicParticleEmitter
	{
		public Vector2 Point;

		public PointEmitter(ParticleData particleData, ParticleDefinition particleDefinition)
			: base(particleData, particleDefinition)
		{

		}
		
		public override int Emit()
		{
			return EmitInternal(Point, MathTools.Random().RandomDirection() * 40, 0);
		}
	}

	//public class LineEmitter : BasicParticleEmitter
	//{
	//    public Line Line;

	//    public LineEmitter(ParticleData particleData)
	//        : base(particleData)
	//    {

	//    }

	//    public override void Emit()
	//    {

	//    }
	//}


	//public abstract class ParticleUpdater
	//{
	//    public abstract void Update(Time time);
	//}


	public class ParticleSystem
	{
		public Vector2 Position;
		public float Angle;
		
		public int ActiveParticles { get { return particleData.ActiveParticles; } }
		public readonly ParticleDefinition Definition;
		public List<ParticleSystem> SubSystems = new List<ParticleSystem>();

		private AssetHandler assetHandler;
		private ParticleManager particleManager;
		private Texture2D particleTexture;
		private List<float> particleAge;
		private List<int> particleSortIndex;
		
		class CompareLeastAgeFirst : IComparer<int>
		{
			public List<float> ParticleAge;

			int IComparer<int>.Compare(int x, int y)
			{
				return ParticleAge[y].CompareTo(ParticleAge[x]);
			}
		}

		class ComparisonMostAgeFirst : IComparer<int>
		{
			public List<float> ParticleAge;

			int IComparer<int>.Compare(int x, int y)
			{
				return ParticleAge[x].CompareTo(ParticleAge[y]);
			}
		}
		
		private CompareLeastAgeFirst compareLeastAgeFirst;
		private ComparisonMostAgeFirst comparisonMostAgeFirst;
		private ParticleData particleData = new ParticleData(64);
		private ParticleEmitter emitter;
		
		private ParticleDefinition.Parameter paramTexture;
		private ParticleDefinition.Parameter paramSortMode;
		private ParticleDefinition.Parameter paramBlendMode;

		public ParticleSystem(AssetHandler assetHandler, ParticleManager particleManager, ParticleDefinition particleDefinition)
		{
			this.assetHandler = assetHandler;
			this.particleManager = particleManager;
			this.Definition = particleDefinition;
			
			particleData.Register<Vector2>("Position");
			particleData.Register<Vector2>("Velocity");
			particleData.Register<float>("Life");
			particleSortIndex = particleData.Register<int>("SortIndex");
			particleAge = particleData.Register<float>("Age");
			
			compareLeastAgeFirst = new CompareLeastAgeFirst { ParticleAge = particleAge };
			comparisonMostAgeFirst = new ComparisonMostAgeFirst { ParticleAge = particleAge };

			emitter = new PointEmitter(particleData, particleDefinition);
		}
		
		public void Reload()
		{
			paramTexture = Definition.Parameters["Texture"];
			paramSortMode = Definition.Parameters["SortMode"];
			paramBlendMode = Definition.Parameters["BlendMode"];

			var texture = paramTexture.Value.Get<FilePath>();
			particleTexture = assetHandler.Load<Texture2D>(texture);

			emitter.Reload();

			if (Definition.Children.Count != SubSystems.Count)
			{
				ClearChildren();
				SubSystems.Capacity = Definition.Children.Count;
				foreach (var child in Definition.Children)
				{
					SubSystems.Add(particleManager.Create(child));
				}
			}
		}

		public void Clear()
		{
			Position = Vector2.Zero;
			particleData.ActiveParticles = 0;
			emitter.Clear();

			ClearChildren();
		}

		private void ClearChildren()
		{
			foreach (var child in SubSystems)
			{
				child.Clear();
				particleManager.Destroy(child);
			}
			SubSystems.Clear();
		}
		
		public void Update(Time time)
		{
			if (time.ElapsedSeconds <= 0)
				return;

			((PointEmitter)emitter).Point = Position;
			emitter.Update(time);

			var particlePosition = particleData.Get<Vector2>("Position");
			var particleVelocity = particleData.Get<Vector2>("Velocity");
			
			var particleLife = particleData.Get<float>("Life");

			for (int i = 0; i < particleData.ActiveParticles;)
			{
				particlePosition[i] += particleVelocity[i] * time.ElapsedSeconds;
				particleAge[i] += time.ElapsedSeconds;

				if (particleAge[i] >= particleLife[i])
				{
					Destroy(i);
				}
				else
				{
					i++;
				}
			}

			foreach (var system in SubSystems)
			{
				system.Update(time);
			}
		}

		public void Draw(RenderContext renderContext)
		{
			Draw(renderContext, Matrix.Identity);
		}

		public void Draw(RenderContext renderContext, Matrix transform)
		{
			var quadBatch = renderContext.QuadBatch;
			var blendMode = (BlendMode)paramBlendMode.Value.Get<int>();
			quadBatch.Begin(transform, blendMode);
			
			for (int i = 0; i < particleSortIndex.Count; i++)
			{
				particleSortIndex[i] = i;
			}

			var sortMode = blendMode == BlendMode.BlendmodeAdditive ? ParticleSortMode.Unsorted : (ParticleSortMode)paramSortMode.Value.Get<int>();
			if (sortMode == ParticleSortMode.NewestOnTop)
			{
				particleSortIndex.Sort(0, ActiveParticles, compareLeastAgeFirst);
			}
			else if (sortMode == ParticleSortMode.OldestOnTop)
			{
				particleSortIndex.Sort(0, ActiveParticles, comparisonMostAgeFirst);
			}
			
			var particlePosition = particleData.Get<Vector2>("Position");
			var particleLife = particleData.Get<float>("Life");

			for (int i = 0; i < particleData.ActiveParticles; i++)
			{
				var index = particleSortIndex[i];
				
				var p = particlePosition[index];
				var a = particleAge[index];
				var l = particleLife[index];
				var color = Color.Lerp(new Color(1.0f, 1.0f, 1.0f, 1.0f), new Color(0, 0, 0, 0), a / l);

				quadBatch.Draw(particleTexture, MathTools.Create2DAffineMatrix(p.X, p.Y, 1, 1, 0), color, particleTexture.Size / 2, 0);
			}
			
			quadBatch.End();

			var childTransform = transform * MathTools.Create2DAffineMatrix(Position.X, Position.Y, 1, 1, 0);
			foreach (var system in SubSystems)
			{
				system.Draw(renderContext, childTransform);
			}
		}
		
		private void Destroy(int index)
		{
			particleData.Move(particleData.ActiveParticles - 1, index);
			particleData.ActiveParticles--;
		}
	}
}
