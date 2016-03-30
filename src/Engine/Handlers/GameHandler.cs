﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectBueno.Game.Entities;
using ProjectBueno.Game.Tiles;
using System.Collections.Generic;
using System;
using ProjectBueno.Engine.World;
using System.Diagnostics;
using ProjectBueno.Game.Spells;
using ProjectBueno.Utility;

namespace ProjectBueno.Engine
{
	public class GameHandler : IHandler
	{
		public GameHandler()
		{
			Main.exiting += onExitSave;

			player = new Player(new Vector2(Terrain.xSize*0.5f*Tile.TILESIZE,Terrain.ySize*0.5f*Tile.TILESIZE), this);
			screenScale = 2.0f;
			projectiles = new List<Projectile>();
			entities = new List<Entity>();

			terrain = new Terrain();

			terrain.generateChunkMap();
			terrain.startPoint = terrain.getRandomForestChunk();
			terrain.endPoint = terrain.getRandomForestChunk();
			terrain.processBiome();

			//selectedEnemy = new AnimatedTexture(Main.content.Load<Texture2D>("selectedTargetTest"),2,1.0f/30,14,16);
			//selectedEnemySize = new Vector2(selectedEnemy.w, selectedEnemy.h);
			//loadedChunks = new List<List<List<Tiles>>>();

			outlineShader = Main.content.Load<Effect>("OutlineShader");

			maskStencil = new DepthStencilState
			{
				StencilEnable = true,
				StencilFunction = CompareFunction.Always,
				StencilPass = StencilOperation.Replace,
				ReferenceStencil = 1,
				DepthBufferEnable = false,
			};
			drawStencil = new DepthStencilState
			{
				StencilEnable = true,
				StencilFunction = CompareFunction.GreaterEqual,
				StencilPass = StencilOperation.Keep,
				ReferenceStencil = 0,
				DepthBufferEnable = false,
			};

			blendTransparent = new BlendState
			{
				ColorWriteChannels = ColorWriteChannels.None
			};

			alphaTest = new AlphaTestEffect(Main.graphicsManager.GraphicsDevice)
			{
				Projection = Matrix.CreateOrthographicOffCenter(0, Main.graphicsManager.GraphicsDevice.PresentationParameters.BackBufferWidth, Main.graphicsManager.GraphicsDevice.PresentationParameters.BackBufferHeight, 0, 0, 1)
			};
		}

		private float _screenScale;
		public float screenScale
		{
			get { return _screenScale; }
			set {
				screenScaleInv = 1 / value;
				_screenScale = value;
			}
		}
		public float screenScaleInv;
		public Vector2 screenShift;
		public Matrix screenMatrix;

		protected static Effect outlineShader;
		protected static DepthStencilState maskStencil, drawStencil;
		protected static AlphaTestEffect alphaTest;
		protected static BlendState blendTransparent;

		protected AnimatedTexture selectedEnemy;
		protected Vector2 selectedEnemySize;

		protected static readonly Random random = new Random();

		public List<Entity> entities;
		public List<Projectile> projectiles;
		//protected Point ppChunk, mmChunk, mpChunk, pmChunk;
		public Player player { get; protected set; }
		public Terrain terrain { get; protected set; }

		public Vector2 posFromScreenPos(Vector2 screenPos)
		{
			return (screenPos - screenShift) * screenScaleInv + player.pos;
		}

		public Entity getEntityAtPos(Vector2 pos)
		{
			foreach (var entity in entities)
			{
				if (entity.checkCollision(pos, Vector2.Zero))
				{
					return entity;
				}
			}
			return null;
		}

		private void onExitSave(object sender, EventArgs args)
		{
			Console.WriteLine("NOW PRETENDING TO SAVE GAME");
		}

		public void closeGameHandler()
		{
			Main.exiting -= onExitSave;
		}

		public void windowResize()
		{
			screenShift = new Vector2((float)Math.Floor((Main.window.ClientBounds.Width - (player.size.X * screenScale)) * 0.5), (float)Math.Floor((Main.window.ClientBounds.Height - (player.size.Y * screenScale)) * 0.5));
			screenMatrix = Matrix.CreateScale(screenScale)*Matrix.CreateTranslation(new Vector3(screenShift, 0.0f));

			alphaTest = new AlphaTestEffect(Main.graphicsManager.GraphicsDevice)
			{
				Projection = Matrix.CreateOrthographicOffCenter(0, Main.graphicsManager.GraphicsDevice.PresentationParameters.BackBufferWidth, Main.graphicsManager.GraphicsDevice.PresentationParameters.BackBufferHeight, 0, 0, 1)
			};
		}

		public void Draw()
		{
			Matrix matrixCache = Matrix.CreateTranslation(new Vector3(-player.pos, 0.0f)) * screenMatrix;

			//Main.graphicsManager.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Transparent, 0, 0);
			Main.graphicsManager.GraphicsDevice.Clear(Color.Green);

			Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, matrixCache);

			//int xRight = Math.Min((int)((player.pos.X + Main.window.ClientBounds.Width * screenScaleInv * 0.5f) * Tile.TILEMULT) + 2, Terrain.xSize);
			//int yBottom = Math.Min((int)((player.pos.Y + Main.window.ClientBounds.Height * screenScaleInv * 0.5f) * Tile.TILEMULT) + 2, Terrain.ySize);

			Point playerChunk = Terrain.getChunkFromPos(player.pos);
			int hShift = (player.pos.X - playerChunk.ToVector2().X * Terrain.CHUNK_SIZE * Tile.TILESIZE) > Terrain.CHUNK_SHIFT ? 1 : -1;
			int vShift = (player.pos.Y - playerChunk.ToVector2().Y * Terrain.CHUNK_SIZE * Tile.TILESIZE) > Terrain.CHUNK_SHIFT ? 1 : -1;
			terrain.drawChunk(playerChunk);
			terrain.drawChunk(new Point(playerChunk.X + hShift, playerChunk.Y));
			terrain.drawChunk(new Point(playerChunk.X, playerChunk.Y + vShift));
			terrain.drawChunk(new Point(playerChunk.X + hShift, playerChunk.Y + vShift));

			player.Draw();
			for (int i = 0; i < projectiles.Count; i++)
			{
				projectiles[i].Draw();
			}

			foreach (var ent in entities)
			{
				ent.Draw();
			}

			Main.spriteBatch.End();

			if (player.target != null)
			{
				alphaTest.World = matrixCache;

				Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendTransparent, SamplerState.PointWrap, maskStencil, null, alphaTest, matrixCache);
				player.target.DrawRaw();
				Main.spriteBatch.End();

				Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, drawStencil, null, outlineShader, matrixCache);
				player.target.DrawOutline(Color.Red);
				Main.spriteBatch.End();

				//selectedEnemy.incrementAnimation();
				//Main.spriteBatch.Draw(selectedEnemy.texture, player.target.pos+0.5f*(player.target.size-selectedEnemySize),selectedEnemy.getCurFrame(), Color.White);
			}

			if (Main.newKeyState.IsKeyDown(Keys.M))
			{
				Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap);
				terrain.drawChunkMap(player.pos);
				Main.spriteBatch.End();
			}
		}

		public void Update()
		{
			if (Main.newKeyState.IsKeyDown(Keys.Up) && !Main.oldKeyState.IsKeyDown(Keys.Up))
			{
				screenScale *= 2.0f;
				windowResize();
			}
			if (Main.newKeyState.IsKeyDown(Keys.Down) && !Main.oldKeyState.IsKeyDown(Keys.Down))
			{
				screenScale *= 0.5f;
				windowResize();
			}

			if (Main.newKeyState.IsKeyDown(Keys.Enter) && !Main.oldKeyState.IsKeyDown(Keys.Enter))
			{
				terrain.clearChunks();
				terrain.generateChunkMap();
				terrain.startPoint = terrain.getRandomForestChunk();
				terrain.endPoint = terrain.getRandomForestChunk();
				terrain.processBiome();
			}

			if (Main.newKeyState.IsKeyDown(Keys.K) && !Main.oldKeyState.IsKeyDown(Keys.K))
			{
				entities.Clear();
			}

			while (entities.Count < 10)
			{
				entities.Add(new Enemy(player.pos + AngleVector.Vector(random.NextDouble()*360.0)*500.0f, this));
			}

			for (int i = 0; i < projectiles.Count; i++)
			{
				projectiles[i].Update();
			}
			foreach (var ent in entities)
			{
				ent.Update();
			}
			player.Update();
			projectiles.RemoveAll(item => item.toRemove);
			entities.RemoveAll(item => item.isDead);

			if (player.target != null && player.target.isDead)
			{
				player.target = null;
			}
			if (Main.newMouseState.LeftButton == ButtonState.Pressed && Main.oldMouseState.LeftButton == ButtonState.Released)
			{
				player.target = getEntityAtPos(posFromScreenPos(Main.newMouseState.Position.ToVector2())) ?? player.target;
				//Console.WriteLine("Mouse:" + getEntityAtPos(posFromScreenPos(Main.newMouseState.Position.ToVector2())));
			}

			if (Main.newKeyState.IsKeyDown(Keys.Back) && !Main.oldKeyState.IsKeyDown(Keys.Back))
			{
				Main.graphicsDirty = true; //DON'T KNOW WHY THIS IS NEEDED
				Main.handler = new SkillHandler(this,player);
			}
			if (Main.newKeyState.IsKeyDown(Keys.P) && !Main.oldKeyState.IsKeyDown(Keys.P))
			{
				Main.handler = new PauseHandler(this);
			}

			/*Stopwatch s1 = new Stopwatch();
			s1.Start();
			if (ppChunk != Terrain.getChunkFromPos(new Vector2(player.pos.X + Terrain.CHUNK_SHIFT, player.pos.Y + Terrain.CHUNK_SHIFT)) ||
				mmChunk != Terrain.getChunkFromPos(new Vector2(player.pos.X - Terrain.CHUNK_SHIFT, player.pos.Y - Terrain.CHUNK_SHIFT)) ||
				mpChunk != Terrain.getChunkFromPos(new Vector2(player.pos.X - Terrain.CHUNK_SHIFT, player.pos.Y + Terrain.CHUNK_SHIFT)) ||
				pmChunk != Terrain.getChunkFromPos(new Vector2(player.pos.X + Terrain.CHUNK_SHIFT, player.pos.Y - Terrain.CHUNK_SHIFT)) )
			{
				ppChunk = Terrain.getChunkFromPos(new Vector2(player.pos.X + Terrain.CHUNK_SHIFT, player.pos.Y + Terrain.CHUNK_SHIFT));
				mmChunk = Terrain.getChunkFromPos(new Vector2(player.pos.X - Terrain.CHUNK_SHIFT, player.pos.Y - Terrain.CHUNK_SHIFT));
				mpChunk = Terrain.getChunkFromPos(new Vector2(player.pos.X - Terrain.CHUNK_SHIFT, player.pos.Y + Terrain.CHUNK_SHIFT));
				pmChunk = Terrain.getChunkFromPos(new Vector2(player.pos.X + Terrain.CHUNK_SHIFT, player.pos.Y - Terrain.CHUNK_SHIFT));
				loadedChunks.Clear();
				if (mmChunk == ppChunk)
				{
					loadedChunks.Add(terrain.getChunk(mmChunk));
				}
				else if (mpChunk == mmChunk)
				{
					loadedChunks.Add(terrain.getChunk(mmChunk));
					loadedChunks.Add(terrain.getChunk(pmChunk));
				}
				else if (pmChunk == mmChunk)
				{
					loadedChunks.Add(terrain.getChunk(mmChunk));
					loadedChunks.Add(terrain.getChunk(mpChunk));
				}
				else
				{
					loadedChunks.Add(terrain.getChunk(ppChunk));
					loadedChunks.Add(terrain.getChunk(mmChunk));
					loadedChunks.Add(terrain.getChunk(mpChunk));
					loadedChunks.Add(terrain.getChunk(pmChunk));
				}
				s1.Stop();
				Console.WriteLine("Check: " + s1.ElapsedTicks);
			}
			s1.Stop();*/
		}
	}
}