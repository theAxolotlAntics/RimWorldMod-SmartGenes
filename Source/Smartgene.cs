using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Smartgene
{
    [StaticConstructorOnStartup]
    public static class StartUp
    {
        static StartUp()
        {
            Smartgene.GenerateGenes();
        }
    }

    public static class Smartgene
    {
        // Prefix all generated defNames to avoid collisions with other mods
        private const string DEF_PREFIX = "SG_ForcedTrait_";
        // A high starting order pushes these genes to the bottom of the gene picker,
        // below cosmetic and other standard genes which typically use lower values.
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
                // TraitDef.degreeDatas is the list of all defined degrees for this trait.
                // We generate one gene per degree so nothing gets skipped.
                foreach (TraitDegreeData degreeData in trait.degreeDatas)
                {
                    try
                    {
                        string defName = $"{DEF_PREFIX}{trait.defName}_deg{degreeData.degree}";

                        // Skip if this defName is already registered (e.g. loaded from a saved def)
                        if (DefDatabase<GeneDef>.GetNamedSilentFail(defName) != null)
                            continue;

                        // Build a human-readable label.
                        // If there's only one degree, just use the trait label.
                        // If there are multiple, append the degree label so the user can tell them apart.
                        string traitLabel = degreeData.label ?? trait.label ?? trait.defName;
                        string geneLabel = trait.degreeDatas.Count > 1
                            ? $"Forced Trait: {traitLabel}"
                            : $"Forced Trait: {trait.label ?? trait.defName}";

                        var traitLink = new GeneticTraitData
                        {
                            def = trait,
                            degree = degreeData.degree
                        };

                        var gene = new GeneDef
                        {
                            defName = defName,
                            label = geneLabel,
                            description = $"Carriers of this gene always have the trait: {traitLabel}",
                            labelShortAdj = $"forced {traitLabel}",
                            iconPath = "UI/forceT",
                            displayCategory = category,
                            forcedTraits = new List<GeneticTraitData> { traitLink },
                            biostatCpx = 1,
                            biostatMet = 0,
                            biostatArc = 0,
                            displayOrderInCategory = displayOrder,
                            // Prevents this gene from appearing in randomly generated
                            // xenogerms and genepack loot. It remains fully selectable
                            // in the xenotype editor.
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

            // Return existing category if already registered (handles hot-reload edge cases)
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
