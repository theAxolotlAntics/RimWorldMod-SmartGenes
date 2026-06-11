using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using UnityEngine.UIElements;
using Verse;

namespace Smartgene
{
    [StaticConstructorOnStartup]
    public class StartUp
    {
        static StartUp()
        {
            Smartgene spine = new Smartgene(); // Create an instance of the Smartgene class on startup
        }
    }

    public class Smartgene
    {
        List<GeneDef> generatedGenes = new List<GeneDef>(); // Create a list to store generated GeneDefs

        public Smartgene()
        {
            Log.Message("Smart Gene is generating genes"); // Log a message indicating gene generation is in progress
            int x = 404;
            GeneCategoryDef SGFT = new GeneCategoryDef(); // Create a new GeneCategoryDef object
            SGFT.defName = "Smart_Genes_ForcedTraits"; // Set the definition name for the gene category
            SGFT.label = "SmartGenes : Forced Traits";
            SGFT.description = "A collection of Genes used to guarantee that a pawn will have a specific trait."; // Set the description of the gene category
            SGFT.displayPriorityInGenepack = -1000;
            SGFT.displayPriorityInXenotype = 1000;
            Verse.DefDatabase<GeneCategoryDef>.Add(SGFT);
            // Loop through all TraitDefs in the game
            foreach (TraitDef trait in Verse.DefDatabase<TraitDef>.AllDefs)
            {
                try
                {
                    //Log.Message("Generating Gene for " + trait); // Log a message for each trait being processed

                    GeneDef Smart = new GeneDef(); // Create a new GeneDef object for the current trait
                    List<Verse.GeneticTraitData> bob = new List<Verse.GeneticTraitData>(); // Create a list for the trait because forced traits needs a list
                    GeneticTraitData traitData = new GeneticTraitData();
                    traitData.def = trait;
                    traitData.degree = 0;
                    bob.Add(traitData);
                    Smart.defName = trait.defName; // Set the definition name of the gene to the trait's definition name
                    Smart.label = "Forced Trait: " + trait.defName; // Set the label of the gene
                    Smart.displayCategory = SGFT; // Set the display category of the gene
                    Smart.labelShortAdj = "forced " + trait.defName; // Set the short adjective label of the gene
                    Smart.description = "Carriers of this gene always have the trait: " + trait.defName; // Set the description of the gene
                    Smart.iconPath = "UI/forceT"; // Set the icon path of the gene
                    Smart.forcedTraits = bob; // Set the forced traits of the gene
                    Smart.biostatCpx = 1; // Set the complexity biostat of the gene
                    Smart.biostatMet = 0; // Set the metabolism biostat of the gene
                    Smart.biostatArc = 0; // Set the arcotech biostat of the gene
                    Smart.displayOrderInCategory = x;
                    generatedGenes.Add(Smart); // Add the generated GeneDef to the list of generated genes
                    x += 10;
                }
                catch (Exception genedef){
                    Log.Message(genedef);
                }
            }
            foreach (GeneDef gene in generatedGenes)
            {
                if (!gene.generated)
                { // checks to make sure that the gene does not already exist.
                    Verse.DefDatabase<GeneDef>.Add(gene);
                }
            }
        }
    }
}

