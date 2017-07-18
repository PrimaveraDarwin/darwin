namespace Primavera.Util.Reflector.UI
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tbpMain = new System.Windows.Forms.TabPage();
            this.grbSettings = new System.Windows.Forms.GroupBox();
            this.btGetFilePath = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.rdbFile = new System.Windows.Forms.RadioButton();
            this.rdbPath = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.tbpOutput = new System.Windows.Forms.TabPage();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.grbProcess = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btProcess = new System.Windows.Forms.Button();
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.txtFileFilter = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFileExtension = new System.Windows.Forms.TextBox();
            this.tabMain.SuspendLayout();
            this.tbpMain.SuspendLayout();
            this.grbSettings.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.grbProcess.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tbpMain);
            this.tabMain.Controls.Add(this.tbpOutput);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 60);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(755, 462);
            this.tabMain.TabIndex = 0;
            // 
            // tbpMain
            // 
            this.tbpMain.Controls.Add(this.grbProcess);
            this.tbpMain.Controls.Add(this.grbSettings);
            this.tbpMain.Location = new System.Drawing.Point(4, 22);
            this.tbpMain.Name = "tbpMain";
            this.tbpMain.Padding = new System.Windows.Forms.Padding(3);
            this.tbpMain.Size = new System.Drawing.Size(747, 436);
            this.tbpMain.TabIndex = 0;
            this.tbpMain.Text = "Main";
            this.tbpMain.UseVisualStyleBackColor = true;
            // 
            // grbSettings
            // 
            this.grbSettings.Controls.Add(this.txtFileExtension);
            this.grbSettings.Controls.Add(this.label4);
            this.grbSettings.Controls.Add(this.txtFileFilter);
            this.grbSettings.Controls.Add(this.lstFiles);
            this.grbSettings.Controls.Add(this.label3);
            this.grbSettings.Controls.Add(this.btGetFilePath);
            this.grbSettings.Controls.Add(this.txtFilePath);
            this.grbSettings.Controls.Add(this.label2);
            this.grbSettings.Controls.Add(this.rdbFile);
            this.grbSettings.Controls.Add(this.rdbPath);
            this.grbSettings.Controls.Add(this.label1);
            this.grbSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.grbSettings.Location = new System.Drawing.Point(3, 3);
            this.grbSettings.Name = "grbSettings";
            this.grbSettings.Size = new System.Drawing.Size(741, 207);
            this.grbSettings.TabIndex = 0;
            this.grbSettings.TabStop = false;
            this.grbSettings.Text = "Settings";
            // 
            // btGetFilePath
            // 
            this.btGetFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btGetFilePath.Location = new System.Drawing.Point(706, 52);
            this.btGetFilePath.Name = "btGetFilePath";
            this.btGetFilePath.Size = new System.Drawing.Size(29, 23);
            this.btGetFilePath.TabIndex = 5;
            this.btGetFilePath.Text = "...";
            this.btGetFilePath.UseVisualStyleBackColor = true;
            this.btGetFilePath.Click += new System.EventHandler(this.btGetFilePath_Click);
            // 
            // txtFilePath
            // 
            this.txtFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilePath.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtFilePath.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtFilePath.Location = new System.Drawing.Point(98, 54);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(602, 20);
            this.txtFilePath.TabIndex = 4;
            this.txtFilePath.TextChanged += new System.EventHandler(this.txtFilePath_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Source";
            // 
            // rdbFile
            // 
            this.rdbFile.AutoSize = true;
            this.rdbFile.Checked = true;
            this.rdbFile.Location = new System.Drawing.Point(151, 26);
            this.rdbFile.Name = "rdbFile";
            this.rdbFile.Size = new System.Drawing.Size(41, 17);
            this.rdbFile.TabIndex = 2;
            this.rdbFile.TabStop = true;
            this.rdbFile.Text = "File";
            this.rdbFile.UseVisualStyleBackColor = true;
            // 
            // rdbPath
            // 
            this.rdbPath.AutoSize = true;
            this.rdbPath.Location = new System.Drawing.Point(98, 26);
            this.rdbPath.Name = "rdbPath";
            this.rdbPath.Size = new System.Drawing.Size(47, 17);
            this.rdbPath.TabIndex = 1;
            this.rdbPath.Text = "Path";
            this.rdbPath.UseVisualStyleBackColor = true;
            this.rdbPath.CheckedChanged += new System.EventHandler(this.rdbPath_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Injection Type";
            // 
            // tbpOutput
            // 
            this.tbpOutput.Location = new System.Drawing.Point(4, 22);
            this.tbpOutput.Name = "tbpOutput";
            this.tbpOutput.Padding = new System.Windows.Forms.Padding(3);
            this.tbpOutput.Size = new System.Drawing.Size(747, 436);
            this.tbpOutput.TabIndex = 1;
            this.tbpOutput.Text = "Output";
            this.tbpOutput.UseVisualStyleBackColor = true;
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.SystemColors.Control;
            this.pnlHeader.Controls.Add(this.pictureBox1);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(755, 60);
            this.pnlHeader.TabIndex = 1;
            // 
            // grbProcess
            // 
            this.grbProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grbProcess.Controls.Add(this.btProcess);
            this.grbProcess.Location = new System.Drawing.Point(3, 210);
            this.grbProcess.Name = "grbProcess";
            this.grbProcess.Size = new System.Drawing.Size(741, 192);
            this.grbProcess.TabIndex = 1;
            this.grbProcess.TabStop = false;
            this.grbProcess.Text = "Process";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(549, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(203, 42);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Files to process";
            // 
            // btProcess
            // 
            this.btProcess.BackColor = System.Drawing.Color.Transparent;
            this.btProcess.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btProcess.Location = new System.Drawing.Point(98, 19);
            this.btProcess.Name = "btProcess";
            this.btProcess.Size = new System.Drawing.Size(75, 70);
            this.btProcess.TabIndex = 0;
            this.btProcess.Text = "Process";
            this.btProcess.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btProcess.UseVisualStyleBackColor = false;
            this.btProcess.Click += new System.EventHandler(this.btProcess_Click);
            // 
            // lstFiles
            // 
            this.lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstFiles.FormattingEnabled = true;
            this.lstFiles.Location = new System.Drawing.Point(98, 83);
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(602, 108);
            this.lstFiles.TabIndex = 7;
            // 
            // txtFileFilter
            // 
            this.txtFileFilter.AcceptsReturn = true;
            this.txtFileFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileFilter.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtFileFilter.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtFileFilter.Location = new System.Drawing.Point(583, 25);
            this.txtFileFilter.Name = "txtFileFilter";
            this.txtFileFilter.Size = new System.Drawing.Size(117, 20);
            this.txtFileFilter.TabIndex = 8;
            this.txtFileFilter.Text = "BS100";
            this.txtFileFilter.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(380, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "File Extension / Filter";
            // 
            // txtFileExtension
            // 
            this.txtFileExtension.AcceptsReturn = true;
            this.txtFileExtension.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileExtension.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtFileExtension.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtFileExtension.Location = new System.Drawing.Point(501, 25);
            this.txtFileExtension.Name = "txtFileExtension";
            this.txtFileExtension.Size = new System.Drawing.Size(76, 20);
            this.txtFileExtension.TabIndex = 10;
            this.txtFileExtension.Text = "*.dll";
            this.txtFileExtension.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(755, 522);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlHeader);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Primavera Code Injector (Darwin)";
            this.tabMain.ResumeLayout(false);
            this.tbpMain.ResumeLayout(false);
            this.grbSettings.ResumeLayout(false);
            this.grbSettings.PerformLayout();
            this.pnlHeader.ResumeLayout(false);
            this.grbProcess.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tbpMain;
        private System.Windows.Forms.GroupBox grbSettings;
        private System.Windows.Forms.TabPage tbpOutput;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton rdbFile;
        private System.Windows.Forms.RadioButton rdbPath;
        private System.Windows.Forms.Label label1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button btGetFilePath;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.GroupBox grbProcess;
        private System.Windows.Forms.Button btProcess;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtFileFilter;
        private System.Windows.Forms.ListBox lstFiles;
        private System.Windows.Forms.TextBox txtFileExtension;
    }
}