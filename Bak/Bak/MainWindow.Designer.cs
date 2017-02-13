namespace Bak
{
    partial class MainWindow
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
            this.panel = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.b_traversable = new System.Windows.Forms.Button();
            this.b_nontraversable = new System.Windows.Forms.Button();
            this.p_editingMapModes = new System.Windows.Forms.Panel();
            this.b_endNode = new System.Windows.Forms.Button();
            this.b_startNode = new System.Windows.Forms.Button();
            this.l_mapEditing = new System.Windows.Forms.Label();
            this.b_startPathFinding = new System.Windows.Forms.Button();
            this.c_selectedPathfinding = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tb_pathOutput = new System.Windows.Forms.TextBox();
            this.visitedLed = new System.Windows.Forms.TextBox();
            this.pathLed = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.b_mapRefresh = new System.Windows.Forms.Button();
            this.l_pathLength = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tb_elapsedTime = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.p_editingMapModes.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel
            // 
            this.panel.BackColor = System.Drawing.Color.White;
            this.panel.Location = new System.Drawing.Point(1, 40);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(600, 600);
            this.panel.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(22, 22);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1442, 29);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadMapToolStripMenuItem,
            this.saveMapToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 25);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loadMapToolStripMenuItem
            // 
            this.loadMapToolStripMenuItem.Name = "loadMapToolStripMenuItem";
            this.loadMapToolStripMenuItem.Size = new System.Drawing.Size(158, 28);
            this.loadMapToolStripMenuItem.Text = "Load map";
            this.loadMapToolStripMenuItem.Click += new System.EventHandler(this.loadMapToolStripMenuItem_Click);
            // 
            // saveMapToolStripMenuItem
            // 
            this.saveMapToolStripMenuItem.Name = "saveMapToolStripMenuItem";
            this.saveMapToolStripMenuItem.Size = new System.Drawing.Size(158, 28);
            this.saveMapToolStripMenuItem.Text = "Save map";
            this.saveMapToolStripMenuItem.Click += new System.EventHandler(this.saveMapToolStripMenuItem_Click);
            // 
            // b_traversable
            // 
            this.b_traversable.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_traversable.Location = new System.Drawing.Point(19, 37);
            this.b_traversable.Name = "b_traversable";
            this.b_traversable.Size = new System.Drawing.Size(128, 35);
            this.b_traversable.TabIndex = 2;
            this.b_traversable.Tag = "traversable";
            this.b_traversable.Text = "Traversable";
            this.b_traversable.UseVisualStyleBackColor = true;
            this.b_traversable.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // b_nontraversable
            // 
            this.b_nontraversable.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_nontraversable.Location = new System.Drawing.Point(19, 95);
            this.b_nontraversable.Name = "b_nontraversable";
            this.b_nontraversable.Size = new System.Drawing.Size(128, 35);
            this.b_nontraversable.TabIndex = 3;
            this.b_nontraversable.Tag = "!traversable";
            this.b_nontraversable.Text = "Non-Traversable";
            this.b_nontraversable.UseVisualStyleBackColor = true;
            this.b_nontraversable.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // p_editingMapModes
            // 
            this.p_editingMapModes.Controls.Add(this.b_endNode);
            this.p_editingMapModes.Controls.Add(this.b_startNode);
            this.p_editingMapModes.Controls.Add(this.l_mapEditing);
            this.p_editingMapModes.Controls.Add(this.b_traversable);
            this.p_editingMapModes.Controls.Add(this.b_nontraversable);
            this.p_editingMapModes.Location = new System.Drawing.Point(1062, 40);
            this.p_editingMapModes.Name = "p_editingMapModes";
            this.p_editingMapModes.Size = new System.Drawing.Size(173, 257);
            this.p_editingMapModes.TabIndex = 4;
            // 
            // b_endNode
            // 
            this.b_endNode.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_endNode.Location = new System.Drawing.Point(19, 212);
            this.b_endNode.Name = "b_endNode";
            this.b_endNode.Size = new System.Drawing.Size(128, 35);
            this.b_endNode.TabIndex = 5;
            this.b_endNode.Tag = "end";
            this.b_endNode.Text = "Set End Node";
            this.b_endNode.UseVisualStyleBackColor = true;
            this.b_endNode.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // b_startNode
            // 
            this.b_startNode.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_startNode.Location = new System.Drawing.Point(19, 154);
            this.b_startNode.Name = "b_startNode";
            this.b_startNode.Size = new System.Drawing.Size(128, 35);
            this.b_startNode.TabIndex = 4;
            this.b_startNode.Tag = "start";
            this.b_startNode.Text = "Set Start Node";
            this.b_startNode.UseVisualStyleBackColor = true;
            this.b_startNode.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // l_mapEditing
            // 
            this.l_mapEditing.AutoSize = true;
            this.l_mapEditing.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_mapEditing.Location = new System.Drawing.Point(3, 14);
            this.l_mapEditing.Name = "l_mapEditing";
            this.l_mapEditing.Size = new System.Drawing.Size(169, 20);
            this.l_mapEditing.TabIndex = 0;
            this.l_mapEditing.Text = "Map Editing Modes";
            // 
            // b_startPathFinding
            // 
            this.b_startPathFinding.Location = new System.Drawing.Point(867, 116);
            this.b_startPathFinding.Name = "b_startPathFinding";
            this.b_startPathFinding.Size = new System.Drawing.Size(134, 46);
            this.b_startPathFinding.TabIndex = 5;
            this.b_startPathFinding.Text = "Start PathFinding";
            this.b_startPathFinding.UseVisualStyleBackColor = true;
            this.b_startPathFinding.Click += new System.EventHandler(this.b_startPathFinding_Click);
            // 
            // c_selectedPathfinding
            // 
            this.c_selectedPathfinding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.c_selectedPathfinding.FormattingEnabled = true;
            this.c_selectedPathfinding.Items.AddRange(new object[] {
            "BackTrack",
            "Dijkstra",
            "A*"});
            this.c_selectedPathfinding.Location = new System.Drawing.Point(867, 77);
            this.c_selectedPathfinding.Name = "c_selectedPathfinding";
            this.c_selectedPathfinding.Size = new System.Drawing.Size(134, 24);
            this.c_selectedPathfinding.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(863, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Pathfinding";
            // 
            // tb_pathOutput
            // 
            this.tb_pathOutput.Location = new System.Drawing.Point(867, 266);
            this.tb_pathOutput.Multiline = true;
            this.tb_pathOutput.Name = "tb_pathOutput";
            this.tb_pathOutput.Size = new System.Drawing.Size(189, 129);
            this.tb_pathOutput.TabIndex = 8;
            // 
            // visitedLed
            // 
            this.visitedLed.Location = new System.Drawing.Point(867, 414);
            this.visitedLed.Name = "visitedLed";
            this.visitedLed.ReadOnly = true;
            this.visitedLed.Size = new System.Drawing.Size(22, 22);
            this.visitedLed.TabIndex = 9;
            // 
            // pathLed
            // 
            this.pathLed.Location = new System.Drawing.Point(868, 442);
            this.pathLed.Name = "pathLed";
            this.pathLed.ReadOnly = true;
            this.pathLed.Size = new System.Drawing.Size(22, 22);
            this.pathLed.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(895, 414);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "Visited";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(896, 442);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "Path";
            // 
            // b_mapRefresh
            // 
            this.b_mapRefresh.Location = new System.Drawing.Point(866, 178);
            this.b_mapRefresh.Name = "b_mapRefresh";
            this.b_mapRefresh.Size = new System.Drawing.Size(134, 46);
            this.b_mapRefresh.TabIndex = 13;
            this.b_mapRefresh.Text = "Refresh map";
            this.b_mapRefresh.UseVisualStyleBackColor = true;
            this.b_mapRefresh.Click += new System.EventHandler(this.b_mapRefresh_Click);
            // 
            // l_pathLength
            // 
            this.l_pathLength.AutoSize = true;
            this.l_pathLength.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_pathLength.Location = new System.Drawing.Point(863, 233);
            this.l_pathLength.Name = "l_pathLength";
            this.l_pathLength.Size = new System.Drawing.Size(124, 20);
            this.l_pathLength.TabIndex = 14;
            this.l_pathLength.Text = "Shortest Path";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label4.Location = new System.Drawing.Point(865, 488);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 16);
            this.label4.TabIndex = 15;
            this.label4.Text = "Elapsed";
            // 
            // tb_elapsedTime
            // 
            this.tb_elapsedTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_elapsedTime.Location = new System.Drawing.Point(868, 508);
            this.tb_elapsedTime.Name = "tb_elapsedTime";
            this.tb_elapsedTime.ReadOnly = true;
            this.tb_elapsedTime.Size = new System.Drawing.Size(188, 26);
            this.tb_elapsedTime.TabIndex = 16;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(865, 537);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(148, 16);
            this.label5.TabIndex = 17;
            this.label5.Text = "HH:mm:ss:milliseconds";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1442, 671);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tb_elapsedTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.l_pathLength);
            this.Controls.Add(this.b_mapRefresh);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pathLed);
            this.Controls.Add(this.visitedLed);
            this.Controls.Add(this.tb_pathOutput);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.c_selectedPathfinding);
            this.Controls.Add(this.b_startPathFinding);
            this.Controls.Add(this.p_editingMapModes);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Test";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.p_editingMapModes.ResumeLayout(false);
            this.p_editingMapModes.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMapToolStripMenuItem;
        private System.Windows.Forms.Button b_traversable;
        private System.Windows.Forms.Button b_nontraversable;
        private System.Windows.Forms.Panel p_editingMapModes;
        private System.Windows.Forms.Button b_endNode;
        private System.Windows.Forms.Button b_startNode;
        private System.Windows.Forms.Label l_mapEditing;
        private System.Windows.Forms.Button b_startPathFinding;
        private System.Windows.Forms.ComboBox c_selectedPathfinding;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tb_pathOutput;
        private System.Windows.Forms.TextBox visitedLed;
        private System.Windows.Forms.TextBox pathLed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button b_mapRefresh;
        private System.Windows.Forms.Label l_pathLength;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tb_elapsedTime;
        private System.Windows.Forms.Label label5;
    }
}

