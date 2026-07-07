using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Smartgene
{
    [StaticConstructorOnStartup]
    public static class StartUp
    {
        static StartUp()
        {
            Harmony harmony = new Harmony("Axolotl.SmartGenes");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Confirm the patch actually landed.
            // If you see FAILED below, your old DLL is still deployed — rebuild and
            // copy the new Smartgene.dll to Common/Assemblies/ before testing.
            MethodInfo original = AccessTools.Method(typeof(TraitDef), nameof(TraitDef.DataAtDegree));
            var patches = Harmony.GetPatchInfo(original);
            if (patches != null && patches.Prefixes.Count > 0)
                Log.Message("[SmartGenes] Harmony patch applied successfully. Degree-0 spam suppressed.");
            else
                Log.Error("[SmartGenes] CRITICAL: Harmony patch FAILED. Old DLL may still be deployed. " +
                          "Rebuild the project and ensure Smartgene.dll in Common/Assemblies/ is up to date.");

            Smartgene.GenerateGenes();
        }
    }

    /// <summary>
    /// Silences the "found no data at degree 0, returning first defined" log spam.
    /// RimWorld calls TraitDef.DataAtDegree(0) from multiple places (tooltip rendering,
    /// stat workers, condition checkers) every tick for any trait that lacks degree 0.
    /// We intercept and return the first defined degree silently.
    /// </summary>
    [HarmonyPatch(typeof(TraitDef), nameof(TraitDef.DataAtDegree))]
    public static class Patch_TraitDef_DataAtDegree_Silence
    {
        public static bool Prefix(TraitDef __instance, int degree, ref TraitDegreeData __result)
        {
            // Guard against malformed TraitDefs with no degree data at all
            if (__instance.degreeDatas == null || __instance.degreeDatas.Count == 0)
            {
                __result = null;
                return false;
            }

            // If the requested degree exists, let the original method run normally
            foreach (TraitDegreeData data in __instance.degreeDatas)
            {
                if (data.degree == degree)
                    return true;
            }

            // Degree not found — return first defined degree silently, no log warning
            __result = __instance.degreeDatas[0];
            return false;
        }
    }

    public static class Smartgene
    {
        private const string DEF_PREFIX = "SG_ForcedTrait_";
        private const int BASE_DISPLAY_ORDER = 404;
        private const int DISPLAY_ORDER_STEP = 10;

        public static void GenerateGenes()
        {
            Log.Message("[SmartGenes] Generating forced-trait genes...");

            GeneCategoryDef category = GetOrCreateCategory();

            var genesToAdd = new List<GeneDef>();
            int displayOrder = BASE_DISPLAY_ORDER;

            foreach (TraitDef trait in DefDatabase<TraitDef>.AllDefs)
            {
                foreach (TraitDegreeData degreeData in trait.degreeDatas)
                {
                    try
                    {
                        string defName = $"{DEF_PREFIX}{trait.defName}_deg{degreeData.degree}";

                        // Skip if our prefixed name already exists (e.g. from a previous load)
                        if (DefDatabase<GeneDef>.GetNamedSilentFail(defName) != null)
                            continue;

                        // Also skip if another mod already defines a GeneDef with the same
                        // bare trait name (e.g. "Delicate", "VRE_Flirty") — adding ours on
                        // top would cause a duplicate def error.
                        if (DefDatabase<GeneDef>.GetNamedSilentFail(trait.defName) != null)
                            continue;

                        // Resolve display label through the full fallback chain:
                        // 1. degreeData.GetLabelCap(trait) — localised degree label (e.g. "Night owl")
                        // 2. trait.LabelCap               — trait-level label if degree has none
                        // 3. trait.defName                — last resort (raw, e.g. "VTE_AbsentMinded")
                        string traitLabel;
                        if (!string.IsNullOrEmpty(degreeData.label))
                            traitLabel = degreeData.GetLabelCap(trait);
                        else if (!string.IsNullOrEmpty(trait.label))
                            traitLabel = trait.LabelCap.ToString();
                        else
                            traitLabel = trait.defName;

                        string geneLabel = $"Forced Trait: {traitLabel}";

                        string traitDesc = !string.IsNullOrEmpty(degreeData.description)
                            ? degreeData.description
                            : !string.IsNullOrEmpty(trait.description)
                                ? trait.description
                                : null;

                        string geneDesc = traitDesc != null
                            ? $"Forces the trait: {traitLabel}\n\n{traitDesc}"
                            : $"Carriers of this gene always have the trait: {traitLabel}";

                        var traitLink = new GeneticTraitData
                        {
                            def = trait,
                            degree = degreeData.degree
                        };

                        var gene = new GeneDef
                        {
                            defName = defName,
                            label = geneLabel,
                            description = geneDesc,
                            labelShortAdj = $"forced {traitLabel}",
                            iconPath = "UI/forceT",
                            displayCategory = category,
                            forcedTraits = new List<GeneticTraitData> { traitLink },
                            biostatCpx = 1,
                            biostatMet = 0,
                            biostatArc = 0,
                            displayOrderInCategory = displayOrder,
                            selectionWeight = 0
                        };

                        genesToAdd.Add(gene);
                        displayOrder += DISPLAY_ORDER_STEP;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[SmartGenes] Failed to generate gene for trait '{trait.defName}' degree {degreeData.degree}: {ex}");
                    }
                }
            }

            int added = 0;
            foreach (GeneDef gene in genesToAdd)
            {
                DefDatabase<GeneDef>.Add(gene);
                added++;
            }

            Log.Message($"[SmartGenes] Done. Generated {added} forced-trait genes.");
        }

        private static GeneCategoryDef GetOrCreateCategory()
        {
            const string categoryDefName = "SmartGenes_ForcedTraits";

            GeneCategoryDef existing = DefDatabase<GeneCategoryDef>.GetNamedSilentFail(categoryDefName);
            if (existing != null)
                return existing;

            var category = new GeneCategoryDef
            {
                defName = categoryDefName,
                label = "SmartGenes: Forced Traits",
                description = "Genes that guarantee a pawn will always have a specific trait.",
                displayPriorityInGenepack = -1000,
                displayPriorityInXenotype = 1000
            };

            DefDatabase<GeneCategoryDef>.Add(category);
            return category;
        }
    }
}
