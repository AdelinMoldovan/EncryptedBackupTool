using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;



namespace RecoveryApplication
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
       // private string correctPassword = "1234";
        public LoginWindow()
        {
            InitializeComponent();
        }
        /*private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (Authenticate(username, password))
            {
                // Login successful
                this.DialogResult = true; // Close the window and return true
                this.Close();
            }
            else
            {
                // Login failed
                MessageBox.Show("Invalid username or password", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                UsernameTextBox.Clear();
                PasswordBox.Clear();
            }
        }

        private bool Authenticate(string username, string password)
        {
            using (SqliteConnection conn = new SqliteConnection("Data Source=backup_app.db;"))
            {
                conn.Open();
                string query = "SELECT * FROM users WHERE username=@username AND password=@password";
                SqliteCommand cmd = new SqliteCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                SqliteDataReader reader = cmd.ExecuteReader();
                return reader.HasRows;
            }
        }*/

        /* private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccessTextBox.Text == correctPassword)
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close(); // Close the login window
            }
            else
            {
                MessageBox.Show("Incorrect password, please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/
    }
}