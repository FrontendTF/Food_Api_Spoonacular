using System.Collections.Generic;

namespace APICall.DataTypes
{
    public class RecipeResponse
    {
        public int Offset { get; set; }
        public int Number { get; set; }
        public int TotalResults { get; set; }
        public List<Recipe> Results { get; set; }
    }

    public class Recipe
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

    public class RecipeDetails
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string Instructions { get; set; }
        public List<Ingredient> ExtendedIngredients { get; set; }
    }

    public class Ingredient
    {
        public string Original { get; set; }
    }
}