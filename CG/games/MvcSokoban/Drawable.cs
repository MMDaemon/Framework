﻿using Geometry;
using OpenTK;
using System.Drawing;

namespace MvcSokoban
{
	public class Drawable
	{
		public Box2D Rect { get; private set; }

		public Drawable(Point position, float speed, float size)
		{
			this.position = position;
			this.Rect = new Box2D(position.X, position.Y, size, size);
			this.speed = speed;
		}

		public Point Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				//convert from logic to screen
				destination = new Vector2((float)value.X, (float)value.Y);
				//Console.WriteLine(destination);
			}
		}

		public void Update(float updatePeriod)
		{
			if (null != destination)
			{
				Vector2 pos = new Vector2(Rect.X, Rect.Y);
				Vector2 dir = destination.Value - pos;
				float length = dir.Length;
				if (length < 0.1f)
				{
					Rect.X = destination.Value.X;
					Rect.Y = destination.Value.Y;
					destination = null;
					return;
				}
				dir /= length;
				dir *= speed * updatePeriod;
				pos += dir;
				Rect.X = pos.X;
				Rect.Y = pos.Y;
			}
		}

		private Vector2? destination = null;
		private Point position;
		private readonly float speed;
	}
}
