﻿using Microsoft.Xna.Framework;
using ProjectBueno.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectBueno.Game.Spells
{
	class ProjectileStream : Projectile
	{
		public ProjectileStream(Spell spell, GameHandler game, int lifetime) : base(spell, game, null)
		{
			projectiles = new List<Proj>();
			this.lifetime = lifetime;
		}

		protected List<Proj> projectiles;

		protected static int collisiontime;
		protected static int effecttime;

		public override bool toRemove
		{
			get
			{
				return lifetime <= 0;
			}
		}

		public void addProjectile(Vector2 pos, Vector2 speed)
		{
		}

		public override void Draw()
		{
		}

		public override void Update()
		{
		}
	}
}
