﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent
{
    public class VanillaCookingRecipeNames : ICookingRecipeNamingHelper
    {
        /// <summary>
        /// Gets the name for ingredients in regards to food.
        /// </summary>
        /// <param name="worldForResolve">The world to resolve in.</param>
        /// <param name="recipeCode">The recipe code.</param>
        /// <param name="stacks">The stacks of items to add.</param>
        /// <returns>The name of the food type.</returns>
        public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
        {
            OrderedDictionary<ItemStack, int> quantitiesByStack = new OrderedDictionary<ItemStack, int>();
            quantitiesByStack = mergeStacks(worldForResolve, stacks);

            CookingRecipe recipe = worldForResolve.Api.GetCookingRecipe(recipeCode);

            if (recipeCode == null || recipe == null || quantitiesByStack.Count == 0) return Lang.Get("unknown");

            int max = 1;
            string MealFormat = "meal";
            string topping = string.Empty;
            ItemStack PrimaryIngredient = null;
            ItemStack SecondaryIngredient = null;
            List<string> OtherIngredients = new List<string>();
            List<string> MashedNames = new List<string>();
            List<string> GarnishedNames = new List<string>();
            List<string> grainNames = new List<string>();
            string mainIngredients;
            string everythingelse = "";



            switch (recipeCode)
            {
                case "soup":
                    {
                        max = 0;
                        foreach (var val in quantitiesByStack)
                        {
                            CookingRecipeIngredient ingred = recipe.GetIngrendientFor(val.Key);
                            if (val.Key.Collectible.Code.Path.Contains("waterportion")) continue;
                            if (ingred?.Code == "topping")
                            {
                                topping = "honeyportion";
                                continue;
                            }


                            if (max < val.Value)
                            {
                                max = val.Value;
                                if (PrimaryIngredient != null)
                                {
                                    SecondaryIngredient = PrimaryIngredient;
                                }
                                PrimaryIngredient = val.Key;
                                
                            }
                            else
                            {
                                OtherIngredients.Add(ingredientName(val.Key, true));
                            }

                        }

                        if (max == 2) max = 3;
                        else if (max == 3) max = 4;
                        else max = 2;

                        break;
                       
                    }

                case "porridge":
                    {
                        max = 0;
                        foreach (var val in quantitiesByStack)
                        {
                            CookingRecipeIngredient ingred = recipe.GetIngrendientFor(val.Key);
                            if (getFoodCat(val.Key) == EnumFoodCategory.Grain)
                            {
                                max++;
                                if (PrimaryIngredient == null)
                                    PrimaryIngredient = val.Key;
                                else if (SecondaryIngredient == null && val.Key != PrimaryIngredient)
                                    SecondaryIngredient = val.Key;

                                continue;
                            }

                            if (ingred?.Code == "topping")
                            {
                                topping = "honeyportion";
                                continue;
                            }

                            MashedNames.Add(ingredientName(val.Key, true));

                        }
                        break;
                    }

                case "meatystew":
                    {
                        max = 0;
                        foreach (var val in quantitiesByStack)
                        {
                            CookingRecipeIngredient ingred = recipe.GetIngrendientFor(val.Key);
                            
                            EnumFoodCategory foodCat = getFoodCat(val.Key);

                            if (foodCat == EnumFoodCategory.Protein)
                            {
                                if (PrimaryIngredient == val.Key || SecondaryIngredient == val.Key)
                                    continue;

                                if (PrimaryIngredient == null)
                                    PrimaryIngredient = val.Key;
                                else if (SecondaryIngredient == null)
                                    SecondaryIngredient = val.Key;
                                else
                                    OtherIngredients.Add(ingredientName(val.Key, true));

                                max += val.Value;

                                continue;
                            }


                            if (ingred?.Code == "topping")
                            {
                                topping = "honeyportion";
                                continue;
                            }

                            OtherIngredients.Add(ingredientName(val.Key, true));
                        }

                        recipeCode = "stew";
                        break;
                    }

                case "vegetablestew":
                    {
                        max = 0;
                        
                        foreach (var val in quantitiesByStack)
                        {
                            if (getFoodCat(val.Key) == EnumFoodCategory.Vegetable)
                            {
                                if (PrimaryIngredient == val.Key || SecondaryIngredient == val.Key)
                                    continue;

                                if (PrimaryIngredient == null)
                                    PrimaryIngredient = val.Key;
                                else if (SecondaryIngredient == null)
                                    SecondaryIngredient = val.Key;
                                else
                                    GarnishedNames.Add(ingredientName(val.Key, true));

                                max += val.Value;

                                continue;
                            }
                            GarnishedNames.Add(ingredientName(val.Key, true));
                        }

                        // Slightly ugly hack for soybean stew
                        if (PrimaryIngredient == null)
                        {
                            foreach (var val in quantitiesByStack)
                            {
                                //CookingRecipeIngredient ingred = recipe.GetIngrendientFor(val.Key); - whats this for?
                                PrimaryIngredient = val.Key;
                                max += val.Value;
                            }
                        }

                        recipeCode = "stew";
                        break;
                    }

                case "scrambledeggs":
                    {
                        max = 0;

                        foreach (var val in quantitiesByStack)
                        {
                            if (val.Key.Collectible.FirstCodePart() == "egg")
                            {
                                PrimaryIngredient = val.Key;
                                max += val.Value;
                                continue;
                            }
                            
                            GarnishedNames.Add(ingredientName(val.Key, true));
                        }


                        recipeCode = "scrambledeggs";
                        break;
                    }

                case "jam":
                    {
                        ItemStack[] fruits = new ItemStack[2];
                        int i = 0;
                        foreach (var val in quantitiesByStack)
                        {
                            if (val.Key.Collectible.NutritionProps?.FoodCategory == EnumFoodCategory.Fruit)
                            {
                                fruits[i++] = val.Key;
                                if (i == 2) break;
                            }
                        }

                        if (fruits[1] != null)
                        {
                            string jamName = fruits[0].Collectible.LastCodePart() + "-" + fruits[1].Collectible.LastCodePart() + "-jam";
                            if (Lang.HasTranslation(jamName)) return Lang.Get(jamName);

                            string firstFruitInJam = (fruits[0].Collectible.Code.Domain == "game" ? "" : fruits[0].Collectible.Code.Domain + ":") + fruits[0].Collectible.LastCodePart() + "-in-jam-name";
                            string secondFruitInJam = (fruits[1].Collectible.Code.Domain == "game" ? "" : fruits[1].Collectible.Code.Domain + ":") + fruits[1].Collectible.LastCodePart() + "-in-jam-name";
                            return Lang.Get("mealname-mixedjam", Lang.HasTranslation(firstFruitInJam) ? Lang.Get(firstFruitInJam) : fruits[0].GetName(), Lang.HasTranslation(secondFruitInJam) ? Lang.Get(secondFruitInJam) : fruits[1].GetName());
                        }
                        else if (fruits[0] != null)
                        {
                            string jamName = fruits[0].Collectible.LastCodePart() + "-jam";
                            if (Lang.HasTranslation(jamName)) return Lang.Get(jamName);

                            string fruitInJam = (fruits[0].Collectible.Code.Domain == "game" ? "" : fruits[0].Collectible.Code.Domain + ":") + fruits[0].Collectible.Code.Domain + ":" + fruits[0].Collectible.LastCodePart() + "-in-jam-name";
                            return Lang.Get("mealname-singlejam", Lang.HasTranslation(fruitInJam) ? Lang.Get(fruitInJam) : fruits[0].GetName());
                        }
                        else return Lang.Get("unknown");
                    }

                case "glueportion-pitch-hot":
                case "glueportion-pitch-cold":
                    {
                        ItemStack stack = stacks[0];
                        if (stack == null) return Lang.Get("unknown");
                        if (stack.Collectible.Code.PathStartsWith("glueportion")) return stack.Collectible.GetHeldItemName(stack) + "\n\n" + stack.Collectible.GetItemDescText();  // Special case for Hardened pitch glue
                        var outstack = recipe.CooksInto?.ResolvedItemstack;
                        if (outstack != null) return outstack.Collectible.GetHeldItemName(outstack);
                        return Lang.Get("unknown");
                    }
            }



            switch (max)
            {
                case 3:
                    MealFormat += "-hearty-" + recipeCode;
                    break;
                case 4:
                    MealFormat += "-hefty-" + recipeCode;
                    break;
                default:
                    MealFormat += "-normal-" + recipeCode;
                    break;
            }

            if (topping == "honeyportion")
            {
                MealFormat += "-honey";
            }
            //mealformat is done.  Time to do the main inredients.



            if (SecondaryIngredient != null && recipeCode != "scrambledeggs")
            {
                mainIngredients = Lang.Get("multi-main-ingredients-format", getMainIngredientName(PrimaryIngredient, recipeCode), getMainIngredientName(SecondaryIngredient, recipeCode, true));
            }
            else
            {
                mainIngredients = PrimaryIngredient == null ? "" : getMainIngredientName(PrimaryIngredient, recipeCode);
            }


            switch (recipeCode)
            {
                case "porridge":
                    if (MashedNames.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-porridge-mashed", MashedNames);
                    } else
                    {
                        everythingelse = "";
                    }
                    break;
                case "stew":
                    if (OtherIngredients.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-meatystew-boiled", OtherIngredients);
                    }
                    else if (GarnishedNames.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-vegetablestew-garnish", GarnishedNames);
                    }
                    else
                    {
                        everythingelse = "";
                    }
                    break;
                case "scrambledeggs":
                    if (GarnishedNames.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-vegetablestew-garnish", GarnishedNames);
                    }
                    return Lang.Get(MealFormat, everythingelse).Trim().UcFirst();
                case "soup":
                    if(OtherIngredients.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-generic", OtherIngredients);
                    }
                    break;
            }
            //everything else is done.

            return Lang.Get(MealFormat, mainIngredients, everythingelse).Trim().UcFirst();
        }

        private EnumFoodCategory getFoodCat(ItemStack stack)
        {
            FoodNutritionProperties props = stack.Collectible.NutritionProps;
            if (props == null) props = stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible?.NutritionProps;

            if (props != null) return props.FoodCategory;

            return EnumFoodCategory.Dairy;
        }

        private string ingredientName(ItemStack stack, bool InsturmentalCase = false)
        {
            string code;

            code = stack.Collectible.Code?.Domain + AssetLocation.LocationSeparator + "recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code?.Path;

            if (InsturmentalCase)
                code += "-insturmentalcase";

            if (Lang.HasTranslation(code))
            {
                return Lang.GetMatching(code);
            }

            code = stack.Collectible.Code?.Domain + AssetLocation.LocationSeparator + "recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.FirstCodePart();

            if (InsturmentalCase)
                code += "-insturmentalcase";

            return Lang.GetMatching(code);
        }

        private string getMainIngredientName(ItemStack itemstack, string code, bool secondary = false)
        {
            string t = secondary ? "secondary" : "primary";
            string langcode = $"meal-ingredient-{code}-{t}-{getInternalName(itemstack)}";

            if (Lang.HasTranslation(langcode, true))
            {
                return Lang.GetMatching(langcode);
            }

            langcode = $"meal-ingredient-{code}-{t}-{itemstack.Collectible.FirstCodePart()}";
            return Lang.GetMatching(langcode);
        }

        private string getInternalName(ItemStack itemstack)
        {
            return itemstack.Collectible.Code.Path;
        }

        private string getMealAddsString(string code, List<string> ingredients1, List<string> ingredients2 = null)
        {
            if (ingredients2 == null)
                return Lang.Get(code, Lang.Get($"meal-ingredientlist-{ingredients1.Count}", ingredients1.ToArray()));
            return Lang.Get(code, Lang.Get($"meal-ingredientlist-{ingredients1.Count}", ingredients1.ToArray()), Lang.Get($"meal-ingredientlist-{ingredients2.Count}", ingredients2.ToArray()));
        }

        private OrderedDictionary<ItemStack, int> mergeStacks(IWorldAccessor worldForResolve, ItemStack[] stacks)
        {
            OrderedDictionary<ItemStack, int> dict = new OrderedDictionary<ItemStack, int>();

            List<ItemStack> stackslist = new List<ItemStack>(stacks);
            while (stackslist.Count > 0)
            {
                ItemStack stack = stackslist[0];
                stackslist.RemoveAt(0);
                if (stack == null) continue;

                int cnt = 1;

                while (true)
                {
                    ItemStack foundstack = stackslist.FirstOrDefault((otherstack) => otherstack != null && otherstack.Equals(worldForResolve, stack, GlobalConstants.IgnoredStackAttributes));

                    if (foundstack != null)
                    {
                        stackslist.Remove(foundstack);
                        cnt++;
                        continue;
                    }

                    break;
                }

                dict[stack] = cnt;
            }

            return dict;
        }

    }

    /// <summary>
    /// Interface for a helper for cooking various food in game.
    /// </summary>
    public interface ICookingRecipeNamingHelper
    {
        /// <summary>
        /// Gets the name for ingredients in regards to food.
        /// </summary>
        /// <param name="worldForResolve">The world to resolve in.</param>
        /// <param name="recipeCode">The recipe code.</param>
        /// <param name="stacks">The stacks of items to add.</param>
        /// <returns>The name of the food type.</returns>
        string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks);
    }

    /// <summary>
    /// Defines a recipe to be made using a cooking pot.
    /// Creating a new recipe for a cooking pot will automatically register the new meal item, unless using <see cref="CooksInto"/>.
    /// </summary>
    /// <example> 
    /// <code langauge="json">
    ///{
    ///	"code": "jam",
    ///	"perishableProps": {
    ///		"freshHours": { "avg": 1080 },
    ///		"transitionHours": { "avg": 180 },
    ///		"transitionRatio": 1,
    ///		"transitionedStack": {
    ///			"type": "item",
    ///			"code": "rot"
    ///		}
    ///	},
    ///	"shape": { "base": "block/food/meal/jam" },
    ///	"ingredients": [
    ///		{
    ///			"code": "honey",
    ///			"validStacks": [
    ///				{
    ///					"type": "item",
    ///					"code": "honeyportion",
    ///					"shapeElement": "bowl/honey",
    ///					"cookedStack": {
    ///						"type": "item",
    ///						"code": "jamhoneyportion"
    ///					}
    ///				}
    ///			],
    ///			"minQuantity": 2,
    ///			"maxQuantity": 2,
    ///			"portionSizeLitres": 0.2
    ///		},
    ///		{
    ///			"code": "fruit",
    ///			"validStacks": [
    ///				{
    ///					"type": "item",
    ///					"code": "fruit-*",
    ///					"shapeElement": "bowl/fruit"
    ///				}
    ///			],
    ///			"minQuantity": 2,
    ///			"maxQuantity": 2
    ///		}
    ///	]
    ///}
    /// </code>
    /// </example>
    [DocumentAsJson]
    public class CookingRecipe : IByteSerializable
    {
        /// <summary>
        /// <!--<jsonoptional>Required</jsonoptional>-->
        /// A unique code for the recipe and meal created.
        /// </summary>
        [DocumentAsJson] public string Code;

        /// <summary>
        /// <!--<jsonoptional>Required</jsonoptional>-->
        /// A list of ingredients for the recipe. Although cooking pots have a maximum of 4 unique entries, there is no limit on the number of potential ingredients.
        /// </summary>
        [DocumentAsJson] public CookingRecipeIngredient[] Ingredients;

        /// <summary>
        /// <!--<jsonoptional>Optional</jsonoptional><jsondefault>True</jsondefault>-->
        /// Should this recipe be loaded by the game?
        /// </summary>
        [DocumentAsJson] public bool Enabled = true;

        /// <summary>
        /// <!--<jsonoptional>Required</jsonoptional>-->
        /// A path to the shape file for this meal when inside a cooking pot. Specific ingredient-based elements can be enabled using the <see cref="CookingRecipeStack.ShapeElement"/> in the ingredient stacks.
        /// </summary>
        [DocumentAsJson] public CompositeShape Shape;

        /// <summary>
        /// <!--<jsonoptional>Required</jsonoptional>-->
        /// The transitionable properties for the meal item. Usually controls meal expiry.
        /// </summary>
        [DocumentAsJson] public TransitionableProperties PerishableProps;

        /// <summary>
        /// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
        /// If set, will treat the recipe not as a meal with its ingredients retained but convert the ingredients into supplied itemstack.
        /// </summary>
        [DocumentAsJson] public JsonItemStack CooksInto;

        public static Dictionary<string, ICookingRecipeNamingHelper> NamingRegistry = new Dictionary<string, ICookingRecipeNamingHelper>();

        static CookingRecipe()
        {
            NamingRegistry["porridge"] = new VanillaCookingRecipeNames();
            NamingRegistry["meatystew"] = new VanillaCookingRecipeNames();
            NamingRegistry["vegetablestew"] = new VanillaCookingRecipeNames();
            NamingRegistry["soup"] = new VanillaCookingRecipeNames();
            NamingRegistry["jam"] = new VanillaCookingRecipeNames();
            NamingRegistry["scrambledeggs"] = new VanillaCookingRecipeNames();
            NamingRegistry["glueportion-pitch-hot"] = new VanillaCookingRecipeNames();
            NamingRegistry["glueportion-pitch-cold"] = new VanillaCookingRecipeNames();
        }

        public bool Matches(ItemStack[] inputStacks)
        {
            int useless = 0;
            return Matches(inputStacks, ref useless);
        }

        public int GetQuantityServings(ItemStack[] stacks)
        {
            int quantity = 0;
            Matches(stacks, ref quantity);
            return quantity;
        }

        /// <summary>
        /// Gets the name of the output food if one exists.
        /// </summary>
        /// <param name="worldForResolve"></param>
        /// <param name="inputStacks"></param>
        /// <returns></returns>
        public string GetOutputName(IWorldAccessor worldForResolve, ItemStack[] inputStacks)
        {
            bool rotten = inputStacks.Any((stack) => stack?.Collectible.Code.Path == "rot");
            if (rotten)
            {
                return Lang.Get("Rotten Food");
            }


            ICookingRecipeNamingHelper namer = null;
            if (NamingRegistry.TryGetValue(Code, out namer))
            {
                return namer.GetNameForIngredients(worldForResolve, Code, inputStacks);
            }

            return Lang.Get("meal-"+Code);
        }



        public bool Matches(ItemStack[] inputStacks, ref int quantityServings)
        {
            List<ItemStack> inputStacksList = new List<ItemStack>(inputStacks);
            List<CookingRecipeIngredient> ingredientList = new List<CookingRecipeIngredient>(Ingredients);

            int totalOutputQuantity = 99999;

            int[] curQuantities = new int[ingredientList.Count];
            for (int i = 0; i < curQuantities.Length; i++) curQuantities[i] = 0;

            while (inputStacksList.Count > 0)
            {
                ItemStack inputStack = inputStacksList[0];
                inputStacksList.RemoveAt(0);
                if (inputStack == null) continue;

                bool found = false;
                for (int i = 0; i < ingredientList.Count; i++)
                {
                    CookingRecipeIngredient ingred = ingredientList[i];
                    
                    if (ingred.Matches(inputStack))
                    {
                        if (curQuantities[i] >= ingred.MaxQuantity) continue;
                        int stackPortion = inputStack.StackSize;

                        if (inputStack.Collectible.Attributes?["waterTightContainerProps"].Exists == true)
                        {
                            var props = BlockLiquidContainerBase.GetContainableProps(inputStack);
                            var temp = GetIngrendientFor(inputStack);
                            stackPortion = (int)(inputStack.StackSize / props.ItemsPerLitre / GetIngrendientFor(inputStack).PortionSizeLitres);
                        }

                        totalOutputQuantity = Math.Min(totalOutputQuantity, stackPortion);
                        curQuantities[i]++;
                        found = true;
                        break;
                    }
                }

                // This input stack does not fit in this cooking recipe
                if (!found) return false;
            }

            // Any required ingredients left?
            for (int i = 0; i < ingredientList.Count; i++)
            {
                if (curQuantities[i] < ingredientList[i].MinQuantity) return false;
            }

            quantityServings = totalOutputQuantity;

            // Too many ingredients?
            for (int i = 0; i < inputStacks.Length; i++)
            {
                var stack = inputStacks[i];
                if (stack == null) continue;

                if (stack.Collectible.Attributes?["waterTightContainerProps"].Exists == true)
                {
                    var props = BlockLiquidContainerBase.GetContainableProps(stack);
                    var temp = GetIngrendientFor(stack);
                    if (stack.StackSize != (int)(quantityServings * props.ItemsPerLitre * GetIngrendientFor(stack).PortionSizeLitres)) return false;
                }
                else if (stack.StackSize != quantityServings) return false;
            }

            return true;
        }
       

        public CookingRecipeIngredient GetIngrendientFor(ItemStack stack, params CookingRecipeIngredient[] ingredsToskip)
        {
            if (stack == null) return null;

            for (int i = 0; i < Ingredients.Length; i++)
            {
                if (Ingredients[i].Matches(stack) && !ingredsToskip.Contains(Ingredients[i])) return Ingredients[i];
            }

            return null;
        }


        public void Resolve(IServerWorldAccessor world, string sourceForErrorLogging)
        {
            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i].Resolve(world, sourceForErrorLogging);
            }

            CooksInto?.Resolve(world, sourceForErrorLogging);
        }



        /// <summary>
        /// Serialized the alloy
        /// </summary>
        /// <param name="writer"></param>
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Code);
            writer.Write(Ingredients.Length);
            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i].ToBytes(writer);
            }

            writer.Write(Shape == null);
            if (Shape != null) writer.Write(Shape.Base.ToString());

            PerishableProps.ToBytes(writer);

            writer.Write(CooksInto != null);
            if (CooksInto != null) CooksInto.ToBytes(writer);
        }

        /// <summary>
        /// Deserializes the alloy
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="resolver"></param>
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Code = reader.ReadString();
            Ingredients = new CookingRecipeIngredient[reader.ReadInt32()];

            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i] = new CookingRecipeIngredient();
                Ingredients[i].FromBytes(reader, resolver.ClassRegistry);
                Ingredients[i].Resolve(resolver, "[FromBytes]");
            }

            if (!reader.ReadBoolean())
            {
                Shape = new CompositeShape() { Base = new AssetLocation(reader.ReadString()) };
            }

            PerishableProps = new TransitionableProperties();
            PerishableProps.FromBytes(reader, resolver.ClassRegistry);

            if (reader.ReadBoolean())
            {
                CooksInto = new JsonItemStack();
                CooksInto.FromBytes(reader, resolver.ClassRegistry);
                CooksInto.Resolve(resolver, "[FromBytes]");
            }
        }

    }
    
}
