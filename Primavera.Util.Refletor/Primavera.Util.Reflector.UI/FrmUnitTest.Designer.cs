namespace Primavera.Util.Reflector.UI
{
    partial class FrmUnitTest
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
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btProcess = new System.Windows.Forms.Button();
            this.TxtDestiny = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btGetFilePath = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtFilePath
            // 
            this.txtFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilePath.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtFilePath.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtFilePath.Location = new System.Drawing.Point(110, 12);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(269, 20);
            this.txtFilePath.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Source";
            // 
            // btProcess
            // 
            this.btProcess.BackColor = System.Drawing.Color.Transparent;
            this.btProcess.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btProcess.Location = new System.Drawing.Point(318, 67);
            this.btProcess.Name = "btProcess";
            this.btProcess.Size = new System.Drawing.Size(109, 40);
            this.btProcess.TabIndex = 7;
            this.btProcess.Text = "Create Test Files";
            this.btProcess.UseVisualStyleBackColor = false;
            this.btProcess.Click += new System.EventHandler(this.btProcess_Click);
            // 
            // TxtDestiny
            // 
            this.TxtDestiny.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtDestiny.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.TxtDestiny.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.TxtDestiny.Location = new System.Drawing.Point(113, 41);
            this.TxtDestiny.Name = "TxtDestiny";
            this.TxtDestiny.Size = new System.Drawing.Size(266, 20);
            this.TxtDestiny.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Destiny";
            // 
            // btGetFilePath
            // 
            this.btGetFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btGetFilePath.Location = new System.Drawing.Point(385, 12);
            this.btGetFilePath.Name = "btGetFilePath";
            this.btGetFilePath.Size = new System.Drawing.Size(29, 23);
            this.btGetFilePath.TabIndex = 10;
            this.btGetFilePath.Text = "...";
            this.btGetFilePath.UseVisualStyleBackColor = true;
            this.btGetFilePath.Click += new System.EventHandler(this.btGetFilePath_Click);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(385, 44);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(29, 23);
            this.button1.TabIndex = 11;

            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FrmUnitTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 119);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btGetFilePath);
            this.Controls.Add(this.TxtDestiny);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btProcess);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.label2);
            this.Name = "FrmUnitTest";
            this.Text = "FrmUnitTest";
            //this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btProcess;
        private System.Windows.Forms.TextBox TxtDestiny;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btGetFilePath;
        private System.Windows.Forms.Button button1;
    }
}