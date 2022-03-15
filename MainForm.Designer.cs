
namespace RoboRouter
{
    partial class MainForm
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
            if (disposing && (components != null)) {
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
            this.txt_directory = new System.Windows.Forms.TextBox();
            this.lbl_directory = new System.Windows.Forms.Label();
            this.btn_route = new System.Windows.Forms.Button();
            this.btn_fetchTimes = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.distinctResultEndTimesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logResultsToTextFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableResultSortingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restartsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.onlyRequiredRestartsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.maxRestartCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lbl_startName = new System.Windows.Forms.Label();
            this.lbl_finishName = new System.Windows.Forms.Label();
            this.txt_startName = new System.Windows.Forms.TextBox();
            this.txt_finishName = new System.Windows.Forms.TextBox();
            this.cbx_useTableInput = new System.Windows.Forms.CheckBox();
            this.txt_tableInput = new System.Windows.Forms.RichTextBox();
            this.lbl_tableInput = new System.Windows.Forms.Label();
            this.lbl_nameSeparators = new System.Windows.Forms.Label();
            this.txt_nameSeparators = new System.Windows.Forms.TextBox();
            this.btn_refresh = new System.Windows.Forms.Button();
            this.num_restartPenalty = new System.Windows.Forms.NumericUpDown();
            this.lbl_restartPenalty = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_restartPenalty)).BeginInit();
            this.SuspendLayout();
            // 
            // txt_directory
            // 
            this.txt_directory.Location = new System.Drawing.Point(151, 237);
            this.txt_directory.Name = "txt_directory";
            this.txt_directory.Size = new System.Drawing.Size(228, 23);
            this.txt_directory.TabIndex = 0;
            // 
            // lbl_directory
            // 
            this.lbl_directory.AutoSize = true;
            this.lbl_directory.Location = new System.Drawing.Point(150, 219);
            this.lbl_directory.Name = "lbl_directory";
            this.lbl_directory.Size = new System.Drawing.Size(98, 15);
            this.lbl_directory.TabIndex = 1;
            this.lbl_directory.Text = "Lobby TAS Folder";
            // 
            // btn_route
            // 
            this.btn_route.Location = new System.Drawing.Point(12, 36);
            this.btn_route.Name = "btn_route";
            this.btn_route.Size = new System.Drawing.Size(126, 57);
            this.btn_route.TabIndex = 2;
            this.btn_route.Text = "Run Router";
            this.btn_route.UseVisualStyleBackColor = true;
            this.btn_route.Click += new System.EventHandler(this.btn_route_Click);
            // 
            // btn_fetchTimes
            // 
            this.btn_fetchTimes.Location = new System.Drawing.Point(12, 99);
            this.btn_fetchTimes.Name = "btn_fetchTimes";
            this.btn_fetchTimes.Size = new System.Drawing.Size(126, 60);
            this.btn_fetchTimes.TabIndex = 3;
            this.btn_fetchTimes.Text = "Fetch File Times";
            this.btn_fetchTimes.UseVisualStyleBackColor = true;
            this.btn_fetchTimes.Click += new System.EventHandler(this.btn_fetchTimes_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem,
            this.outputToolStripMenuItem,
            this.restartsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(394, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
            // 
            // outputToolStripMenuItem
            // 
            this.outputToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.distinctResultEndTimesToolStripMenuItem,
            this.logResultsToTextFilesToolStripMenuItem,
            this.disableResultSortingToolStripMenuItem});
            this.outputToolStripMenuItem.Name = "outputToolStripMenuItem";
            this.outputToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.outputToolStripMenuItem.Text = "Output";
            // 
            // distinctResultEndTimesToolStripMenuItem
            // 
            this.distinctResultEndTimesToolStripMenuItem.Name = "distinctResultEndTimesToolStripMenuItem";
            this.distinctResultEndTimesToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.distinctResultEndTimesToolStripMenuItem.Text = "Distinct Result End Times";
            this.distinctResultEndTimesToolStripMenuItem.Click += new System.EventHandler(this.distinctResultEndTimesToolStripMenuItem_Click);
            // 
            // logResultsToTextFilesToolStripMenuItem
            // 
            this.logResultsToTextFilesToolStripMenuItem.Name = "logResultsToTextFilesToolStripMenuItem";
            this.logResultsToTextFilesToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.logResultsToTextFilesToolStripMenuItem.Text = "Log Results To Text Files";
            this.logResultsToTextFilesToolStripMenuItem.Click += new System.EventHandler(this.logResultsToTextFilesToolStripMenuItem_Click);
            // 
            // disableResultSortingToolStripMenuItem
            // 
            this.disableResultSortingToolStripMenuItem.Name = "disableResultSortingToolStripMenuItem";
            this.disableResultSortingToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.disableResultSortingToolStripMenuItem.Text = "Disable Result Sorting";
            this.disableResultSortingToolStripMenuItem.Click += new System.EventHandler(this.disableResultSortingToolStripMenuItem_Click);
            // 
            // restartsToolStripMenuItem
            // 
            this.restartsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.onlyRequiredRestartsToolStripMenuItem,
            this.maxRestartCountToolStripMenuItem});
            this.restartsToolStripMenuItem.Name = "restartsToolStripMenuItem";
            this.restartsToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.restartsToolStripMenuItem.Text = "Restarts";
            // 
            // onlyRequiredRestartsToolStripMenuItem
            // 
            this.onlyRequiredRestartsToolStripMenuItem.Name = "onlyRequiredRestartsToolStripMenuItem";
            this.onlyRequiredRestartsToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.onlyRequiredRestartsToolStripMenuItem.Text = "Only Dead End Restarts";
            this.onlyRequiredRestartsToolStripMenuItem.Click += new System.EventHandler(this.onlyRequiredRestartsToolStripMenuItem_Click);
            // 
            // maxRestartCountToolStripMenuItem
            // 
            this.maxRestartCountToolStripMenuItem.Name = "maxRestartCountToolStripMenuItem";
            this.maxRestartCountToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            this.maxRestartCountToolStripMenuItem.Text = "Max Restart Count";
            // 
            // lbl_startName
            // 
            this.lbl_startName.AutoSize = true;
            this.lbl_startName.Location = new System.Drawing.Point(13, 219);
            this.lbl_startName.Name = "lbl_startName";
            this.lbl_startName.Size = new System.Drawing.Size(66, 15);
            this.lbl_startName.TabIndex = 5;
            this.lbl_startName.Text = "Start Name";
            // 
            // lbl_finishName
            // 
            this.lbl_finishName.AutoSize = true;
            this.lbl_finishName.Location = new System.Drawing.Point(14, 270);
            this.lbl_finishName.Name = "lbl_finishName";
            this.lbl_finishName.Size = new System.Drawing.Size(73, 15);
            this.lbl_finishName.TabIndex = 6;
            this.lbl_finishName.Text = "Finish Name";
            // 
            // txt_startName
            // 
            this.txt_startName.Location = new System.Drawing.Point(14, 237);
            this.txt_startName.Name = "txt_startName";
            this.txt_startName.ReadOnly = true;
            this.txt_startName.Size = new System.Drawing.Size(124, 23);
            this.txt_startName.TabIndex = 8;
            // 
            // txt_finishName
            // 
            this.txt_finishName.Location = new System.Drawing.Point(14, 288);
            this.txt_finishName.Name = "txt_finishName";
            this.txt_finishName.Size = new System.Drawing.Size(124, 23);
            this.txt_finishName.TabIndex = 9;
            // 
            // cbx_useTableInput
            // 
            this.cbx_useTableInput.AutoSize = true;
            this.cbx_useTableInput.Location = new System.Drawing.Point(150, 35);
            this.cbx_useTableInput.Name = "cbx_useTableInput";
            this.cbx_useTableInput.Size = new System.Drawing.Size(106, 19);
            this.cbx_useTableInput.TabIndex = 11;
            this.cbx_useTableInput.Text = "Use Table Input";
            this.cbx_useTableInput.UseVisualStyleBackColor = true;
            this.cbx_useTableInput.CheckedChanged += new System.EventHandler(this.cbx_useTableInput_CheckedChanged);
            // 
            // txt_tableInput
            // 
            this.txt_tableInput.Location = new System.Drawing.Point(150, 82);
            this.txt_tableInput.Name = "txt_tableInput";
            this.txt_tableInput.Size = new System.Drawing.Size(231, 126);
            this.txt_tableInput.TabIndex = 12;
            this.txt_tableInput.Text = "";
            this.txt_tableInput.WordWrap = false;
            // 
            // lbl_tableInput
            // 
            this.lbl_tableInput.AutoSize = true;
            this.lbl_tableInput.Location = new System.Drawing.Point(147, 59);
            this.lbl_tableInput.Name = "lbl_tableInput";
            this.lbl_tableInput.Size = new System.Drawing.Size(197, 15);
            this.lbl_tableInput.TabIndex = 13;
            this.lbl_tableInput.Text = "Table Input (Secondary use method)";
            // 
            // lbl_nameSeparators
            // 
            this.lbl_nameSeparators.AutoSize = true;
            this.lbl_nameSeparators.Location = new System.Drawing.Point(14, 167);
            this.lbl_nameSeparators.Name = "lbl_nameSeparators";
            this.lbl_nameSeparators.Size = new System.Drawing.Size(97, 15);
            this.lbl_nameSeparators.TabIndex = 14;
            this.lbl_nameSeparators.Text = "Name Separators";
            // 
            // txt_nameSeparators
            // 
            this.txt_nameSeparators.Location = new System.Drawing.Point(14, 185);
            this.txt_nameSeparators.Name = "txt_nameSeparators";
            this.txt_nameSeparators.Size = new System.Drawing.Size(124, 23);
            this.txt_nameSeparators.TabIndex = 15;
            // 
            // btn_refresh
            // 
            this.btn_refresh.Location = new System.Drawing.Point(150, 271);
            this.btn_refresh.Name = "btn_refresh";
            this.btn_refresh.Size = new System.Drawing.Size(111, 41);
            this.btn_refresh.TabIndex = 16;
            this.btn_refresh.Text = "Refresh File Parsing";
            this.btn_refresh.UseVisualStyleBackColor = true;
            this.btn_refresh.Click += new System.EventHandler(this.btn_refresh_Click);
            // 
            // num_restartPenalty
            // 
            this.num_restartPenalty.Location = new System.Drawing.Point(273, 288);
            this.num_restartPenalty.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.num_restartPenalty.Name = "num_restartPenalty";
            this.num_restartPenalty.Size = new System.Drawing.Size(106, 23);
            this.num_restartPenalty.TabIndex = 17;
            // 
            // lbl_restartPenalty
            // 
            this.lbl_restartPenalty.AutoSize = true;
            this.lbl_restartPenalty.Location = new System.Drawing.Point(273, 270);
            this.lbl_restartPenalty.Name = "lbl_restartPenalty";
            this.lbl_restartPenalty.Size = new System.Drawing.Size(85, 15);
            this.lbl_restartPenalty.TabIndex = 18;
            this.lbl_restartPenalty.Text = "Restart Penalty";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(394, 325);
            this.Controls.Add(this.lbl_restartPenalty);
            this.Controls.Add(this.num_restartPenalty);
            this.Controls.Add(this.btn_refresh);
            this.Controls.Add(this.txt_nameSeparators);
            this.Controls.Add(this.lbl_nameSeparators);
            this.Controls.Add(this.lbl_tableInput);
            this.Controls.Add(this.txt_tableInput);
            this.Controls.Add(this.cbx_useTableInput);
            this.Controls.Add(this.txt_finishName);
            this.Controls.Add(this.txt_startName);
            this.Controls.Add(this.lbl_finishName);
            this.Controls.Add(this.lbl_startName);
            this.Controls.Add(this.btn_fetchTimes);
            this.Controls.Add(this.btn_route);
            this.Controls.Add(this.lbl_directory);
            this.Controls.Add(this.txt_directory);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "RoboRouter";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.num_restartPenalty)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txt_directory;
        private System.Windows.Forms.Label lbl_directory;
        private System.Windows.Forms.Button btn_route;
        private System.Windows.Forms.Button btn_fetchTimes;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outputToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restartsToolStripMenuItem;
        private System.Windows.Forms.Label lbl_startName;
        private System.Windows.Forms.Label lbl_finishName;
        private System.Windows.Forms.TextBox txt_startName;
        private System.Windows.Forms.TextBox txt_finishName;
        private System.Windows.Forms.CheckBox cbx_useTableInput;
        private System.Windows.Forms.RichTextBox txt_tableInput;
        private System.Windows.Forms.Label lbl_tableInput;
        private System.Windows.Forms.Label lbl_nameSeparators;
        private System.Windows.Forms.TextBox txt_nameSeparators;
        private System.Windows.Forms.Button btn_refresh;
        private System.Windows.Forms.ToolStripMenuItem distinctResultEndTimesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logResultsToTextFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem onlyRequiredRestartsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem maxRestartCountToolStripMenuItem;
        private System.Windows.Forms.NumericUpDown num_restartPenalty;
        private System.Windows.Forms.Label lbl_restartPenalty;
        private System.Windows.Forms.ToolStripMenuItem disableResultSortingToolStripMenuItem;
    }
}
