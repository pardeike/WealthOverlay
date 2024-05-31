using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace WealthOverlay
{
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate))]
	public static class MapInterface_MapInterfaceUpdate_Patch
	{
		public static void Postfix()
		{
			var wealth = Find.CurrentMap?.GetComponent<Wealth>();
			wealth?.UpdateDrawer();
		}
	}

	public class Wealth : MapComponent, ICellBoolGiver
	{
		private int zoom = 1;
		private float[] wealthGrid;
		private readonly ZoomGridDrawer drawer;

		public Wealth(Map map) : base(map)
		{
			drawer = new ZoomGridDrawer(this, map.Size.x, map.Size.z, 0.5f);
			Reset();
		}

		public void Reset() => Zoom = zoom;

		public void Register(float marketValue, Thing thing)
		{
			var index = map.cellIndices.CellToIndex(thing.PositionHeld);
			wealthGrid[index] += marketValue;
			drawer.SetDirty();
		}

		public void Register(float marketValue, Pawn pawn)
		{
			var index = map.cellIndices.CellToIndex(pawn.PositionHeld);
			wealthGrid[index] += marketValue;
			drawer.SetDirty();
		}

		public void Register(float marketValue, int index)
		{
			wealthGrid[index] += marketValue;
			drawer.SetDirty();
		}

		public void UpdateDrawer()
		{
			drawer.MarkForDraw(); // use to turn drawing on/off
			drawer.ZoomGridDrawerUpdate();
		}

		public float TotalWealth => wealthGrid.Sum();

		public int Zoom
		{
			get => zoom;
			set
			{
				zoom = value;
				wealthGrid = new float[map.Size.x * map.Size.z / zoom / zoom];
				drawer.SetDirty();
			}
		}

		public Color Color => Color.yellow;

		public bool GetCellBool(int index) => wealthGrid[index] > 0;

		public Color GetCellExtraColor(int _) => Color.yellow;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref zoom, "zoom", 1);
		}
	}
}