namespace Torque2dMitToPhaserConverter
{
    partial class FormTorque2dToPhaserConverter
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
            this.labelTorque2dProject = new System.Windows.Forms.Label();
            this.textBoxTorque2dProjectModulesFolder = new System.Windows.Forms.TextBox();
            this.buttonTorque2dProjectBrowse = new System.Windows.Forms.Button();
            this.buttonPhaserProjectOutputFolderBrowse = new System.Windows.Forms.Button();
            this.textBoxPhaserProjectOutputFolder = new System.Windows.Forms.TextBox();
            this.labelPhaserProject = new System.Windows.Forms.Label();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.folderBrowserDialogTorque2dProjectModulesFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.folderBrowserDialogPhaserProjectOutputFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.labelAppCoreVersion = new System.Windows.Forms.Label();
            this.labelModuleVersion = new System.Windows.Forms.Label();
            this.textBoxAppCoreVersion = new System.Windows.Forms.TextBox();
            this.textBoxModuleVersion = new System.Windows.Forms.TextBox();
            this.buttonQuit = new System.Windows.Forms.Button();
            this.textBoxCameraSizeWidth = new System.Windows.Forms.TextBox();
            this.textBoxCameraSizeHeight = new System.Windows.Forms.TextBox();
            this.labelCameraSizeTitle = new System.Windows.Forms.Label();
            this.labelCameraSizeWidth = new System.Windows.Forms.Label();
            this.labelCameraSizeHeight = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelTorque2dProject
            // 
            this.labelTorque2dProject.AutoSize = true;
            this.labelTorque2dProject.Location = new System.Drawing.Point(30, 32);
            this.labelTorque2dProject.Name = "labelTorque2dProject";
            this.labelTorque2dProject.Size = new System.Drawing.Size(319, 17);
            this.labelTorque2dProject.TabIndex = 0;
            this.labelTorque2dProject.Text = "Torque2D MIT project (source) - \'modules\' folder:";
            // 
            // textBoxTorque2dProjectModulesFolder
            // 
            this.textBoxTorque2dProjectModulesFolder.Location = new System.Drawing.Point(33, 66);
            this.textBoxTorque2dProjectModulesFolder.Name = "textBoxTorque2dProjectModulesFolder";
            this.textBoxTorque2dProjectModulesFolder.Size = new System.Drawing.Size(257, 22);
            this.textBoxTorque2dProjectModulesFolder.TabIndex = 1;
            // 
            // buttonTorque2dProjectBrowse
            // 
            this.buttonTorque2dProjectBrowse.Location = new System.Drawing.Point(296, 59);
            this.buttonTorque2dProjectBrowse.Name = "buttonTorque2dProjectBrowse";
            this.buttonTorque2dProjectBrowse.Size = new System.Drawing.Size(96, 36);
            this.buttonTorque2dProjectBrowse.TabIndex = 2;
            this.buttonTorque2dProjectBrowse.Text = "Browse...";
            this.buttonTorque2dProjectBrowse.UseVisualStyleBackColor = true;
            this.buttonTorque2dProjectBrowse.Click += new System.EventHandler(this.buttonTorque2dProjectBrowse_Click);
            // 
            // buttonPhaserProjectOutputFolderBrowse
            // 
            this.buttonPhaserProjectOutputFolderBrowse.Location = new System.Drawing.Point(296, 158);
            this.buttonPhaserProjectOutputFolderBrowse.Name = "buttonPhaserProjectOutputFolderBrowse";
            this.buttonPhaserProjectOutputFolderBrowse.Size = new System.Drawing.Size(96, 36);
            this.buttonPhaserProjectOutputFolderBrowse.TabIndex = 5;
            this.buttonPhaserProjectOutputFolderBrowse.Text = "Browse...";
            this.buttonPhaserProjectOutputFolderBrowse.UseVisualStyleBackColor = true;
            this.buttonPhaserProjectOutputFolderBrowse.Click += new System.EventHandler(this.buttonPhaserProjectOutputFolderBrowse_Click);
            // 
            // textBoxPhaserProjectOutputFolder
            // 
            this.textBoxPhaserProjectOutputFolder.Location = new System.Drawing.Point(33, 165);
            this.textBoxPhaserProjectOutputFolder.Name = "textBoxPhaserProjectOutputFolder";
            this.textBoxPhaserProjectOutputFolder.Size = new System.Drawing.Size(257, 22);
            this.textBoxPhaserProjectOutputFolder.TabIndex = 4;
            // 
            // labelPhaserProject
            // 
            this.labelPhaserProject.AutoSize = true;
            this.labelPhaserProject.Location = new System.Drawing.Point(30, 131);
            this.labelPhaserProject.Name = "labelPhaserProject";
            this.labelPhaserProject.Size = new System.Drawing.Size(280, 17);
            this.labelPhaserProject.TabIndex = 3;
            this.labelPhaserProject.Text = "Phaser project (destination) - output folder:";
            // 
            // buttonConvert
            // 
            this.buttonConvert.Location = new System.Drawing.Point(188, 322);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(173, 53);
            this.buttonConvert.TabIndex = 6;
            this.buttonConvert.Text = "CONVERT!";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // labelAppCoreVersion
            // 
            this.labelAppCoreVersion.AutoSize = true;
            this.labelAppCoreVersion.Location = new System.Drawing.Point(30, 210);
            this.labelAppCoreVersion.Name = "labelAppCoreVersion";
            this.labelAppCoreVersion.Size = new System.Drawing.Size(119, 17);
            this.labelAppCoreVersion.TabIndex = 7;
            this.labelAppCoreVersion.Text = "AppCore Version:";
            // 
            // labelModuleVersion
            // 
            this.labelModuleVersion.AutoSize = true;
            this.labelModuleVersion.Location = new System.Drawing.Point(30, 297);
            this.labelModuleVersion.Name = "labelModuleVersion";
            this.labelModuleVersion.Size = new System.Drawing.Size(110, 17);
            this.labelModuleVersion.TabIndex = 8;
            this.labelModuleVersion.Text = "Module Version:";
            // 
            // textBoxAppCoreVersion
            // 
            this.textBoxAppCoreVersion.Location = new System.Drawing.Point(33, 250);
            this.textBoxAppCoreVersion.Name = "textBoxAppCoreVersion";
            this.textBoxAppCoreVersion.Size = new System.Drawing.Size(100, 22);
            this.textBoxAppCoreVersion.TabIndex = 9;
            this.textBoxAppCoreVersion.Text = "1";
            // 
            // textBoxModuleVersion
            // 
            this.textBoxModuleVersion.Location = new System.Drawing.Point(33, 337);
            this.textBoxModuleVersion.Name = "textBoxModuleVersion";
            this.textBoxModuleVersion.Size = new System.Drawing.Size(100, 22);
            this.textBoxModuleVersion.TabIndex = 10;
            this.textBoxModuleVersion.Text = "1";
            // 
            // buttonQuit
            // 
            this.buttonQuit.Location = new System.Drawing.Point(97, 533);
            this.buttonQuit.Name = "buttonQuit";
            this.buttonQuit.Size = new System.Drawing.Size(213, 59);
            this.buttonQuit.TabIndex = 11;
            this.buttonQuit.Text = "Quit";
            this.buttonQuit.UseVisualStyleBackColor = true;
            this.buttonQuit.Click += new System.EventHandler(this.buttonQuit_Click);
            // 
            // textBoxCameraSizeWidth
            // 
            this.textBoxCameraSizeWidth.Location = new System.Drawing.Point(188, 261);
            this.textBoxCameraSizeWidth.Name = "textBoxCameraSizeWidth";
            this.textBoxCameraSizeWidth.Size = new System.Drawing.Size(102, 22);
            this.textBoxCameraSizeWidth.TabIndex = 12;
            this.textBoxCameraSizeWidth.Text = "100";
            // 
            // textBoxCameraSizeHeight
            // 
            this.textBoxCameraSizeHeight.Location = new System.Drawing.Point(296, 261);
            this.textBoxCameraSizeHeight.Name = "textBoxCameraSizeHeight";
            this.textBoxCameraSizeHeight.Size = new System.Drawing.Size(102, 22);
            this.textBoxCameraSizeHeight.TabIndex = 13;
            this.textBoxCameraSizeHeight.Text = "75";
            // 
            // labelCameraSizeTitle
            // 
            this.labelCameraSizeTitle.AutoSize = true;
            this.labelCameraSizeTitle.Location = new System.Drawing.Point(185, 210);
            this.labelCameraSizeTitle.Name = "labelCameraSizeTitle";
            this.labelCameraSizeTitle.Size = new System.Drawing.Size(202, 17);
            this.labelCameraSizeTitle.TabIndex = 14;
            this.labelCameraSizeTitle.Text = "Camera Size (sizeX and sizeY)";
            // 
            // labelCameraSizeWidth
            // 
            this.labelCameraSizeWidth.AutoSize = true;
            this.labelCameraSizeWidth.Location = new System.Drawing.Point(185, 233);
            this.labelCameraSizeWidth.Name = "labelCameraSizeWidth";
            this.labelCameraSizeWidth.Size = new System.Drawing.Size(44, 17);
            this.labelCameraSizeWidth.TabIndex = 15;
            this.labelCameraSizeWidth.Text = "Width";
            // 
            // labelCameraSizeHeight
            // 
            this.labelCameraSizeHeight.AutoSize = true;
            this.labelCameraSizeHeight.Location = new System.Drawing.Point(293, 233);
            this.labelCameraSizeHeight.Name = "labelCameraSizeHeight";
            this.labelCameraSizeHeight.Size = new System.Drawing.Size(49, 17);
            this.labelCameraSizeHeight.TabIndex = 16;
            this.labelCameraSizeHeight.Text = "Height";
            // 
            // FormTorque2dToPhaserConverter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 620);
            this.Controls.Add(this.labelCameraSizeHeight);
            this.Controls.Add(this.labelCameraSizeWidth);
            this.Controls.Add(this.labelCameraSizeTitle);
            this.Controls.Add(this.textBoxCameraSizeHeight);
            this.Controls.Add(this.textBoxCameraSizeWidth);
            this.Controls.Add(this.buttonQuit);
            this.Controls.Add(this.textBoxModuleVersion);
            this.Controls.Add(this.textBoxAppCoreVersion);
            this.Controls.Add(this.labelModuleVersion);
            this.Controls.Add(this.labelAppCoreVersion);
            this.Controls.Add(this.buttonConvert);
            this.Controls.Add(this.buttonPhaserProjectOutputFolderBrowse);
            this.Controls.Add(this.textBoxPhaserProjectOutputFolder);
            this.Controls.Add(this.labelPhaserProject);
            this.Controls.Add(this.buttonTorque2dProjectBrowse);
            this.Controls.Add(this.textBoxTorque2dProjectModulesFolder);
            this.Controls.Add(this.labelTorque2dProject);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FormTorque2dToPhaserConverter";
            this.Text = "Torque2D MIT to Phaser Converter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelTorque2dProject;
        private System.Windows.Forms.TextBox textBoxTorque2dProjectModulesFolder;
        private System.Windows.Forms.Button buttonTorque2dProjectBrowse;
        private System.Windows.Forms.Button buttonPhaserProjectOutputFolderBrowse;
        private System.Windows.Forms.TextBox textBoxPhaserProjectOutputFolder;
        private System.Windows.Forms.Label labelPhaserProject;
        private System.Windows.Forms.Button buttonConvert;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogTorque2dProjectModulesFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogPhaserProjectOutputFolder;
        private System.Windows.Forms.Label labelAppCoreVersion;
        private System.Windows.Forms.Label labelModuleVersion;
        private System.Windows.Forms.TextBox textBoxAppCoreVersion;
        private System.Windows.Forms.TextBox textBoxModuleVersion;
        private System.Windows.Forms.Button buttonQuit;
        private System.Windows.Forms.TextBox textBoxCameraSizeWidth;
        private System.Windows.Forms.TextBox textBoxCameraSizeHeight;
        private System.Windows.Forms.Label labelCameraSizeTitle;
        private System.Windows.Forms.Label labelCameraSizeWidth;
        private System.Windows.Forms.Label labelCameraSizeHeight;
    }
}

