using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WealthOverlay
{

	public class ZoomGridDrawer
	{
		private bool wantDraw;
		private Material material;
		private bool materialCaresAboutVertexColors;
		private bool dirty = true;
		private readonly List<Mesh> meshes = [];
		private readonly int mapSizeX;
		private readonly int mapSizeZ;
		private readonly float opacity = 0.33f;
		private readonly int renderQueue = 3600;
		private readonly Func<Color> colorGetter;
		private readonly Func<int, Color> extraColorGetter;
		private readonly Func<int, bool> cellBoolGetter;
		private readonly static List<Vector3> verts = [];
		private readonly static List<int> tris = [];
		private readonly static List<Color> colors = [];
		private const float DefaultOpacity = 0.33f;
		private const int MaxCellsPerMesh = 16383;

		private ZoomGridDrawer(int mapSizeX, int mapSizeZ, float opacity = DefaultOpacity)
		{
			this.mapSizeX = mapSizeX;
			this.mapSizeZ = mapSizeZ;
			this.opacity = opacity;
		}

		public ZoomGridDrawer(ICellBoolGiver giver, int mapSizeX, int mapSizeZ, float opacity = DefaultOpacity)
			: this(mapSizeX, mapSizeZ, opacity)
		{
			colorGetter = () => giver.Color;
			extraColorGetter = new Func<int, Color>(giver.GetCellExtraColor);
			cellBoolGetter = new Func<int, bool>(giver.GetCellBool);
		}

		public ZoomGridDrawer(ICellBoolGiver giver, int mapSizeX, int mapSizeZ, int renderQueue, float opacity = DefaultOpacity)
			: this(giver, mapSizeX, mapSizeZ, opacity)
		{
			this.renderQueue = renderQueue;
		}

		public ZoomGridDrawer(Func<int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, Color> extraColorGetter, int mapSizeX, int mapSizeZ, float opacity = DefaultOpacity)
			: this(mapSizeX, mapSizeZ, opacity)
		{
			this.colorGetter = colorGetter;
			this.extraColorGetter = extraColorGetter;
			this.cellBoolGetter = cellBoolGetter;
		}

		public ZoomGridDrawer(Func<int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, Color> extraColorGetter, int mapSizeX, int mapSizeZ, int renderQueue, float opacity = DefaultOpacity)
			: this(cellBoolGetter, colorGetter, extraColorGetter, mapSizeX, mapSizeZ, opacity)
		{
			this.renderQueue = renderQueue;
		}

		public void MarkForDraw()
		{
			wantDraw = true;
		}

		public void ZoomGridDrawerUpdate()
		{
			if (wantDraw)
			{
				ActuallyDraw();
				wantDraw = false;
			}
		}

		private void ActuallyDraw()
		{
			if (dirty)
				RegenerateMesh();
			for (int i = 0; i < meshes.Count; i++)
				Graphics.DrawMesh(meshes[i], Vector3.zero, Quaternion.identity, material, 0);
		}

		public void SetDirty()
		{
			dirty = true;
		}

		public void RegenerateMesh()
		{
			for (int i = 0; i < meshes.Count; i++)
				meshes[i].Clear();

			var num = 0;
			var num2 = 0;
			if (meshes.Count < num + 1)
			{
				var mesh = new Mesh { name = "ZoomGridDrawer" };
				meshes.Add(mesh);
			}
			var mesh2 = meshes[num];
			var cellRect = new CellRect(0, 0, mapSizeX, mapSizeZ);
			var num3 = AltitudeLayer.MapDataOverlay.AltitudeFor();
			var flag = false;
			for (var j = cellRect.minX; j <= cellRect.maxX; j++)
				for (var k = cellRect.minZ; k <= cellRect.maxZ; k++)
				{
					var num4 = CellIndicesUtility.CellToIndex(j, k, mapSizeX);
					if (cellBoolGetter(num4))
					{
						verts.Add(new Vector3((float)j, num3, (float)k));
						verts.Add(new Vector3((float)j, num3, (float)(k + 1)));
						verts.Add(new Vector3((float)(j + 1), num3, (float)(k + 1)));
						verts.Add(new Vector3((float)(j + 1), num3, (float)k));
						var color = extraColorGetter(num4);
						colors.Add(color);
						colors.Add(color);
						colors.Add(color);
						colors.Add(color);
						if (color != Color.white)
							flag = true;
						var count = verts.Count;
						tris.Add(count - 4);
						tris.Add(count - 3);
						tris.Add(count - 2);
						tris.Add(count - 4);
						tris.Add(count - 2);
						tris.Add(count - 1);
						num2++;
						if (num2 >= MaxCellsPerMesh)
						{
							FinalizeWorkingDataIntoMesh(mesh2);
							num++;
							if (meshes.Count < num + 1)
							{
								var mesh3 = new Mesh { name = "ZoomGridDrawer" };
								meshes.Add(mesh3);
							}
							mesh2 = meshes[num];
							num2 = 0;
						}
					}
				}
			FinalizeWorkingDataIntoMesh(mesh2);
			CreateMaterialIfNeeded(flag);
			dirty = false;
		}

		private void FinalizeWorkingDataIntoMesh(Mesh mesh)
		{
			if (verts.Count > 0)
			{
				mesh.SetVertices(verts);
				verts.Clear();
				mesh.SetTriangles(tris, 0);
				tris.Clear();
				mesh.SetColors(colors);
				colors.Clear();
			}
		}

		private void CreateMaterialIfNeeded(bool careAboutVertexColors)
		{
			if (material == null || materialCaresAboutVertexColors != careAboutVertexColors)
			{
				Color color = colorGetter();
				material = SolidColorMaterials.SimpleSolidColorMaterial(new Color(color.r, color.g, color.b, opacity * color.a), careAboutVertexColors);
				materialCaresAboutVertexColors = careAboutVertexColors;
				material.renderQueue = renderQueue;
			}
		}
	}
}
