﻿using StarlightRiver.Content.Abilities;
using StarlightRiver.Content.Buffs;
using StarlightRiver.Content.Physics;
using StarlightRiver.Core.Systems.MetaballSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Animations;
using Terraria.ID;

namespace StarlightRiver.Content.NPCs.Moonstone
{
	internal class Dreambeast : ModNPC, IHintable
	{
		public VerletChain[] chains = new VerletChain[6];

		public Vector2 homePos;
		public int flashTime;
		public int frameCounter = 0;

		private bool AppearVisible => Main.LocalPlayer.HasBuff(ModContent.BuffType<Overcharge>());
		private Player Target => Main.player[NPC.target];

		public ref float Phase => ref NPC.ai[0];
		public ref float Timer => ref NPC.ai[1];
		public ref float AttackTimer => ref NPC.ai[2];
		public ref float RandomTime => ref NPC.ai[3];

		public int TelegraphTime => 40;

		public override string Texture => AssetDirectory.Debug;

		public override void SetDefaults()
		{
			NPC.width = 64;
			NPC.height = 64;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.lifeMax = 666666;
			NPC.damage = 66;
			NPC.dontTakeDamage = true;
			NPC.immortal = true;
		}

		public override ModNPC Clone(NPC newEntity)
		{
			ModNPC clone = base.Clone(newEntity);
			(clone as Dreambeast).chains = chains;
			return clone;
		}

		public override void OnSpawn(IEntitySource source)
		{
			for (int k = 0; k < chains.Length; k++)
			{
				VerletChain chain = chains[k];

				if (chain is null)
				{
					chains[k] = new VerletChain(24 + 2 * Main.rand.Next(4), true, NPC.Center, 5, false)
					{
						constraintRepetitions = 10,//defaults to 2, raising this lowers stretching at the cost of performance
						drag = 1.2f,//this number defaults to 1, is very sensitive
						forceGravity = -Vector2.UnitX,
						scale = 0.6f,
						parent = NPC
					};
				}
			}
		}

		public override void AI()
		{
			Timer++;
			AttackTimer++;

			if (!AppearVisible)
				flashTime = 0;

			if (homePos == default)
				homePos = NPC.Center;

			if (flashTime < 30 && Phase != 0)
				flashTime++;

			for (int k = 0; k < chains.Length; k++)
			{
				VerletChain chain = chains[k];
				chain.forceGravity = -NPC.rotation.ToRotationVector2();
				chain?.UpdateChain(NPC.Center - NPC.rotation.ToRotationVector2() * 30 + NPC.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * (k - chains.Length / 2 + 1) * 15);

				for (int i = 0; i < chain.ropeSegments.Count; i++)
				{
					chain.ropeSegments[i].posNow += NPC.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(Main.GameUpdateCount * 0.04f + 251 % (k + 1) + i / 4f) * i / 30;
				}
			}


			if (Phase == 0)
				PassiveBehavior();
			else if (Phase == 1)
				AttackRest();
			else if (Phase == 2)
				AttackCharge();
			else if (Phase == 3)
				AttackShoot();
		}

		/// <summary>
		/// picks a random valid target. Meaning a player within range of the beasts home base and that has the insanity debuff.
		/// </summary>
		private void PickTarget()
		{
			var possibleTargets = new List<Player>();

			foreach (Player player in Main.player)
			{
				if (player.active && player.HasBuff<Overcharge>() && Vector2.Distance(player.Center, homePos) < 1000)
					possibleTargets.Add(player);
			}

			if (possibleTargets.Count <= 0)
			{
				NPC.target = -1;
				return;
			}

			NPC.target = possibleTargets[Main.rand.Next(possibleTargets.Count)].whoAmI;
		}

		/// <summary>
		/// Teleports the beast, as well as all of his chains' points. 
		/// </summary>
		/// <param name="target">The position to teleport to</param>
		private void Teleport(Vector2 target)
		{
			Vector2 diff = target - NPC.Center;
			NPC.Center = target;

			if (Phase == 0)
			{
				NPC.direction = Main.rand.NextBool() ? -1 : 1;
			}
			else
			{
				NPC.direction = (Target.Center - NPC.Center).X > 0 ? 1 : -1;
				NPC.rotation = (Target.Center - NPC.Center).ToRotation();
			}

			//We need to do this so the chains dont snap back like a rubber band
			foreach (VerletChain chain in chains)
			{
				chain.startPoint += diff;

				foreach (RopeSegment segment in chain.ropeSegments)
				{
					segment.posOld += diff;
					segment.posNow += diff;
				}
			}
		}

		/// <summary>
		/// What the NPC will be doing while its not actively attacking anyone.
		/// </summary>
		private void PassiveBehavior()
		{
			//logic for phase transition
			if (Main.player.Any(n => n.active && n.HasBuff<Overcharge>() && Vector2.Distance(n.Center, homePos) < 1000))
			{
				Phase = 1;
				NPC.Opacity = 1;
				AttackTimer = 0;
				return;
			}

			NPC.Center += Vector2.One.RotatedBy(Timer * 0.005f) * 0.25f;
			NPC.rotation = NPC.direction == 1 ? 0 : MathHelper.Pi;

			if (NPC.Opacity < 1)
				NPC.Opacity += 0.05f;

			if (AttackTimer > RandomTime)
			{
				NPC.Opacity = (20 - (AttackTimer - RandomTime)) / 20f;

				if (AttackTimer > RandomTime + 20)
				{
					AttackTimer = 0;
					RandomTime = Main.rand.Next(120, 240);

					Teleport(homePos + Vector2.One.RotatedByRandom(6.28f) * Main.rand.NextFloat(200, 400));
				}
			}

			if (AttackTimer % 4 == 0)
			{
				frameCounter = ++frameCounter % 7;
			}
		}

		/// <summary>
		/// What the NPC does while it is attacking but isnt currently executing a specific attack. It chooses its attack from here.
		/// </summary>
		private void AttackRest()
		{
			NPC.velocity *= 0;

			if (AttackTimer > 30)
			{
				NPC.Opacity = (20 - (AttackTimer - 30)) / 20f;

				if (AttackTimer > 60 && Main.rand.NextBool(60))
				{
					AttackTimer = 0;
					PickTarget();

					if (NPC.target == -1)
					{
						Phase = 0;
						return;
					}

					Teleport(Target.Center + (Main.rand.NextBool() ? -1 : 1) * Vector2.UnitX.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(450, 600));
					Phase = 2;
				}
			}

			if (AttackTimer % 4 == 0)
			{
				frameCounter = ++frameCounter % 7;
			}
		}

		/// <summary>
		/// Charge at the targeted player
		/// </summary>
		private void AttackCharge()
		{
			if (NPC.Opacity < 1)
				NPC.Opacity += 0.05f;

			if (AttackTimer < TelegraphTime)
			{
				frameCounter = 0;
				NPC.Center += Vector2.One.RotatedBy(Timer * 0.005f) * 0.25f;

				float rotDifference = (((Target.Center - NPC.Center).ToRotation() - NPC.rotation) % MathHelper.TwoPi + MathHelper.Pi * 3) % MathHelper.TwoPi - MathHelper.Pi;
				NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.rotation + rotDifference, 0.01f);
			}

			if (AttackTimer > TelegraphTime  && AttackTimer < TelegraphTime + 15)
			{
				for (int k = 0; k < chains.Length; k++)
				{
					VerletChain chain = chains[k];

					for (int i = 0; i < chain.ropeSegments.Count; i++)
					{
						chain.ropeSegments[i].posNow += NPC.rotation.ToRotationVector2() * 2 + NPC.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * (k - chains.Length / 2 + 1) * (float)(Math.Pow(2f * i / chain.ropeSegments.Count - 1, 2) + 1);
					}
				}
			}

			if (AttackTimer == TelegraphTime || AttackTimer == TelegraphTime + 2 || AttackTimer == TelegraphTime + 5 || AttackTimer == TelegraphTime + 10 || AttackTimer == TelegraphTime + 65)
				frameCounter++;

			if (AttackTimer == TelegraphTime + 70)
				frameCounter = 2;

			if (AttackTimer == TelegraphTime + 75 || AttackTimer == TelegraphTime + 80)
				frameCounter--;

			if (AttackTimer > TelegraphTime && AttackTimer <  TelegraphTime + 10)
				NPC.velocity += NPC.rotation.ToRotationVector2() * 7.5f;

			NPC.velocity *= 0.96f;

			if (AttackTimer > TelegraphTime + 6)
				NPC.velocity *= 0.98f;

			if (AttackTimer == TelegraphTime + 60)
				NPC.velocity *= -1;

			if (AttackTimer > TelegraphTime + 90)
			{
				AttackTimer = 0;
				Phase = 1;
				return;
			}
		}

		private void AttackShoot()
		{
			if (NPC.Opacity < 1)
				NPC.Opacity += 0.05f;

			AttackTimer = 0;
			Phase = 1;
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frame.Width = 244;
			NPC.frame.Height = 198;

			NPC.frame.X = 0;

			if (Phase == 2)
				NPC.frame.X = 244;

			NPC.frame.Y = 198 * frameCounter;
		}

		public void DrawToMetaballs(SpriteBatch spriteBatch)
		{
			if (NPC.active)
			{
				Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.MoonstoneNPC + "Dreambeast").Value;

				foreach (VerletChain chain in chains)
				{
					foreach (RopeSegment segment in chain.ropeSegments)
					{
						spriteBatch.Draw(tex, segment.ScreenPos / 2, NPC.frame, Color.White * NPC.Opacity, 0, new Vector2(122, 99), 0.05f, 0, 0);
					}
				}

				spriteBatch.Draw(tex, (NPC.Center - Main.screenPosition) / 2, NPC.frame, Color.White * NPC.Opacity, NPC.rotation + (NPC.direction == -1 ? MathHelper.Pi : 0), new Vector2(122, 99), 0.5f, NPC.direction == -1 ? SpriteEffects.FlipHorizontally : 0, 0);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			spriteBatch.End();
			spriteBatch.Begin(default, default, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);

			if (AppearVisible && flashTime > 0)
			{
				Texture2D flashTex = ModContent.Request<Texture2D>("StarlightRiver/Assets/Keys/GlowAlpha").Value;
				Color color = Color.White * (1 - flashTime / 30f);
				color.A = 0;

				spriteBatch.Draw(flashTex, NPC.Center - Main.screenPosition, null, color, 0, flashTex.Size() / 2, flashTime, 0, 0);
			}

			return false;
		}

		public string GetHint()
		{
			return "It's not real. It's not real. It's not real. IT'S NOT REAL. IT'S NOT REAL. IT'S NOT REAL.";
		}
	}

	internal class DreamBeastActor : MetaballActor
	{
		public NPC activeBeast;

		public override bool Active => NPC.AnyNPCs(ModContent.NPCType<Dreambeast>());

		public override Color OutlineColor => new Color(220, 200, 255) * (activeBeast?.Opacity ?? 0);

		public override void DrawShapes(SpriteBatch spriteBatch)
		{
			for (int k = 0; k < Main.maxNPCs; k++)
			{
				NPC npc = Main.npc[k];

				if (npc.ModNPC is Dreambeast)
					(npc.ModNPC as Dreambeast).DrawToMetaballs(spriteBatch);
			}
		}

		public override bool PostDraw(SpriteBatch spriteBatch, Texture2D target)
		{
			activeBeast = Main.npc.FirstOrDefault(n => n.active && n.type == ModContent.NPCType<Dreambeast>()); //TODO: proper find for onscreen beast

			if (activeBeast is null)
				return false;

			if (!Main.LocalPlayer.HasBuff(ModContent.BuffType<Buffs.Overcharge>()))
			{
				Effect effect = Terraria.Graphics.Effects.Filters.Scene["MoonstoneBeastEffect"].GetShader().Shader;
				effect.Parameters["baseTexture"].SetValue(target);
				effect.Parameters["distortTexture"].SetValue(ModContent.Request<Texture2D>("StarlightRiver/Assets/Noise/MiscNoise2").Value);
				effect.Parameters["size"].SetValue(target.Size());
				effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.005f);
				effect.Parameters["opacity"].SetValue(activeBeast.Opacity);
				effect.Parameters["noiseSampleSize"].SetValue(new Vector2(800, 800));
				effect.Parameters["noisePower"].SetValue(100f);

				spriteBatch.End();
				spriteBatch.Begin(default, BlendState.Additive, default, default, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
			}

			spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0, new Vector2(0, 0), 2f, SpriteEffects.None, 0);

			spriteBatch.End();
			spriteBatch.Begin(default, default, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}
	}
}