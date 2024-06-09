using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace WealthOverlay
{
	[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
	public static class UIRoot_Play_UIRootOnGUI_Patch
	{
		public static void Prefix()
		{
			if (Find.CurrentMap == null)
				return;

			if (Event.current.isKey && Event.current.type == EventType.KeyDown)
			{
				var wealth = Find.CurrentMap.GetComponent<Wealth>();
				if (Event.current.keyCode == KeyCode.LeftBracket)
				{
					if (wealth.Zoom > 1)
						wealth.Zoom--;
					Event.current.Use();
				}
				if (Event.current.keyCode == KeyCode.RightBracket)
				{
					wealth.Zoom++;
					Event.current.Use();
				}
			}
		}

	}

	[HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount))]
	public static class WealthWatcher_ForceRecount_Patch
	{
		public static void Prefix(Map ___map)
		{
			var wealth = ___map.GetComponent<Wealth>();
			wealth.Reset();
		}

		public static void Postfix(Map ___map)
		{
			var wealth = ___map.GetComponent<Wealth>();
			var sum = wealth.TotalWealth;
			Log.Warning("Total wealth: " + sum);
		}

		public static Wealth GetWealth(WealthWatcher watcher) => watcher.map.GetComponent<Wealth>();

		public static void Register(float marketValue, Thing thing, Wealth wealth) => wealth.Register(marketValue, thing);
		public static void Register(float marketValue, Pawn pawn, Wealth wealth) => wealth.Register(marketValue, pawn);

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var vWealth = generator.DeclareLocal(typeof(Wealth));
			var vThing = generator.DeclareLocal(typeof(Thing));
			var vPawn = generator.DeclareLocal(typeof(Pawn));
			var mGetWealth = SymbolExtensions.GetMethodInfo(() => GetWealth(default));
			var mGetStatValue = SymbolExtensions.GetMethodInfo(() => StatExtension.GetStatValue(default, default, default, default));
			var mGetIsSlave = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsSlave));
			var mRegisterThing = SymbolExtensions.GetMethodInfo(() => Register(default, (Thing)default, default));
			var mRegisterPawn = SymbolExtensions.GetMethodInfo(() => Register(default, default, default));
			var fWealthPawns = AccessTools.Field(typeof(WealthWatcher), nameof(WealthWatcher.wealthPawns));
			return new CodeMatcher(instructions)
				.Advance(1)
				.Insert(
					Code.Ldarg_0,
					Code.Call[mGetWealth],
					Code.Stloc[vWealth]
				)
				.MatchStartForward(new CodeMatch(OpCodes.Ldsfld), CodeMatch.LoadsConstant(), CodeMatch.LoadsConstant(), new CodeMatch(operand: mGetStatValue))
				.InsertAndAdvance(
					Code.Dup,
					Code.Stloc[vThing]
				)
				.Advance(4)
				.InsertAndAdvance(
					Code.Dup,
					Code.Ldloc[vThing],
					Code.Ldloc[vWealth],
					Code.Call[mRegisterThing]
				)
				.MatchStartForward(new CodeMatch(operand: mGetIsSlave))
				.InsertAndAdvance(
					Code.Dup,
					Code.Stloc[vPawn]
				)
				.MatchEndForward(new CodeMatch(opcode: OpCodes.Ldfld, operand: fWealthPawns), CodeMatch.LoadsLocal(), new CodeMatch(OpCodes.Add))
				.InsertAndAdvance(
					Code.Dup,
					Code.Ldloc[vPawn],
					Code.Ldloc[vWealth],
					Code.Call[mRegisterPawn]
				)
				.InstructionEnumeration();
		}
	}

	[HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.CalculateWealthItems))]
	public static class WealthWatcher_CalculateWealthItems_Patch
	{
		public static Wealth GetWealth(WealthWatcher watcher) => watcher.map.GetComponent<Wealth>();

		public static void Register(float marketValue, Thing thing, Wealth wealth) => wealth.Register(marketValue, thing);

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var vWealth = generator.DeclareLocal(typeof(Wealth));
			var vThing = generator.DeclareLocal(typeof(Thing));
			var mGetWealth = SymbolExtensions.GetMethodInfo(() => GetWealth(default));
			var mGetPositionHeld = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.PositionHeld));
			var mRegister = SymbolExtensions.GetMethodInfo(() => Register(default, default, default));
			return new CodeMatcher(instructions)
				.Advance(1)
				.Insert(
					Code.Ldarg_0,
					Code.Call[mGetWealth],
					Code.Stloc[vWealth]
				)
				.MatchStartForward(new CodeMatch(operand: mGetPositionHeld))
				.InsertAndAdvance(
					Code.Dup,
					Code.Stloc[vThing]
				)
				.MatchEndForward(new CodeMatch(OpCodes.Mul), new CodeMatch(OpCodes.Add), new CodeMatch(OpCodes.Stloc_0))
				.InsertAndAdvance(
					Code.Dup,
					Code.Ldloc[vThing],
					Code.Ldloc[vWealth],
					Code.Call[mRegister]
				)
				.InstructionEnumeration();
		}
	}

	[HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.CalculateWealthFloors))]
	public static class WealthWatcher_CalculateWealthFloors_Patch
	{
		public static Wealth GetWealth(WealthWatcher watcher) => watcher.map.GetComponent<Wealth>();

		public static void Register(float marketValue, int index, Wealth wealth) => wealth.Register(marketValue, index);

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var vWealth = generator.DeclareLocal(typeof(Wealth));
			var vIndex = generator.DeclareLocal(typeof(int));
			var mGetWealth = SymbolExtensions.GetMethodInfo(() => GetWealth(default));
			var mRegister = SymbolExtensions.GetMethodInfo(() => Register(default, default, default));
			return new CodeMatcher(instructions)
				.Advance(1)
				.Insert(
					Code.Ldarg_0,
					Code.Call[mGetWealth],
					Code.Stloc[vWealth]
				)
				.MatchStartForward(
					new CodeMatch(OpCodes.Ldelem_Ref),
					new CodeMatch(OpCodes.Ldfld),
					new CodeMatch(OpCodes.Ldelem_R4),
					new CodeMatch(OpCodes.Add)
				)
				.InsertAndAdvance(
					Code.Dup,
					Code.Stloc[vIndex]
				)
				.Advance(3)
				.InsertAndAdvance(
					Code.Dup,
					Code.Ldloc[vIndex],
					Code.Ldloc[vWealth],
					Code.Call[mRegister]
				)
				.InstructionEnumeration();
		}
	}
}