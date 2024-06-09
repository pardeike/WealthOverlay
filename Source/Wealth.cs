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

	public class Wealth(Map map) : MapComponent(map), IZoomGridBoolGiver
	{
		private int zoom = 1, xSize, zSize;
		private float cellMax = 0;
		private float[] wealthGrid;
		private ZoomGridDrawer drawer;

		public int Zoom
		{
			get => zoom;
			set
			{
				zoom = value;
				Log.Warning("Zoom: " + zoom);
				Reset();
				map.wealthWatcher.ForceRecount(true);
			}
		}

		public void Reset()
		{
			xSize = map.Size.x / zoom + 1;
			zSize = map.Size.z / zoom + 1;
			wealthGrid = new float[xSize * zSize];
			drawer?.Cleanup();
			drawer = new ZoomGridDrawer(this, xSize, zSize, zoom, 0.9f);
			drawer.SetDirty();
		}

		public int CellToIndex(IntVec3 cell) => (cell.z / zoom) * xSize + (cell.x / zoom);

		public void Register(float marketValue, Thing thing)
		{
			var idx = CellToIndex(thing.PositionHeld);
			wealthGrid[idx] += marketValue;
			drawer.SetDirty();
		}

		public void Register(float marketValue, Pawn pawn)
		{
			var idx = CellToIndex(pawn.PositionHeld);
			wealthGrid[idx] += marketValue;
			drawer.SetDirty();
		}

		public void Register(float marketValue, int index)
		{
			var idx = CellToIndex(map.cellIndices.IndexToCell(index));
			wealthGrid[idx] += marketValue;
			drawer.SetDirty();
		}

		public void UpdateDrawer()
		{
			drawer.MarkForDraw();
			cellMax = MaxCellWealth;
			drawer.ZoomGridDrawerUpdate();
		}

		public float TotalWealth => wealthGrid.Sum();

		public float MaxCellWealth => wealthGrid.Max();

		public Color Color => Color.yellow;

		public bool GetCellBool(int x, int z) => cellMax > 0 && wealthGrid[z * xSize + x] > 0;

		public Color GetCellExtraColor(int x, int z) => Color.yellow.ToTransparent(wealthGrid[z * xSize + x] / cellMax);

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref zoom, "zoom", 1);
		}
	}
}