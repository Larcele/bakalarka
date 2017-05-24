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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clustersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showHPAClustersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.b_traversable = new System.Windows.Forms.Button();
            this.b_nontraversable = new System.Windows.Forms.Button();
            this.b_endNode = new System.Windows.Forms.Button();
            this.b_startNode = new System.Windows.Forms.Button();
            this.l_mapEditing = new System.Windows.Forms.Label();
            this.b_startPathFinding = new System.Windows.Forms.Button();
            this.c_selectedPathfinding = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.visitedLed = new System.Windows.Forms.TextBox();
            this.pathLed = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.b_mapRefresh = new System.Windows.Forms.Button();
            this.l_pathLength = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tb_elapsedTime = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.l_nodeInfo = new System.Windows.Forms.Label();
            this.tb_nodeInfo = new System.Windows.Forms.TextBox();
            this.p_editingMapModes = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.l_pathCost = new System.Windows.Forms.TextBox();
            this.tb_pathLength = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cb_mapTests = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tb_expandedN = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.tb_praTime = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tb_hpaTime = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.p_editingMapModes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(22, 22);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.clustersToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1212, 29);
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
            this.loadMapToolStripMenuItem.Size = new System.Drawing.Size(194, 28);
            this.loadMapToolStripMenuItem.Text = "Load map";
            this.loadMapToolStripMenuItem.Click += new System.EventHandler(this.loadMapToolStripMenuItem_Click);
            // 
            // saveMapToolStripMenuItem
            // 
            this.saveMapToolStripMenuItem.Name = "saveMapToolStripMenuItem";
            this.saveMapToolStripMenuItem.Size = new System.Drawing.Size(194, 28);
            this.saveMapToolStripMenuItem.Text = "Save map";
            this.saveMapToolStripMenuItem.Click += new System.EventHandler(this.saveMapToolStripMenuItem_Click);
            // 
            // clustersToolStripMenuItem
            // 
            this.clustersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showHPAClustersToolStripMenuItem});
            this.clustersToolStripMenuItem.Name = "clustersToolStripMenuItem";
            this.clustersToolStripMenuItem.Size = new System.Drawing.Size(78, 25);
            this.clustersToolStripMenuItem.Text = "Clusters";
            // 
            // showHPAClustersToolStripMenuItem
            // 
            this.showHPAClustersToolStripMenuItem.Name = "showHPAClustersToolStripMenuItem";
            this.showHPAClustersToolStripMenuItem.Size = new System.Drawing.Size(226, 28);
            this.showHPAClustersToolStripMenuItem.Text = "Show HPA* clusters";
            this.showHPAClustersToolStripMenuItem.Click += new System.EventHandler(this.showHPAClustersToolStripMenuItem_Click);
            // 
            // b_traversable
            // 
            this.b_traversable.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_traversable.Location = new System.Drawing.Point(6, 28);
            this.b_traversable.Name = "b_traversable";
            this.b_traversable.Size = new System.Drawing.Size(128, 23);
            this.b_traversable.TabIndex = 2;
            this.b_traversable.Tag = "traversable";
            this.b_traversable.Text = "Traversable";
            this.b_traversable.UseVisualStyleBackColor = true;
            this.b_traversable.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // b_nontraversable
            // 
            this.b_nontraversable.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_nontraversable.Location = new System.Drawing.Point(6, 60);
            this.b_nontraversable.Name = "b_nontraversable";
            this.b_nontraversable.Size = new System.Drawing.Size(128, 23);
            this.b_nontraversable.TabIndex = 3;
            this.b_nontraversable.Tag = "!traversable";
            this.b_nontraversable.Text = "Non-Traversable";
            this.b_nontraversable.UseVisualStyleBackColor = true;
            this.b_nontraversable.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // b_endNode
            // 
            this.b_endNode.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_endNode.Location = new System.Drawing.Point(6, 92);
            this.b_endNode.Name = "b_endNode";
            this.b_endNode.Size = new System.Drawing.Size(128, 23);
            this.b_endNode.TabIndex = 5;
            this.b_endNode.Tag = "end";
            this.b_endNode.Text = "Set End Node";
            this.b_endNode.UseVisualStyleBackColor = true;
            this.b_endNode.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // b_startNode
            // 
            this.b_startNode.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_startNode.Location = new System.Drawing.Point(147, 92);
            this.b_startNode.Name = "b_startNode";
            this.b_startNode.Size = new System.Drawing.Size(128, 23);
            this.b_startNode.TabIndex = 4;
            this.b_startNode.Tag = "start";
            this.b_startNode.Text = "Set Start Node";
            this.b_startNode.UseVisualStyleBackColor = true;
            this.b_startNode.Click += new System.EventHandler(this.editingModesButton_Click);
            // 
            // l_mapEditing
            // 
            this.l_mapEditing.AutoSize = true;
            this.l_mapEditing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_mapEditing.Location = new System.Drawing.Point(3, 3);
            this.l_mapEditing.Name = "l_mapEditing";
            this.l_mapEditing.Size = new System.Drawing.Size(152, 18);
            this.l_mapEditing.TabIndex = 0;
            this.l_mapEditing.Text = "Map Editing Modes";
            // 
            // b_startPathFinding
            // 
            this.b_startPathFinding.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.b_startPathFinding.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_startPathFinding.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.b_startPathFinding.Location = new System.Drawing.Point(14, 90);
            this.b_startPathFinding.Name = "b_startPathFinding";
            this.b_startPathFinding.Size = new System.Drawing.Size(186, 25);
            this.b_startPathFinding.TabIndex = 5;
            this.b_startPathFinding.Text = "Start Pathfinding";
            this.b_startPathFinding.UseVisualStyleBackColor = false;
            this.b_startPathFinding.Click += new System.EventHandler(this.b_startPathFinding_Click);
            // 
            // c_selectedPathfinding
            // 
            this.c_selectedPathfinding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.c_selectedPathfinding.FormattingEnabled = true;
            this.c_selectedPathfinding.Items.AddRange(new object[] {
            "PRA* (Diagonal shortcut)",
            "PRA* (Manhattan heuristic)",
            "HPA* (Manhattan Heuristic)",
            "A* (Manhattan Heuristic)",
            "A* (Diagonal Shortcut)",
            "Dijkstra",
            "BackTrack"});
            this.c_selectedPathfinding.Location = new System.Drawing.Point(15, 61);
            this.c_selectedPathfinding.Name = "c_selectedPathfinding";
            this.c_selectedPathfinding.Size = new System.Drawing.Size(185, 24);
            this.c_selectedPathfinding.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(12, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Pathfinding";
            // 
            // visitedLed
            // 
            this.visitedLed.Location = new System.Drawing.Point(147, 32);
            this.visitedLed.Name = "visitedLed";
            this.visitedLed.ReadOnly = true;
            this.visitedLed.Size = new System.Drawing.Size(22, 22);
            this.visitedLed.TabIndex = 9;
            // 
            // pathLed
            // 
            this.pathLed.Location = new System.Drawing.Point(147, 56);
            this.pathLed.Name = "pathLed";
            this.pathLed.ReadOnly = true;
            this.pathLed.Size = new System.Drawing.Size(22, 22);
            this.pathLed.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(175, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "Visited";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(174, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "Path";
            // 
            // b_mapRefresh
            // 
            this.b_mapRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.b_mapRefresh.Location = new System.Drawing.Point(14, 124);
            this.b_mapRefresh.Name = "b_mapRefresh";
            this.b_mapRefresh.Size = new System.Drawing.Size(186, 25);
            this.b_mapRefresh.TabIndex = 13;
            this.b_mapRefresh.Text = "Refresh map";
            this.b_mapRefresh.UseVisualStyleBackColor = true;
            this.b_mapRefresh.Click += new System.EventHandler(this.b_mapRefresh_Click);
            // 
            // l_pathLength
            // 
            this.l_pathLength.AutoSize = true;
            this.l_pathLength.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_pathLength.Location = new System.Drawing.Point(505, 32);
            this.l_pathLength.Name = "l_pathLength";
            this.l_pathLength.Size = new System.Drawing.Size(111, 18);
            this.l_pathLength.TabIndex = 14;
            this.l_pathLength.Text = "Shortest Path";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label4.Location = new System.Drawing.Point(635, 109);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 18);
            this.label4.TabIndex = 15;
            this.label4.Text = "Elapsed";
            // 
            // tb_elapsedTime
            // 
            this.tb_elapsedTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_elapsedTime.Location = new System.Drawing.Point(638, 130);
            this.tb_elapsedTime.Name = "tb_elapsedTime";
            this.tb_elapsedTime.ReadOnly = true;
            this.tb_elapsedTime.Size = new System.Drawing.Size(103, 26);
            this.tb_elapsedTime.TabIndex = 16;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(747, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(26, 16);
            this.label5.TabIndex = 17;
            this.label5.Text = "ms";
            // 
            // l_nodeInfo
            // 
            this.l_nodeInfo.AutoSize = true;
            this.l_nodeInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_nodeInfo.Location = new System.Drawing.Point(635, 32);
            this.l_nodeInfo.Name = "l_nodeInfo";
            this.l_nodeInfo.Size = new System.Drawing.Size(81, 18);
            this.l_nodeInfo.TabIndex = 18;
            this.l_nodeInfo.Text = "Node Info";
            // 
            // tb_nodeInfo
            // 
            this.tb_nodeInfo.BackColor = System.Drawing.Color.White;
            this.tb_nodeInfo.Location = new System.Drawing.Point(638, 57);
            this.tb_nodeInfo.Multiline = true;
            this.tb_nodeInfo.Name = "tb_nodeInfo";
            this.tb_nodeInfo.ReadOnly = true;
            this.tb_nodeInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tb_nodeInfo.Size = new System.Drawing.Size(128, 48);
            this.tb_nodeInfo.TabIndex = 19;
            // 
            // p_editingMapModes
            // 
            this.p_editingMapModes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.p_editingMapModes.Controls.Add(this.b_endNode);
            this.p_editingMapModes.Controls.Add(this.l_mapEditing);
            this.p_editingMapModes.Controls.Add(this.b_nontraversable);
            this.p_editingMapModes.Controls.Add(this.b_startNode);
            this.p_editingMapModes.Controls.Add(this.b_traversable);
            this.p_editingMapModes.Controls.Add(this.visitedLed);
            this.p_editingMapModes.Controls.Add(this.pathLed);
            this.p_editingMapModes.Controls.Add(this.label3);
            this.p_editingMapModes.Controls.Add(this.label2);
            this.p_editingMapModes.Location = new System.Drawing.Point(209, 32);
            this.p_editingMapModes.Name = "p_editingMapModes";
            this.p_editingMapModes.Size = new System.Drawing.Size(290, 126);
            this.p_editingMapModes.TabIndex = 4;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.White;
            this.pictureBox1.Location = new System.Drawing.Point(3, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1280, 800);
            this.pictureBox1.TabIndex = 20;
            this.pictureBox1.TabStop = false;
            // 
            // panel
            // 
            this.panel.AutoScroll = true;
            this.panel.BackColor = System.Drawing.Color.White;
            this.panel.Controls.Add(this.pictureBox1);
            this.panel.Location = new System.Drawing.Point(16, 175);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(1280, 650);
            this.panel.TabIndex = 0;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(505, 57);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 16);
            this.label6.TabIndex = 20;
            this.label6.Text = "Path cost : ";
            // 
            // l_pathCost
            // 
            this.l_pathCost.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.l_pathCost.Location = new System.Drawing.Point(508, 75);
            this.l_pathCost.Name = "l_pathCost";
            this.l_pathCost.ReadOnly = true;
            this.l_pathCost.Size = new System.Drawing.Size(119, 26);
            this.l_pathCost.TabIndex = 23;
            // 
            // tb_pathLength
            // 
            this.tb_pathLength.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_pathLength.Location = new System.Drawing.Point(508, 130);
            this.tb_pathLength.Name = "tb_pathLength";
            this.tb_pathLength.ReadOnly = true;
            this.tb_pathLength.Size = new System.Drawing.Size(119, 26);
            this.tb_pathLength.TabIndex = 25;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(505, 109);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(80, 16);
            this.label7.TabIndex = 24;
            this.label7.Text = "Path length: ";
            // 
            // cb_mapTests
            // 
            this.cb_mapTests.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cb_mapTests.FormattingEnabled = true;
            this.cb_mapTests.Location = new System.Drawing.Point(782, 75);
            this.cb_mapTests.Name = "cb_mapTests";
            this.cb_mapTests.Size = new System.Drawing.Size(118, 24);
            this.cb_mapTests.TabIndex = 26;
            this.cb_mapTests.SelectedIndexChanged += new System.EventHandler(this.cb_mapTests_SelectedIndexChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label8.Location = new System.Drawing.Point(779, 32);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(123, 18);
            this.label8.TabIndex = 27;
            this.label8.Text = "Dynamic PRA* ";
            // 
            // tb_expandedN
            // 
            this.tb_expandedN.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_expandedN.Location = new System.Drawing.Point(782, 132);
            this.tb_expandedN.Name = "tb_expandedN";
            this.tb_expandedN.ReadOnly = true;
            this.tb_expandedN.Size = new System.Drawing.Size(118, 26);
            this.tb_expandedN.TabIndex = 28;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(779, 111);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(116, 16);
            this.label9.TabIndex = 29;
            this.label9.Text = "Nodes expanded:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label10.Location = new System.Drawing.Point(932, 109);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(41, 16);
            this.label10.TabIndex = 31;
            this.label10.Text = "PRA*";
            // 
            // tb_praTime
            // 
            this.tb_praTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_praTime.Location = new System.Drawing.Point(935, 130);
            this.tb_praTime.Name = "tb_praTime";
            this.tb_praTime.ReadOnly = true;
            this.tb_praTime.Size = new System.Drawing.Size(118, 26);
            this.tb_praTime.TabIndex = 30;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label11.Location = new System.Drawing.Point(932, 36);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(179, 18);
            this.label11.TabIndex = 33;
            this.label11.Text = "Abstraction build times";
            // 
            // tb_hpaTime
            // 
            this.tb_hpaTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tb_hpaTime.Location = new System.Drawing.Point(935, 78);
            this.tb_hpaTime.Name = "tb_hpaTime";
            this.tb_hpaTime.ReadOnly = true;
            this.tb_hpaTime.Size = new System.Drawing.Size(118, 26);
            this.tb_hpaTime.TabIndex = 32;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(1059, 88);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(26, 16);
            this.label12.TabIndex = 34;
            this.label12.Text = "ms";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(1059, 138);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(26, 16);
            this.label13.TabIndex = 35;
            this.label13.Text = "ms";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label14.Location = new System.Drawing.Point(779, 50);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(87, 18);
            this.label14.TabIndex = 36;
            this.label14.Text = "Map Tests";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label15.Location = new System.Drawing.Point(932, 61);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(41, 16);
            this.label15.TabIndex = 37;
            this.label15.Text = "HPA*";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1212, 651);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tb_hpaTime);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.tb_praTime);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.tb_expandedN);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cb_mapTests);
            this.Controls.Add(this.tb_pathLength);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.l_pathCost);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tb_nodeInfo);
            this.Controls.Add(this.l_nodeInfo);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tb_elapsedTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.l_pathLength);
            this.Controls.Add(this.b_mapRefresh);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.c_selectedPathfinding);
            this.Controls.Add(this.b_startPathFinding);
            this.Controls.Add(this.p_editingMapModes);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Test";
            this.Resize += new System.EventHandler(this.MainWindow_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.p_editingMapModes.ResumeLayout(false);
            this.p_editingMapModes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadMapToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMapToolStripMenuItem;
        private System.Windows.Forms.Button b_traversable;
        private System.Windows.Forms.Button b_nontraversable;
        private System.Windows.Forms.Button b_endNode;
        private System.Windows.Forms.Button b_startNode;
        private System.Windows.Forms.Label l_mapEditing;
        private System.Windows.Forms.Button b_startPathFinding;
        private System.Windows.Forms.ComboBox c_selectedPathfinding;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox visitedLed;
        private System.Windows.Forms.TextBox pathLed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button b_mapRefresh;
        private System.Windows.Forms.Label l_pathLength;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tb_elapsedTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label l_nodeInfo;
        private System.Windows.Forms.TextBox tb_nodeInfo;
        private System.Windows.Forms.Panel p_editingMapModes;
        private System.Windows.Forms.ToolStripMenuItem clustersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHPAClustersToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox l_pathCost;
        private System.Windows.Forms.TextBox tb_pathLength;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cb_mapTests;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tb_expandedN;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tb_praTime;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tb_hpaTime;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
    }
}

