﻿namespace Cocoon.ORM.ModelGen
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.ConnectionStringTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TablesListBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ConnectButton = new System.Windows.Forms.Button();
            this.ClassTextBox = new FastColoredTextBoxNS.FastColoredTextBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ClassTextBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ConnectionStringTextBox
            // 
            this.ConnectionStringTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ConnectionStringTextBox.Location = new System.Drawing.Point(12, 35);
            this.ConnectionStringTextBox.Name = "ConnectionStringTextBox";
            this.ConnectionStringTextBox.Size = new System.Drawing.Size(679, 25);
            this.ConnectionStringTextBox.TabIndex = 0;
            this.ConnectionStringTextBox.Text = "Server=172.99.97.188,4120;Database=424828_TempTimeSystem;User ID=424828_TempTimeS" +
    "ystem;Password=XraHFY639JH";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 19);
            this.label1.TabIndex = 1;
            this.label1.Text = "Connection String";
            // 
            // TablesListBox
            // 
            this.TablesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TablesListBox.FormattingEnabled = true;
            this.TablesListBox.ItemHeight = 17;
            this.TablesListBox.Location = new System.Drawing.Point(11, 92);
            this.TablesListBox.Name = "TablesListBox";
            this.TablesListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.TablesListBox.Size = new System.Drawing.Size(760, 157);
            this.TablesListBox.TabIndex = 2;
            this.TablesListBox.SelectedIndexChanged += new System.EventHandler(this.TablesListBox_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Choose Table";
            // 
            // ConnectButton
            // 
            this.ConnectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ConnectButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ConnectButton.Location = new System.Drawing.Point(697, 35);
            this.ConnectButton.Name = "ConnectButton";
            this.ConnectButton.Size = new System.Drawing.Size(75, 25);
            this.ConnectButton.TabIndex = 4;
            this.ConnectButton.Text = "Connect";
            this.ConnectButton.UseVisualStyleBackColor = true;
            this.ConnectButton.Click += new System.EventHandler(this.ConnectButton_Click);
            // 
            // ClassTextBox
            // 
            this.ClassTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ClassTextBox.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.ClassTextBox.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n^\\s*(case|default)\\s*[^:]" +
    "*(?<range>:)\\s*(?<range>[^;]+);\r\n";
            this.ClassTextBox.AutoScrollMinSize = new System.Drawing.Size(27, 15);
            this.ClassTextBox.BackBrush = null;
            this.ClassTextBox.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.ClassTextBox.CharHeight = 15;
            this.ClassTextBox.CharWidth = 8;
            this.ClassTextBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ClassTextBox.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.ClassTextBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.ClassTextBox.IsReplaceMode = false;
            this.ClassTextBox.Language = FastColoredTextBoxNS.Language.CSharp;
            this.ClassTextBox.LeftBracket = '(';
            this.ClassTextBox.LeftBracket2 = '{';
            this.ClassTextBox.Location = new System.Drawing.Point(12, 282);
            this.ClassTextBox.Name = "ClassTextBox";
            this.ClassTextBox.Paddings = new System.Windows.Forms.Padding(0);
            this.ClassTextBox.RightBracket = ')';
            this.ClassTextBox.RightBracket2 = '}';
            this.ClassTextBox.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.ClassTextBox.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("ClassTextBox.ServiceColors")));
            this.ClassTextBox.Size = new System.Drawing.Size(760, 267);
            this.ClassTextBox.TabIndex = 5;
            this.ClassTextBox.Zoom = 100;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 260);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(108, 19);
            this.label3.TabIndex = 6;
            this.label3.Text = "Generated Class";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ClassTextBox);
            this.Controls.Add(this.ConnectButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.TablesListBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ConnectionStringTextBox);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cocoon ORM Model Generator";
            ((System.ComponentModel.ISupportInitialize)(this.ClassTextBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ConnectionStringTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox TablesListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button ConnectButton;
        private FastColoredTextBoxNS.FastColoredTextBox ClassTextBox;
        private System.Windows.Forms.Label label3;
    }
}
