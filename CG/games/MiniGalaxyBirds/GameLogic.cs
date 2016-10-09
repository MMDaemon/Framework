﻿using Framework;
using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniGalaxyBirds
{
	class GameLogic
	{
		public static readonly Box2D visibleFrame = new Box2D(0.0f, 0.0f, 1.0f, 1.0f);

		public GameLogic(IRenderer renderer)
		{
			Lost = false;
			Points = 0;
			if (null == renderer)
			{
				throw new Exception("Renderer required!");
			}
			this.renderer = renderer;

			CreateEnemySource(0.0f);
			CreatePlayer();
		}

		public void Update(float absoluteTime, float axisUpDown, float axisLeftRight, bool shoot)
		{
			UpdatePlayer(axisUpDown, axisLeftRight, shoot);
			//do foreach on a copy to avoid errors if components are added or deleted in update
			foreach (ITimedUpdate updatable in registry.GetComponents<ITimedUpdate>().ToList())
			{
				updatable.Update(absoluteTime);
			}
			var collisions = ResolveCollisions(registry.GetComponents<ICollidable>());
			foreach (var collision in collisions)
			{
				var containerA = registry.GetContainer(collision.Item1 as IComponent);
				var containerB = registry.GetContainer(collision.Item2 as IComponent);

			}
			//player stuff
			foreach (var containerPlayer in registry.GetAllContainerWithComponent<ComponentPlayer>().ToList())
			{
				HandleCollisions(absoluteTime, containerPlayer);
			}
			Console.WriteLine(registry.GetAllContainer().Count());
		}

		private IEnumerable<Tuple<ICollidable, ICollidable>> ResolveCollisions(IEnumerable<ICollidable> collidables)
		{
			var collisions = new List<Tuple<ICollidable, ICollidable>>();
			var work = new Stack<ICollidable>(collidables);
			while (0 < work.Count)
			{
				var collidableA = work.Pop();
				foreach (var collidableB in work)
				{
					if (collidableA.Frame.Intersects(collidableB.Frame))
					{
						collisions.Add(new Tuple<ICollidable, ICollidable>(collidableA, collidableB));
					}
				}
			}
			return collisions;
		}

		public bool Lost { get; private set; }
		public uint Points { get; private set; }

		private ComponentRegistry registry = new ComponentRegistry();
		private IRenderer renderer;

		private void UpdatePlayer(float axisUpDown, float axisLeftRight, bool shoot)
		{
			//player stuff
			foreach (var containerPlayer in registry.GetAllContainerWithComponent<ComponentPlayer>())
			{
				var compPlayer = containerPlayer.GetComponent<ComponentPlayer>();
				compPlayer.SetPlayerState(axisUpDown, axisLeftRight, shoot);
			}
		}
	

		private void HandleCollisions(float absoluteTime, IComponentContainer containerPlayer)
		{
			var player = containerPlayer.GetComponent<Component<Box2D>>().Value;
			foreach (IComponentContainer enemy in registry.GetAllContainerWithComponent<Enemy>().ToList())
			{
				Box2D enemyFrame = enemy.GetComponent<Component<Box2D>>().Value;
				//intersections enemy <-> player
				if (player.Intersects(enemyFrame))
				{
					//game lost
					Lost = true;
					//remove player
					Remove(containerPlayer);
					CreateExplosion(absoluteTime, player);
					//remove enemy
					Remove(enemy);
					CreateExplosion(absoluteTime, enemyFrame);
					return;
				}
				//intersections enemy <-> playerBullet
				foreach (IComponentContainer bullet in registry.GetAllContainerWithComponent<PlayerBullet>().ToList())
				{
					Box2D bulletFrame = bullet.GetComponent<Component<Box2D>>().Value;
					if (bulletFrame.Intersects(enemyFrame))
					{
						Points += 20;
						//delete bullet and enemy
						Remove(bullet);
						Remove(enemy);
						CreateExplosion(absoluteTime, enemyFrame);
					}
				}
			}
			//intersections enemyBullet <-> player
			foreach (IComponentContainer enemyBullet in registry.GetAllContainerWithComponent<EnemyBullet>().ToList())
			{
				Box2D enemyFrame = enemyBullet.GetComponent<Component<Box2D>>().Value;
				if (player.Intersects(enemyFrame))
				{
					//game lost
					Lost = true;
					//remove player
					Remove(containerPlayer);
					CreateExplosion(absoluteTime, player);
					//remove enemyBullet
					Remove(enemyBullet);
					return;
				}
				//bullet <-> bullet
				foreach (IComponentContainer bullet in registry.GetAllContainerWithComponent<PlayerBullet>().ToList())
				{
					Box2D bulletFrame = bullet.GetComponent<Component<Box2D>>().Value;
					if (bulletFrame.Intersects(enemyFrame))
					{
						Points += 2;
						Remove(bullet);
						Remove(enemyBullet);
						CreateExplosion(absoluteTime, bulletFrame);
					}
				}
			}
		}

		private void Remove(IComponentContainer container)
		{
			var drawable = container.GetComponent<Component<IDrawable>>();
			if (null != drawable)
			{
				renderer.DeleteDrawable(drawable.Value);
			}
			registry.Unregister(container);
		}

		private void CreateEnemy(float absoluteTime)
		{		
			var container = registry.CreateComponentContainer();
			var frame = new Box2D(0.5f, 1.0f, 0.06f, 0.1f);
			var compFrame = new Component<Box2D>(frame);
			registry.RegisterComponentTo(container, compFrame);
			var enemy = new Enemy(compFrame, absoluteTime, - 0.3f);
			registry.RegisterComponentTo(container, enemy);
			var timerEnemyBullet = new ComponentTimer(absoluteTime, 1.5f);
			timerEnemyBullet.OnTimerElapsed += (time) =>
			{
				CreateEnemyBullet(time, frame.X, frame.Y);
				CreateEnemyBullet(time, frame.MaxX, frame.Y);
			};
			registry.RegisterComponentTo(container, timerEnemyBullet);
			registry.RegisterComponentTo(container, new ComponentClipper(visibleFrame, frame, () => Remove(container)));
			registry.RegisterComponentTo(container, new Collidable(frame));
			registry.RegisterComponentTo(container, new Component<IDrawable>(renderer.CreateDrawable("enemy", frame)));
		}

		private void CreateEnemyBullet(float time, float x, float y)
		{
			var container = registry.CreateComponentContainer();
			var frame = new Box2D(x, y, 0.02f, 0.04f);
			registry.RegisterComponentTo(container, new Component<Box2D>(frame));
			registry.RegisterComponentTo(container, new ConstantMovement(frame, time, 0.0f, -0.7f));
			registry.RegisterComponentTo(container, new EnemyBullet());
			registry.RegisterComponentTo(container, new Collidable(frame));
			registry.RegisterComponentTo(container, new ComponentClipper(visibleFrame, frame, () => Remove(container)));
			registry.RegisterComponentTo(container, new Component<IDrawable>(renderer.CreateDrawable("bulletEnemy", frame)));
		}

		private void CreateEnemySource(float absoluteTime)
		{
			var containerEnemySource = registry.CreateComponentContainer();
			var timer = new ComponentTimer(absoluteTime, 0.5f);
			timer.OnTimerElapsed += (time) => { CreateEnemy(time); };
			registry.RegisterComponentTo(containerEnemySource, timer);
		}

		private void CreatePlayerBullet(float time, float x, float y)
		{
			float bulletWith = 0.02f;
			var container = registry.CreateComponentContainer();
			var frame = new Box2D(x - 0.5f * bulletWith, y, bulletWith, 0.04f);
			registry.RegisterComponentTo(container, new Component<Box2D>(frame));
			registry.RegisterComponentTo(container, new ConstantMovement(frame, time, 0.0f, 1.0f));
			registry.RegisterComponentTo(container, new PlayerBullet());
			registry.RegisterComponentTo(container, new ComponentClipper(visibleFrame, frame, () => Remove(container) ));
			registry.RegisterComponentTo(container, new Collidable(frame));
			registry.RegisterComponentTo(container, new Component<IDrawable>(renderer.CreateDrawable("bulletPlayer", frame)));
		}

		private void CreateExplosion(float absoluteTime, Box2D frame)
		{
			var container = registry.CreateComponentContainer();
			var compFrame = new Component<Box2D>(new Box2D(frame));
			var compAnim = new ComponentAnimated(absoluteTime, 1.0f);
			compAnim.OnTimerElapsed += (t) => Remove(container);
			registry.RegisterComponentTo(container, compFrame);
			registry.RegisterComponentTo(container, compAnim);
			registry.RegisterComponentTo(container, new Component<IDrawable>(renderer.CreateDrawable("explosion", compFrame.Value, compAnim)));
		}

		private void CreatePlayer()
		{
			var container = registry.CreateComponentContainer();
			var frame = new Box2D(0.5f, 0.0f, 0.0789f, 0.1f);
			var compFrame = new Component<Box2D>(frame);
			var player = new ComponentPlayer(compFrame.Value, visibleFrame);
			player.OnCreateBullet += CreatePlayerBullet;
			registry.RegisterComponentTo(container, compFrame);
			registry.RegisterComponentTo(container, player);
			registry.RegisterComponentTo(container, new Collidable(frame));
			registry.RegisterComponentTo(container, new Component<IDrawable>(renderer.CreateDrawable("player", compFrame.Value)));
		}
	}
}
