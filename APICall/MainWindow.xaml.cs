using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using APICall.DataTypes;

namespace APICall
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private const string apiKey = "dea168977e484521b80c865e1f942cf1"; //token
        private List<RecipeDetails> myRecipes = new List<RecipeDetails>(); // rezeptiste
        private RecipeDetails currentRecipe;   // rezeptdetails

        public MainWindow()
        {
            InitializeComponent();
        }

       /// <summary>
       /// Rezepte fetchen wenn der button geklickt wird
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private async void FetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            string query = QueryTextBox.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Bitte geben Sie was ein");
                return;
            }

            try
            {
                string jsonResponse = await FetchDataFromApi(query);
                var response = JsonConvert.DeserializeObject<RecipeResponse>(jsonResponse);
                DataListBox.Items.Clear();
                foreach (var recipe in response.Results)
                {
                    DataListBox.Items.Add(recipe);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Beim Fetchen der Daten ist ein Fehler aufgetreten: {ex.Message}");
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn ein Rezept in der ListBox ausgewählt wird und
        /// ladet die Details des ausgewählten Rezepts 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataListBox.SelectedItem is Recipe selectedRecipe)
            {
                try
                {
                    RecipeDetails recipeDetails;
                    if (myRecipes.Any(r => r.Id == selectedRecipe.Id))
                    {
                        // Wenn das Rezept in Rezeptliste ist nehmen wir das
                        recipeDetails = myRecipes.First(r => r.Id == selectedRecipe.Id);
                    }
                    else
                    {
                        // Wenn nicht von api 
                        recipeDetails = await FetchRecipeDetails(selectedRecipe.Id);
                    }
                    DisplayRecipeDetails(recipeDetails);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching recipe details: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// Ruft Rezeptdaten von der Spoonacular API ab GET Request
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private async Task<string> FetchDataFromApi(string query)
        {
            string url = $"https://api.spoonacular.com/recipes/complexSearch?query={query}&apiKey={apiKey}&number=10";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// Ruft die genauen Detaials eines Rezeptes ab GET Requst
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<RecipeDetails> FetchRecipeDetails(int id)
        {
            string url = $"https://api.spoonacular.com/recipes/{id}/information?apiKey={apiKey}";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RecipeDetails>(jsonResponse);
        }
        /// <summary>
        /// Zeigt die Details an
        /// </summary>
        /// <param name="recipe"></param>
        private void DisplayRecipeDetails(RecipeDetails recipe)
        {
            currentRecipe = recipe;
            RecipeTitleTextBlock.Text = recipe.Title;

            // Zutaten anzeigen
            StringBuilder ingredientsBuilder = new StringBuilder();
            foreach (var ingredient in recipe.ExtendedIngredients)
            {
                ingredientsBuilder.AppendLine($"• {ingredient.Original}");
            }
            IngredientsTextBlock.Text = ingredientsBuilder.ToString();

            // HTML in normalen Text umwandeln
            string plainInstructions = ConvertHtmlToPlainText(recipe.Instructions);
            RecipeInstructionsTextBlock.Text = plainInstructions;

            if (!string.IsNullOrEmpty(recipe.Image))
            {
                RecipeImage.Source = new BitmapImage(new Uri(recipe.Image));
            }
            else
            {
                RecipeImage.Source = null;
            }

            // Prüfen, ob das Rezept bereits in der Liste ist
            bool isInList = myRecipes.Any(r => r.Id == recipe.Id);
            AddToListButton.Visibility = isInList ? Visibility.Collapsed : Visibility.Visible;
            RemoveFromListButton.Visibility = isInList ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// Rezept in die Rezeptliste hinzufügen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddToListButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentRecipe != null && !myRecipes.Any(r => r.Id == currentRecipe.Id))
            {
                myRecipes.Add(currentRecipe);
                MessageBox.Show("Rezept hinzugefügt");
                AddToListButton.Visibility = Visibility.Collapsed;
                RemoveFromListButton.Visibility = Visibility.Visible;
            }
        }
        /// <summary>
        /// Entfernen eines Rezeptes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFromListButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentRecipe != null)
            {
                myRecipes.RemoveAll(r => r.Id == currentRecipe.Id);
                MessageBox.Show("Rezept entfernt");
                AddToListButton.Visibility = Visibility.Visible;
                RemoveFromListButton.Visibility = Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Anzeigen des Rezeptes welches ich in der Liste angeklickt habe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyRecipesButton_Click(object sender, RoutedEventArgs e)
        {
            DataListBox.Items.Clear();
            foreach (var recipe in myRecipes)
            {
                DataListBox.Items.Add(new Recipe { Id = recipe.Id, Title = recipe.Title });
            }
        }
       
        /// <summary>
        /// Alle "Codezeichen entfernen/ersetzen damit nur normaler Text angezeigt wird"
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string ConvertHtmlToPlainText(string html)
        {
            // HTML-Entities dekodieren wegen den li ol ... tags
            string decoded = HttpUtility.HtmlDecode(html);

            // HTML-Tags entfernen
            string withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty);

            // Mehrfache Leerzeichen durch ein einzelnes Leerzeichen ersetzen
            string singleSpaced = Regex.Replace(withoutTags, @"\s+", " ");

            // Nummerierte Liste manuell formatieren
            string[] lines = singleSpaced.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"^\d+\."))
                {
                    lines[i] = $"\n{lines[i]}";
                }
            }

            return string.Join("\n", lines).Trim();
        }
    }

}