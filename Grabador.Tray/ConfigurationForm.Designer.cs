namespace Grabador.Tray
{
    partial class ConfigurationForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblWindowIdentifier = new System.Windows.Forms.Label();
            this.txtWindowIdentifier = new System.Windows.Forms.TextBox();
            this.lblRegexPattern = new System.Windows.Forms.Label();
            this.txtRegexPattern = new System.Windows.Forms.TextBox();
            this.lblUrlTemplate = new System.Windows.Forms.Label();
            this.txtUrlTemplate = new System.Windows.Forms.TextBox();
            this.lblJsonKey = new System.Windows.Forms.Label();
            this.txtJsonKey = new System.Windows.Forms.TextBox();
            this.lblHotkey = new System.Windows.Forms.Label();
            this.txtHotkey = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblWindowIdentifier
            //
            this.lblWindowIdentifier.AutoSize = true;
            this.lblWindowIdentifier.Location = new System.Drawing.Point(12, 15);
            this.lblWindowIdentifier.Name = "lblWindowIdentifier";
            this.lblWindowIdentifier.Size = new System.Drawing.Size(130, 13);
            this.lblWindowIdentifier.TabIndex = 0;
            this.lblWindowIdentifier.Text = "Window Identifier (Regex):";
            //
            // txtWindowIdentifier
            //
            this.txtWindowIdentifier.Location = new System.Drawing.Point(148, 12);
            this.txtWindowIdentifier.Name = "txtWindowIdentifier";
            this.txtWindowIdentifier.Size = new System.Drawing.Size(224, 20);
            this.txtWindowIdentifier.TabIndex = 1;
            //
            // lblRegexPattern
            //
            this.lblRegexPattern.AutoSize = true;
            this.lblRegexPattern.Location = new System.Drawing.Point(12, 41);
            this.lblRegexPattern.Name = "lblRegexPattern";
            this.lblRegexPattern.Size = new System.Drawing.Size(78, 13);
            this.lblRegexPattern.TabIndex = 2;
            this.lblRegexPattern.Text = "Regex Pattern:";
            //
            // txtRegexPattern
            //
            this.txtRegexPattern.Location = new System.Drawing.Point(148, 38);
            this.txtRegexPattern.Name = "txtRegexPattern";
            this.txtRegexPattern.Size = new System.Drawing.Size(224, 20);
            this.txtRegexPattern.TabIndex = 3;
            //
            // lblUrlTemplate
            //
            this.lblUrlTemplate.AutoSize = true;
            this.lblUrlTemplate.Location = new System.Drawing.Point(12, 67);
            this.lblUrlTemplate.Name = "lblUrlTemplate";
            this.lblUrlTemplate.Size = new System.Drawing.Size(107, 13);
            this.lblUrlTemplate.TabIndex = 4;
            this.lblUrlTemplate.Text = "URL Template (Opt.):";
            //
            // txtUrlTemplate
            //
            this.txtUrlTemplate.Location = new System.Drawing.Point(148, 64);
            this.txtUrlTemplate.Name = "txtUrlTemplate";
            this.txtUrlTemplate.Size = new System.Drawing.Size(224, 20);
            this.txtUrlTemplate.TabIndex = 5;
            //
            // lblJsonKey
            //
            this.lblJsonKey.AutoSize = true;
            this.lblJsonKey.Location = new System.Drawing.Point(12, 93);
            this.lblJsonKey.Name = "lblJsonKey";
            this.lblJsonKey.Size = new System.Drawing.Size(87, 13);
            this.lblJsonKey.TabIndex = 6;
            this.lblJsonKey.Text = "JSON Key (Opt.):";
            //
            // txtJsonKey
            //
            this.txtJsonKey.Location = new System.Drawing.Point(148, 90);
            this.txtJsonKey.Name = "txtJsonKey";
            this.txtJsonKey.Size = new System.Drawing.Size(224, 20);
            this.txtJsonKey.TabIndex = 7;
            //
            // lblHotkey
            //
            this.lblHotkey.AutoSize = true;
            this.lblHotkey.Location = new System.Drawing.Point(12, 119);
            this.lblHotkey.Name = "lblHotkey";
            this.lblHotkey.Size = new System.Drawing.Size(44, 13);
            this.lblHotkey.TabIndex = 8;
            this.lblHotkey.Text = "Hotkey:";
            //
            // txtHotkey
            //
            this.txtHotkey.Location = new System.Drawing.Point(148, 116);
            this.txtHotkey.Name = "txtHotkey";
            this.txtHotkey.Size = new System.Drawing.Size(224, 20);
            this.txtHotkey.TabIndex = 9;
            //
            // btnSave
            //
            this.btnSave.Location = new System.Drawing.Point(216, 150);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(297, 150);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // ConfigurationForm
            //
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 185);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtHotkey);
            this.Controls.Add(this.lblHotkey);
            this.Controls.Add(this.txtJsonKey);
            this.Controls.Add(this.lblJsonKey);
            this.Controls.Add(this.txtUrlTemplate);
            this.Controls.Add(this.lblUrlTemplate);
            this.Controls.Add(this.txtRegexPattern);
            this.Controls.Add(this.lblRegexPattern);
            this.Controls.Add(this.txtWindowIdentifier);
            this.Controls.Add(this.lblWindowIdentifier);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.Text = "Grabador Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblWindowIdentifier;
        private System.Windows.Forms.TextBox txtWindowIdentifier;
        private System.Windows.Forms.Label lblRegexPattern;
        private System.Windows.Forms.TextBox txtRegexPattern;
        private System.Windows.Forms.Label lblUrlTemplate;
        private System.Windows.Forms.TextBox txtUrlTemplate;
        private System.Windows.Forms.Label lblJsonKey;
        private System.Windows.Forms.TextBox txtJsonKey;
        private System.Windows.Forms.Label lblHotkey;
        private System.Windows.Forms.TextBox txtHotkey;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
