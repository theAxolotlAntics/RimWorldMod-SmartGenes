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
            // Apply Harmony patches first, then generate genes
            new Harmony("Axolotl.SmartGenes").PatchAll(Assembly.GetExecutingAssembly());
            Smartgene.GenerateGenes();
        }
    }

    /// <summary>
    /// Silences the "found no data at degree 0, returning first defined" log spam.
    /// RimWorld's gene tooltip calls TraitDef.DataAtDegree(0) to render the
    /// "Forced traits:" section, but many traits have no degree 0 defined.
    /// We patch the method to return the first defined degree silently instead
    /// of logging a warning every time the player mouses over a gene card.
    /// </summary>
    [HarmonyPatch(typeof(TraitDef), nameof(TraitDef.DataAtDegree))]
    public static class Patch_TraitDef_DataAtDegree_Silence
    {
        public static bool Prefix(TraitDef __instance, int degree, ref TraitDegreeData __result)
        {
            // Check whether this degree actually exists
            foreach (TraitDegreeData data in __instance.degreeDatas)
            {
                if (data.degree == degree)
                    return true; // Degree found — let the original method run normally
            }

            // Degree not found — return first defined degree silently (no log warning)
            __result = __instance.degreeDatas[0];
            return false; // Skip original method
        }
    }

    public static class Smartgene
    {
        private const string DEF_PREFIX = "SG_ForcedTrait_";
        // High starting order pushes these genes below cosmetic/standard genes in the picker
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

                        // Resolve display label through the full fallback chain:
                        // 1. degreeData.GetLabelCap(trait) — proper localised degree label (e.g. "Night owl")
                        // 2. trait.LabelCap                — trait-level label if degree has none
                        // 3. trait.label                   — unlocalised fallback
                        // 4. trait.defName                 — last resort (raw, e.g. "VTE_AbsentMinded")
                        string traitLabel;
                        if (!string.IsNullOrEmpty(degreeData.label))
                            traitLabel = degreeData.GetLabelCap(trait);
                        else if (!string.IsNullOrEmpty(trait.label))
                            traitLabel = trait.LabelCap.ToString();
                        else
                            traitLabel = trait.defName;

                        string geneLabel = $"Forced Trait: {traitLabel}";

                        // Bake description text onto the gene so the tooltip renderer
                        // doesn't need to call DataAtDegree at all for the description field.
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
                            // Excluded from random xenogerm/genepack loot; still selectable in editor
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
