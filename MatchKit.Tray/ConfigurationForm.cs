using MatchKit.Core;
using System;
using System.Windows.Forms;

namespace MatchKit.Tray
{
    public partial class ConfigurationForm : Form
    {
        public ConfigurationForm()
        {
            InitializeComponent();
            LoadExistingConfiguration();
        }

        private void LoadExistingConfiguration()
        {
            ConfigData config = ConfigurationService.LoadConfiguration();
            if (config != null)
            {
                txtWindowIdentifier.Text = config.WindowIdentifier;
                txtRegexPattern.Text = config.RegexPattern;
                txtUrlTemplate.Text = config.UrlTemplate;
                txtJsonKey.Text = config.JsonKey;
                txtHotkey.Text = config.Hotkey;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtWindowIdentifier.Text) ||
                string.IsNullOrWhiteSpace(txtRegexPattern.Text) ||
                string.IsNullOrWhiteSpace(txtHotkey.Text))
            {
                MessageBox.Show("Window Identifier, Regex Pattern, and Hotkey are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Validate hotkey format before saving
                HotkeyParser.Parse(txtHotkey.Text);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Invalid Hotkey format: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ConfigData config = new ConfigData
            {
                WindowIdentifier = txtWindowIdentifier.Text,
                RegexPattern = txtRegexPattern.Text,
                UrlTemplate = txtUrlTemplate.Text,
                JsonKey = txtJsonKey.Text,
                Hotkey = txtHotkey.Text
            };

            try
            {
                ConfigurationService.SaveConfiguration(config);
                MessageBox.Show("Configuration saved successfully! The application will now close.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}\nPlease ensure the application has administrator privileges.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
