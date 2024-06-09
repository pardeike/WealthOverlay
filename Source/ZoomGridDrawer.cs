using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WealthOverlay
{
	public interface IZoomGridBoolGiver
	{
		Color Color { get; }

		bool GetCellBool(int x, int z);
		Color GetCellExtraColor(int x, int z);
	}

	public class ZoomGridDrawer
	{
		private const float defaultOpacity = 0.33f;
		private const int maxCellsPerMesh = 16383;

		private bool wantDraw;
		private Material material;
		private bool materialCaresAboutVertexColors;
		private bool dirty = true;
		private readonly List<Mesh> meshes = [];
		private readonly int sizeX;
		private readonly int sizeZ;
		private readonly int scale;
		private readonly float opacity = 0.33f;
		private readonly int renderQueue = 3600;
		private readonly Func<Color> colorGetter;
		private readonly Func<int, int, Color> extraColorGetter;
		private readonly Func<int, int, bool> cellBoolGetter;
		private readonly static List<Vector3> verts = [];
		private readonly static List<int> tris = [];
		private readonly static List<Color> colors = [];

		private ZoomGridDrawer(int sizeX, int sizeZ, int scale, float opacity = defaultOpacity)
		{
			this.sizeX = sizeX;
			this.sizeZ = sizeZ;
			this.scale = scale;
			this.opacity = opacity;
		}

		public ZoomGridDrawer(IZoomGridBoolGiver giver, int sizeX, int sizeZ, int scale = 1, float opacity = defaultOpacity)
			: this(sizeX, sizeZ, scale, opacity)
		{
			colorGetter = () => giver.Color;
			extraColorGetter = new Func<int, int, Color>(giver.GetCellExtraColor);
			cellBoolGetter = new Func<int, int, bool>(giver.GetCellBool);
		}

		public ZoomGridDrawer(IZoomGridBoolGiver giver, int sizeX, int sizeZ, int scale, int renderQueue, float opacity = defaultOpacity)
			: this(giver, sizeX, sizeZ, scale, opacity)
		{
			this.renderQueue = renderQueue;
		}

		public ZoomGridDrawer(Func<int, int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, int, Color> extraColorGetter, int sizeX, int sizeZ, int scale, float opacity = defaultOpacity)
			: this(sizeX, sizeZ, scale, opacity)
		{
			this.colorGetter = colorGetter;
			this.extraColorGetter = extraColorGetter;
			this.cellBoolGetter = cellBoolGetter;
		}

		public ZoomGridDrawer(Func<int, int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, int, Color> extraColorGetter, int sizeX, int sizeZ, int scale, int renderQueue, float opacity = defaultOpacity)
			: this(cellBoolGetter, colorGetter, extraColorGetter, sizeX, sizeZ, scale, opacity)
		{
			this.renderQueue = renderQueue;
		}

		public void MarkForDraw() => wantDraw = true;

		public void ZoomGridDrawerUpdate()
		{
			if (wantDraw)
			{
				if (dirty)
					RegenerateMesh();
				for (int i = 0; i < meshes.Count; i++)
					Graphics.DrawMesh(meshes[i], Vector3.zero, Quaternion.identity, material, 0);

				wantDraw = false;
			}
		}

		public void SetDirty() => dirty = true;

		public void Cleanup()
		{
			for (int j = 0; j < meshes.Count; j++)
				meshes[j].Clear();
			meshes.Clear();

			//UnityEngine.Object.Destroy(material);
			//verts.Clear();
			//tris.Clear();
			//colors.Clear();
		}

		public void RegenerateMesh()
		{
			for (int j = 0; j < meshes.Count; j++)
				meshes[j].Clear();

			var n = 0;
			var i = 0;
			if (meshes.Count < n + 1)
				meshes.Add(new Mesh());
			var mesh = meshes[n];
			var cellRect = new CellRect(0, 0, sizeX, sizeZ);
			var altitude = AltitudeLayer.MapDataOverlay.AltitudeFor();
			var useCustomColor = false;
			for (var x = cellRect.minX; x <= cellRect.maxX; x++)
				for (var z = cellRect.minZ; z <= cellRect.maxZ; z++)
				{
					if (cellBoolGetter(x, z))
					{
						var x1 = x * scale;
						var x2 = x1 + scale;
						var z1 = z * scale;
						var z2 = z1 + scale;

						verts.Add(new Vector3(x1, altitude, z1));
						verts.Add(new Vector3(x1, altitude, z2));
						verts.Add(new Vector3(x2, altitude, z2));
						verts.Add(new Vector3(x2, altitude, z1));

						var color = extraColorGetter(x, z);
						colors.Add(color);
						colors.Add(color);
						colors.Add(color);
						colors.Add(color);

						if (color != Color.white)
							useCustomColor = true;

						var count = verts.Count;
						tris.Add(count - 4);
						tris.Add(count - 3);
						tris.Add(count - 2);
						tris.Add(count - 4);
						tris.Add(count - 2);
						tris.Add(count - 1);

						if (++i >= maxCellsPerMesh)
						{
							FinalizeWorkingDataIntoMesh(mesh);
							n++;
							if (meshes.Count < n + 1)
								meshes.Add(new Mesh());
							mesh = meshes[n];
							i = 0;
						}
					}
				}
			FinalizeWorkingDataIntoMesh(mesh);
			CreateMaterialIfNeeded(useCustomColor);
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
				var color = colorGetter();
				color.a = opacity * color.a;
				material = SolidColorMaterials.SimpleSolidColorMaterial(color, careAboutVertexColors);
				materialCaresAboutVertexColors = careAboutVertexColors;
				material.renderQueue = renderQueue;
			}
		}
	}
}
