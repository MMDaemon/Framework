﻿using Framework;
using Geometry;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Drawing;

namespace MvcSpaceInvaders
{
	public class Visual
	{
		public Visual(Texture texEnemy, Texture texBullet, Texture texPlayer)
		{
			this.texEnemy = texEnemy;
			this.texBullet = texBullet;
			this.texPlayer = texPlayer;
		}

		public void Resize(int width, int height)
		{
			GL.Viewport(0, 0, width, height);
		}

        public void DrawScreen(IEnumerable<Box2D> enemies, IEnumerable<Box2D> bullets, Box2D player)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.LoadIdentity();
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			texEnemy.BeginUse();
			foreach (Box2D enemy in enemies)
			{
				Draw(enemy);
			}
			texEnemy.EndUse();
			texBullet.BeginUse();
			foreach (Box2D bullet in bullets)
			{
				Draw(bullet);
			}
			texBullet.EndUse();
			texPlayer.BeginUse();
			Draw(player);
			texPlayer.EndUse();
			GL.Disable(EnableCap.Blend);
		}

		private readonly Texture texEnemy;
		private readonly Texture texPlayer;
		private readonly Texture texBullet;

		private void Draw(Box2D Rectanlge)
		{
			GL.Color3(Color.White);
			GL.Begin(PrimitiveType.Quads);
				GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(Rectanlge.X, Rectanlge.Y);
				GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(Rectanlge.MaxX, Rectanlge.Y);
				GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(Rectanlge.MaxX, Rectanlge.MaxY);
				GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(Rectanlge.X, Rectanlge.MaxY);
			GL.End();
		}
	}
}
