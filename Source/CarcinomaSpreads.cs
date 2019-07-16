/// <summary>
///   Harmony Patch for Carcinoma Spreads
///     This patch fixes a problem when a pawn becomes
///     horribly diseased with carcinoma in every part
///     of their body.  The game throws errors when it
///     cannot find a new body part to spread to.
///     This patch fixes HediffGiverUtility's TryApply
///     to use TryRandomElementsByWeight instead, thus
///     resolving the problem.
/// </summary>
/// <license>
///   This patch assembly library is licensed under the LGPL v3.
///   You can find the text of the license at:
///   https://www.gnu.org/licenses/lgpl-3.0.en.html
///   Copyright 2019 LWM
/// </license>



using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using UnityEngine;

using RimWorld;
using Verse;

using Harmony;
using System.Reflection;      // may be useful for Target(), &c.
using System.Reflection.Emit; // for OpCodes in Harmony Transpiler


namespace LWM.CarcinomaSpreads
{
    /****      HediffGiverUtility's TryApply() 
     * Replace the call RandomElementsByWeight with TryRandomElementsByWeight
     * (done via utility function because I don't know how to handle IL code for 
     * 'out' variables off the top of my head and don't care to look it up right now)
     */
    [HarmonyPatch(typeof(HediffGiverUtility), "TryApply")]
    static public class Patch_TryApply_For_CarcinomaSpreads {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen) {
            var code = new List<CodeInstruction>(instructions);
            for (int i = 0; i < code.Count; i++) {
                if (code[i].opcode == OpCodes.Call &&
                    code[i].operand == AccessTools.Method(typeof(GenCollection), "RandomElementByWeight",
                                                          null, new Type[] { typeof(BodyPartRecord) })
                    ) {
                    code[i].operand = AccessTools.Method("Patch_TryApply_For_CarcinomaSpreads:RandomElementByWeightCS");
                    yield return code[i++]; // return RandomElementsByWeightCS()
                    yield return code[i];   // and advance to store the result.  Stloc_S, somewhere
                    yield return new CodeInstruction(OpCodes.Ldloc_S, code[i].operand); // get it back on the stack.
                    var skipLabel = gen.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brtrue, skipLabel); // if it's non-null, continue with regular
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0); // otherwise return false
                    yield return new CodeInstruction(OpCodes.Ret);
                    if (code[i + 1].labels == null) code[i + 1].labels = new List<Label>();
                    code[i + 1].labels.Add(skipLabel);
                } else {
                    yield return code[i];
                }

            }
        }
        public static BodyPartRecord RandomElementByWeightCS(this IEnumerable<BodyPartRecord> source, 
                                                          Func<BodyPartRecord, float> weightSelector) {
            if (source.TryRandomElementByWeight(weightSelector, out BodyPartRecord x)) {
                return x;
            }
            return null;
        }
    }

}
