using System;
using System.Configuration;

namespace EncodePassword
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the password to encode: ");
            string plainPassword = Console.ReadLine();

            string encodedPassword = EncodeBase64(plainPassword);
            Console.WriteLine($"Encoded Password: {encodedPassword}");

            Console.WriteLine("Do you want to save this encoded password to App.config? (y/n): ");
            string saveOption = Console.ReadLine();

            if (saveOption?.ToLower() == "y")
            {
                SaveToAppConfig("SmtpPassword", encodedPassword);
                Console.WriteLine("Password saved to App.config.");
            }
        }

        static string EncodeBase64(string plainValue)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainValue);
            return Convert.ToBase64String(plainTextBytes);
        }

        static void SaveToAppConfig(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}