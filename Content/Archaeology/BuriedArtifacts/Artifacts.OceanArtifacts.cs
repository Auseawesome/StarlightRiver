﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Core;
using StarlightRiver.Content.Archaeology;
using StarlightRiver.Content.Items.BuriedArtifacts;
using StarlightRiver.Helpers;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace StarlightRiver.Content.Archaeology.BuriedArtifacts
{
    public class PirateChestArtifact : OceanArtifact
    {
        public override Vector2 Size => new Vector2(32, 24);

        public override float SpawnChance => 0.5f;

        public override int ItemType => ModContent.ItemType<PirateChestArtifactItem>();

        public override int SparkleDust => ModContent.DustType<Dusts.ArtifactSparkles.GoldArtifactSparkle>();

        public override int SparkleRate => 40;

        public override Color BeamColor => Color.Gold;
    }
}