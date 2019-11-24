using Kingmaker.Globalmap.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony12;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Tasks;
using UnityEngine;

namespace CallOfTheWild.GlobalMap
{

        /*[Harmony12.HarmonyPatch(typeof(BlueprintLocation))]
        [Harmony12.HarmonyPatch("GetPerceptionDC", Harmony12.MethodType.Normal)]
        class BlueprintLocation__GetPerceptionDC__Patch
        {
            static bool Prefix(BlueprintLocation __instance, ref int __result)
            {
                __result = 0;
                return false;
            }
        }*/
        [HarmonyPatch(typeof(KingdomEvent), "CalculateRulerTime")]
        static class KingdomEvent_CalculateRulerTime_Patch
        {
            static void Postfix(KingdomEvent __instance, ref int __result)
            {
                try
                {
                    if (__result > 14)
                        __result /= 2;
                    else if (__result > 7)
                        __result = 7;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }



        /*[HarmonyPatch(typeof(KingdomTaskEvent), "SkipPlayerTime", MethodType.Getter)]
        static class KingdomTaskEvent_SkipPlayerTime_Patch
        {
            static void Postfix(ref int __result)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (__result < 1) return;
                    if(__result > 14)
                        __result /= 2;
                    else if (__result > 7)
                        __result = 7;
                    
                    __result = __result < 1 ? 1 : __result;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }*/
}
