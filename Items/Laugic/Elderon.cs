using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace UnmasterMode.Items.Laugic
{
    class Elderon : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Elderon");
            Tooltip.SetDefault("One sword to conquer all.");
        }

        public override void SetDefaults()
        {
            item.damage = 10;
            item.melee = true;
            item.width = 48;
            item.height = 48;
            item.useTime = 30;
            item.useAnimation = item.useTime;
            item.knockBack = 4;
            item.value = Item.buyPrice(silver:50);
            item.rare = ItemRarityID.Green;
            item.UseSound = SoundID.Item1;
            item.autoReuse = true;
            item.crit = 8;
        }
    }
}
