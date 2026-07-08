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
            if (__instance.degreeDatas == null || __instance.degreeDatas.Count == 0)
            {
                __result = null;
                return false;
            }

            foreach (TraitDegreeData data in __instance.degreeDatas)
            {
                if (data.degree == degree)
                    return true;
            }

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

                        if (DefDatabase<GeneDef>.GetNamedSilentFail(defName) != null)
                            continue;

                        if (DefDatabase<GeneDef>.GetNamedSilentFail(trait.defName) != null)
                            continue;

                        // Resolve display label
                        string traitLabel;
                        if (!string.IsNullOrEmpty(degreeData.label))
                            traitLabel = degreeData.label.CapitalizeFirst();
                        else if (!string.IsNullOrEmpty(trait.label))
                            traitLabel = trait.LabelCap.ToString();
                        else
                            traitLabel = trait.defName;

                        // Resolve trait description
                        string traitDesc = !string.IsNullOrEmpty(degreeData.description)
                            ? degreeData.description
                            : !string.IsNullOrEmpty(trait.description)
                                ? trait.description
                                : null;

                        // Find which mod this trait comes from.
                        // modContentPack is null for base-game content, so we fall back to "RimWorld".
                        string modSource = trait.modContentPack?.Name ?? "RimWorld";

                        // Build the full gene description
                        string geneDesc;
                        if (traitDesc != null)
                            geneDesc = $"Forces the trait: {traitLabel}\n\n{traitDesc}\n\n<color=#aaaaaa>Source: {modSource}</color>";
                        else
                            geneDesc = $"Carriers of this gene always have the trait: {traitLabel}\n\n<color=#aaaaaa>Source: {modSource}</color>";

                        string geneLabel = $"Forced Trait: {traitLabel}";

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
