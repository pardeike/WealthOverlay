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
		}

		public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
		public override string SettingsCategory() => "Wealth Overlay";
	}
}