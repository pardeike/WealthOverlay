using Brrainz;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace WealthOverlay
{
	public class WealthOverlayMain : Mod
	{
		public static WealthOverlaySettings Settings;

		public WealthOverlayMain(ModContentPack content) : base(content)
		{
			Settings = GetSettings<WealthOverlaySettings>();

			var harmony = new Harmony("brrainz.wealthoverlay");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);
		}

		public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
		public override string SettingsCategory() => "Wealth Overlay";
	}
}