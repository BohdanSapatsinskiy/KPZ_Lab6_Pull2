﻿using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data;

namespace RecipeBookApp
{
    public partial class Form1 : Form
    {
        string connectionString = "Server=localhost;Database=recipebookdb;Uid=root;Pwd=;";
        private int loggedInUserId;
        private Button activeCategoryButton = null;

        public Form1(int userId)
        {
            InitializeComponent();
            loggedInUserId = userId;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    MessageBox.Show("Успішне з'єднання з базою даних.");
                    LoadRecipes();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка з'єднання: {ex.Message}");
                }
            }
        }

        private void LoadCategories()
        {
            pnlCategories.Controls.Clear();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT DISTINCT Category FROM Recipes";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        int buttonY = 70;
                        while (reader.Read())
                        {
                            string category = reader["Category"].ToString();
                            Button btnCategory = new Button
                            {
                                Text = category,
                                BackColor = System.Drawing.Color.LightGray,
                                FlatStyle = FlatStyle.Flat,
                                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                                Location = new System.Drawing.Point(3, buttonY),
                                Size = new System.Drawing.Size(194, 50)
                            };
                            btnCategory.Click += (s, e) => {
                                LoadRecipes(category);
                                HighlightActiveButton(btnCategory);
                            };
                            pnlCategories.Controls.Add(btnCategory);
                            buttonY += 60;
                        }
                    }
                }
            }

            // всі категорії
            Button btnAllCategories = new Button
            {
                Text = "Усі категорії",
                BackColor = System.Drawing.Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(3, 10),
                Size = new System.Drawing.Size(194, 50)
            };
            btnAllCategories.Click += (s, e) => {
                LoadRecipes();
                HighlightActiveButton(btnAllCategories);
            };
            pnlCategories.Controls.Add(btnAllCategories);
        }

        private void LoadRecipes(string category = null)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name AS 'Назва рецепту', Category AS 'Категорія', CookingTime AS 'Час приготування', TotalCalories AS 'Калорії' FROM Recipes";
                if (category != null)
                {
                    query += " WHERE Category = @category";
                }
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                {
                    if (category != null)
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@category", category);
                    }
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dgvRecipes.DataSource = dataTable;
                    dgvRecipes.Columns["Id"].Visible = false;
                }
            }
        }

        private void dgvRecipes_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int recipeId = Convert.ToInt32(dgvRecipes.Rows[e.RowIndex].Cells["Id"].Value);
                RecipeDetailsForm detailsForm = new RecipeDetailsForm(recipeId);
                detailsForm.StartPosition = FormStartPosition.CenterScreen;
                detailsForm.ShowDialog();
            }
        }

      

        private void HighlightActiveButton(Button activeButton)
        {
            if (activeCategoryButton != null)
            {
                activeCategoryButton.BackColor = System.Drawing.Color.LightGray;
            }

            activeCategoryButton = activeButton;
            activeCategoryButton.BackColor = System.Drawing.Color.LightBlue;
        }


        private void dgvRecipes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnEditRecipe_Click(object sender, EventArgs e)
        {
                if (dgvRecipes.SelectedRows.Count > 0)
                {
                    int recipeId = Convert.ToInt32(dgvRecipes.SelectedRows[0].Cells["Id"].Value);
                    EditRecipeForm editRecipeForm = new EditRecipeForm(recipeId);
                    editRecipeForm.StartPosition = FormStartPosition.CenterScreen;
                    editRecipeForm.ShowDialog();
                    LoadRecipes();
                }
                else
                {
                    MessageBox.Show("Будь ласка, виберіть рецепт для редагування.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        private void DeleteRecipe(int recipeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Спочатку видаляємо записи з таблиці recipeingredients
                    string deleteIngredientsQuery = "DELETE FROM recipeingredients WHERE RecipeId = @id";
                    using (MySqlCommand deleteIngredientsCommand = new MySqlCommand(deleteIngredientsQuery, connection))
                    {
                        deleteIngredientsCommand.Parameters.AddWithValue("@id", recipeId);
                        deleteIngredientsCommand.ExecuteNonQuery();
                    }

                    // Потім видаляємо записи з таблиці favoriterecipes
                    string deleteFavoritesQuery = "DELETE FROM favoriterecipes WHERE RecipeId = @id";
                    using (MySqlCommand deleteFavoritesCommand = new MySqlCommand(deleteFavoritesQuery, connection))
                    {
                        deleteFavoritesCommand.Parameters.AddWithValue("@id", recipeId);
                        deleteFavoritesCommand.ExecuteNonQuery();
                    }

                    // Потім видаляємо запис з таблиці Recipes
                    string deleteRecipeQuery = "DELETE FROM Recipes WHERE Id = @id";
                    using (MySqlCommand deleteRecipeCommand = new MySqlCommand(deleteRecipeQuery, connection))
                    {
                        deleteRecipeCommand.Parameters.AddWithValue("@id", recipeId);
                        deleteRecipeCommand.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Рецепт успішно видалено!", "Підтвердження", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Виникла помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteRecipe_Click(object sender, EventArgs e)
        {
            if (dgvRecipes.SelectedRows.Count > 0)
            {
                int recipeId = Convert.ToInt32(dgvRecipes.SelectedRows[0].Cells["Id"].Value);
                DeleteRecipe(recipeId);
                LoadRecipes();
                LoadCategories();
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть рецепт для видалення.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddRecipe_Click(object sender, EventArgs e)
        {
            AddRecipeForm addRecipeForm = new AddRecipeForm();
            addRecipeForm.StartPosition = FormStartPosition.CenterScreen;
            addRecipeForm.ShowDialog();
            LoadRecipes();
            LoadCategories();
        }

        private void btnSearchRecipe_Click(object sender, EventArgs e)
        {
            SearchRecipeForm searchRecipeForm = new SearchRecipeForm();
            searchRecipeForm.StartPosition = FormStartPosition.CenterScreen;
            searchRecipeForm.ShowDialog();
        }
        private void LoadFavoriteRecipes()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT r.Id, r.Name AS 'Назва рецепту', r.Category AS 'Категорія', r.CookingTime AS 'Час приготування', r.TotalCalories AS 'Калорії'
                    FROM Recipes r
                    JOIN FavoriteRecipes f ON r.Id = f.RecipeId
                    WHERE f.UserId = @userId";
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@userId", loggedInUserId);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dgvRecipes.DataSource = dataTable;
                    dgvRecipes.Columns["Id"].Visible = false;
                }
            }
        }
        private void btnShowFavorites_Click(object sender, EventArgs e)
        {
                LoadFavoriteRecipes();
        }


        private void AddToFavorites(int recipeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // додаємо рецепт до улюблених
                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM FavoriteRecipes 
                WHERE UserId = @userId AND RecipeId = @recipeId";

                    using (MySqlCommand checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@userId", loggedInUserId);
                        checkCommand.Parameters.AddWithValue("@recipeId", recipeId);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Цей рецепт вже додано до улюблених!", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    //додаємо до таблці улюблених
                    string query = @"
                INSERT INTO FavoriteRecipes (UserId, RecipeId)
                VALUES (@userId, @recipeId)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", loggedInUserId);
                        command.Parameters.AddWithValue("@recipeId", recipeId);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Рецепт успішно додано до улюблених!", "Підтвердження", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Виникла помилка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnAddToFavorites_Click(object sender, EventArgs e)
        {
            if (dgvRecipes.SelectedRows.Count > 0 && loggedInUserId > 0)
            {
                int recipeId = Convert.ToInt32(dgvRecipes.SelectedRows[0].Cells["Id"].Value);
                AddToFavorites(recipeId);
            }
        }
    }
}
