namespace Intune_Content_Prep
{
    partial class ContentPrep
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            souce_label = new Label();
            source_textbox = new TextBox();
            destination_label = new Label();
            destination_textbox = new TextBox();
            open_source_file_dialog = new OpenFileDialog();
            open_winapputil_dialog = new OpenFileDialog();
            browse_for_source_button = new Button();
            browse_for_destination_button = new Button();
            overwrite_destination_checkbox = new CheckBox();
            intunewinapp_label = new Label();
            intunewinapp_textbox = new TextBox();
            browse_for_intunewinapp_button = new Button();
            browse_for_destination_dialog = new FolderBrowserDialog();
            SuspendLayout();
            // 
            // souce_label
            // 
            souce_label.AutoSize = true;
            souce_label.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            souce_label.Location = new Point(21, 42);
            souce_label.Name = "souce_label";
            souce_label.Size = new Size(74, 25);
            souce_label.TabIndex = 0;
            souce_label.Text = "Source:";
            // 
            // source_textbox
            // 
            source_textbox.AllowDrop = true;
            source_textbox.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            source_textbox.Location = new Point(21, 69);
            source_textbox.Name = "source_textbox";
            source_textbox.Size = new Size(700, 35);
            source_textbox.TabIndex = 1;
            source_textbox.WordWrap = false;
            // 
            // destination_label
            // 
            destination_label.AutoSize = true;
            destination_label.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            destination_label.Location = new Point(21, 118);
            destination_label.Name = "destination_label";
            destination_label.Size = new Size(124, 30);
            destination_label.TabIndex = 0;
            destination_label.Text = "Destination:";
            // 
            // destination_textbox
            // 
            destination_textbox.AllowDrop = true;
            destination_textbox.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            destination_textbox.Location = new Point(21, 151);
            destination_textbox.Name = "destination_textbox";
            destination_textbox.Size = new Size(700, 35);
            destination_textbox.TabIndex = 3;
            destination_textbox.WordWrap = false;
            // 
            // open_source_file_dialog
            // 
            open_source_file_dialog.DefaultExt = "exe";
            // 
            // browse_for_source_button
            // 
            browse_for_source_button.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            browse_for_source_button.Location = new Point(727, 70);
            browse_for_source_button.Name = "browse_for_source_button";
            browse_for_source_button.Size = new Size(54, 35);
            browse_for_source_button.TabIndex = 2;
            browse_for_source_button.Text = "...";
            browse_for_source_button.UseVisualStyleBackColor = true;
            // 
            // browse_for_destination_button
            // 
            browse_for_destination_button.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            browse_for_destination_button.Location = new Point(730, 151);
            browse_for_destination_button.Name = "browse_for_destination_button";
            browse_for_destination_button.Size = new Size(51, 35);
            browse_for_destination_button.TabIndex = 4;
            browse_for_destination_button.Text = "...";
            browse_for_destination_button.UseVisualStyleBackColor = true;
            // 
            // overwrite_destination_checkbox
            // 
            overwrite_destination_checkbox.AutoSize = true;
            overwrite_destination_checkbox.Checked = true;
            overwrite_destination_checkbox.CheckState = CheckState.Checked;
            overwrite_destination_checkbox.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            overwrite_destination_checkbox.Location = new Point(580, 201);
            overwrite_destination_checkbox.Name = "overwrite_destination_checkbox";
            overwrite_destination_checkbox.RightToLeft = RightToLeft.Yes;
            overwrite_destination_checkbox.Size = new Size(181, 25);
            overwrite_destination_checkbox.TabIndex = 5;
            overwrite_destination_checkbox.Text = "Overwrite Destination";
            overwrite_destination_checkbox.UseVisualStyleBackColor = true;
            // 
            // intunewinapp_label
            // 
            intunewinapp_label.AutoSize = true;
            intunewinapp_label.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            intunewinapp_label.Location = new Point(21, 234);
            intunewinapp_label.Name = "intunewinapp_label";
            intunewinapp_label.Size = new Size(301, 30);
            intunewinapp_label.TabIndex = 0;
            intunewinapp_label.Text = "IntuneWinAppUtil.exe location:";
            // 
            // intunewinapp_textbox
            // 
            intunewinapp_textbox.AllowDrop = true;
            intunewinapp_textbox.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            intunewinapp_textbox.Location = new Point(21, 267);
            intunewinapp_textbox.Name = "intunewinapp_textbox";
            intunewinapp_textbox.Size = new Size(700, 35);
            intunewinapp_textbox.TabIndex = 6;
            intunewinapp_textbox.WordWrap = false;
            // 
            // browse_for_intunewinapp_button
            // 
            browse_for_intunewinapp_button.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            browse_for_intunewinapp_button.Location = new Point(730, 267);
            browse_for_intunewinapp_button.Name = "browse_for_intunewinapp_button";
            browse_for_intunewinapp_button.Size = new Size(51, 35);
            browse_for_intunewinapp_button.TabIndex = 7;
            browse_for_intunewinapp_button.Text = "...";
            browse_for_intunewinapp_button.UseVisualStyleBackColor = true;
            // 
            // ContentPrep
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 690);
            Controls.Add(browse_for_intunewinapp_button);
            Controls.Add(intunewinapp_textbox);
            Controls.Add(intunewinapp_label);
            Controls.Add(overwrite_destination_checkbox);
            Controls.Add(browse_for_destination_button);
            Controls.Add(browse_for_source_button);
            Controls.Add(destination_textbox);
            Controls.Add(destination_label);
            Controls.Add(source_textbox);
            Controls.Add(souce_label);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "ContentPrep";
            Text = "Intune Content Prep";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label souce_label;
        private TextBox source_textbox;
        private Label destination_label;
        private TextBox destination_textbox;
        private OpenFileDialog open_source_file_dialog;
        private OpenFileDialog open_winapputil_dialog;
        private Button browse_for_source_button;
        private Button browse_for_destination_button;
        private CheckBox overwrite_destination_checkbox;
        private Label intunewinapp_label;
        private TextBox intunewinapp_textbox;
        private Button browse_for_intunewinapp_button;
        private FolderBrowserDialog browse_for_destination_dialog;
    }
}
