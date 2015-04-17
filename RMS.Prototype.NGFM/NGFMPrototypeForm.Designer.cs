using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RMS.Prototype.NGFM
{
    partial class NGFMPrototypeForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NGFMPrototypeForm));
            this.txtbxCDL1 = new System.Windows.Forms.TextBox();
            this.txtbxIR1 = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToPosition1 = new System.Windows.Forms.ToolStripMenuItem();
            this.rewritePosition1 = new System.Windows.Forms.ToolStripMenuItem();
            this.delete1 = new System.Windows.Forms.ToolStripMenuItem();
            this.createCopiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.treeListView1 = new BrightIdeasSoftware.TreeListView();
            this.scheduleTreeListView1 = new BrightIdeasSoftware.TreeListView();
            this.contextMenuStrip4 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveScheduleToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gridConExp1 = new System.Windows.Forms.DataGridView();
            this.PrimeID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Payout = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProcessInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabSDL1 = new System.Windows.Forms.TabPage();
            this.tabIR1 = new System.Windows.Forms.TabPage();
            this.tabTreeView1 = new System.Windows.Forms.TabPage();
            this.tabScedule1 = new System.Windows.Forms.TabPage();
            this.tabCOL1 = new System.Windows.Forms.TabPage();
            this.gridCOL1 = new System.Windows.Forms.DataGridView();
            this.COL1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.COL1Check = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tabTimeSeries1 = new System.Windows.Forms.TabPage();
            this.gridTimeSeries1 = new System.Windows.Forms.DataGridView();
            this.time1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.guLoss = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.appendToPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updatePositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addNew2 = new System.Windows.Forms.ToolStripMenuItem();
            this.removeSelected2 = new System.Windows.Forms.ToolStripMenuItem();
            this.tabBugLog1 = new System.Windows.Forms.TabPage();
            this.textbxBugLog1 = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenu1 = new System.Windows.Forms.ToolStripMenuItem();
            this.openFile1 = new System.Windows.Forms.ToolStripMenuItem();
            this.addFile1 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFile1 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveasFile1 = new System.Windows.Forms.ToolStripMenuItem();
            this.closeFile1 = new System.Windows.Forms.ToolStripMenuItem();
            this.parseCompileMenu1 = new System.Windows.Forms.ToolStripMenuItem();
            this.compileSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compileAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.executeMenu1 = new System.Windows.Forms.ToolStripMenuItem();
            this.executeFM1 = new System.Windows.Forms.ToolStripMenuItem();
            this.gULossToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fromfileGULoss1 = new System.Windows.Forms.ToolStripMenuItem();
            this.simulateGULoss1 = new System.Windows.Forms.ToolStripMenuItem();
            this.simulateRandomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gridTreaty2 = new System.Windows.Forms.DataGridView();
            this.TreatyId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TreatyPayout = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TreatyProcessInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPosition = new System.Windows.Forms.DataGridView();
            this.Position = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuStrip2 = new System.Windows.Forms.MenuStrip();
            this.fileMenu2 = new System.Windows.Forms.ToolStripMenuItem();
            this.openFile2 = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFile2 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveasFile2 = new System.Windows.Forms.ToolStripMenuItem();
            this.closeFile2 = new System.Windows.Forms.ToolStripMenuItem();
            this.clearFile2 = new System.Windows.Forms.ToolStripMenuItem();
            this.parseCompileMenu2 = new System.Windows.Forms.ToolStripMenuItem();
            this.compileSelectedToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.compileAllToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.executeMenu2 = new System.Windows.Forms.ToolStripMenuItem();
            this.executeFM2 = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.gridPositionContent = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip3 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.toolStripContainer2 = new System.Windows.Forms.ToolStripContainer();
            this.toolStripProgress = new System.Windows.Forms.ToolStrip();
            this.buttonOutput = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.InfoNumberThreads = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.InfoNumberTasks = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripContainer3 = new System.Windows.Forms.ToolStripContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.txtbxCurrentContract = new System.Windows.Forms.ToolStripTextBox();
            this.bttnFreeze = new System.Windows.Forms.ToolStripButton();
            this.toolStripContainer4 = new System.Windows.Forms.ToolStripContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.textNodeResults = new System.Windows.Forms.TextBox();
            this.treeTreaty = new System.Windows.Forms.TreeView();
            this.tabControl3 = new System.Windows.Forms.TabControl();
            this.tabID1 = new System.Windows.Forms.TabPage();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.tabID2 = new System.Windows.Forms.TabPage();
            this.toolStripContainer5 = new System.Windows.Forms.ToolStripContainer();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.treeListView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scheduleTreeListView1)).BeginInit();
            this.contextMenuStrip4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridConExp1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabSDL1.SuspendLayout();
            this.tabIR1.SuspendLayout();
            this.tabTreeView1.SuspendLayout();
            this.tabScedule1.SuspendLayout();
            this.tabCOL1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCOL1)).BeginInit();
            this.tabTimeSeries1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTimeSeries1)).BeginInit();
            this.contextMenuStrip2.SuspendLayout();
            this.tabBugLog1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTreaty2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPosition)).BeginInit();
            this.menuStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPositionContent)).BeginInit();
            this.contextMenuStrip3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.toolStripContainer2.ContentPanel.SuspendLayout();
            this.toolStripContainer2.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer2.SuspendLayout();
            this.toolStripProgress.SuspendLayout();
            this.toolStripContainer3.ContentPanel.SuspendLayout();
            this.toolStripContainer3.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer3.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.toolStripContainer4.ContentPanel.SuspendLayout();
            this.toolStripContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl3.SuspendLayout();
            this.tabID1.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.tabID2.SuspendLayout();
            this.toolStripContainer5.ContentPanel.SuspendLayout();
            this.toolStripContainer5.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer5.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtbxCDL1
            // 
            this.txtbxCDL1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtbxCDL1.Location = new System.Drawing.Point(3, 3);
            this.txtbxCDL1.Multiline = true;
            this.txtbxCDL1.Name = "txtbxCDL1";
            this.txtbxCDL1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtbxCDL1.Size = new System.Drawing.Size(711, 396);
            this.txtbxCDL1.TabIndex = 0;
            this.txtbxCDL1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtbx_KeyDown);
            this.txtbxCDL1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtbxCDL1_MouseDoubleClick);
            this.txtbxCDL1.MouseEnter += new System.EventHandler(this.txtbx_MouseEnter);
            // 
            // txtbxIR1
            // 
            this.txtbxIR1.BackColor = System.Drawing.SystemColors.Window;
            this.txtbxIR1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtbxIR1.Location = new System.Drawing.Point(3, 3);
            this.txtbxIR1.Multiline = true;
            this.txtbxIR1.Name = "txtbxIR1";
            this.txtbxIR1.ReadOnly = true;
            this.txtbxIR1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtbxIR1.Size = new System.Drawing.Size(711, 394);
            this.txtbxIR1.TabIndex = 3;
            this.txtbxIR1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtbx_KeyDown);
            this.txtbxIR1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtbxIR1_MouseDoubleClick);
            this.txtbxIR1.MouseEnter += new System.EventHandler(this.txtbx_MouseEnter);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToPosition1,
            this.rewritePosition1,
            this.delete1,
            this.createCopiesToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.ShowCheckMargin = true;
            this.contextMenuStrip1.Size = new System.Drawing.Size(205, 92);
            // 
            // addToPosition1
            // 
            this.addToPosition1.Name = "addToPosition1";
            this.addToPosition1.Size = new System.Drawing.Size(204, 22);
            this.addToPosition1.Text = "Append  To Position";
            this.addToPosition1.Click += new System.EventHandler(this.addToPosition1_Click);
            // 
            // rewritePosition1
            // 
            this.rewritePosition1.Name = "rewritePosition1";
            this.rewritePosition1.Size = new System.Drawing.Size(204, 22);
            this.rewritePosition1.Text = "Update Position";
            this.rewritePosition1.Click += new System.EventHandler(this.rewritePosition1_Click);
            // 
            // delete1
            // 
            this.delete1.Name = "delete1";
            this.delete1.Size = new System.Drawing.Size(204, 22);
            this.delete1.Text = "Remove Selected";
            this.delete1.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // createCopiesToolStripMenuItem
            // 
            this.createCopiesToolStripMenuItem.Name = "createCopiesToolStripMenuItem";
            this.createCopiesToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
            this.createCopiesToolStripMenuItem.Text = "Create Copies";
            this.createCopiesToolStripMenuItem.Click += new System.EventHandler(this.createCopiesToolStripMenuItem_Click);
            // 
            // treeListView1
            // 
            this.treeListView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeListView1.FullRowSelect = true;
            this.treeListView1.Location = new System.Drawing.Point(3, 3);
            this.treeListView1.Name = "treeListView1";
            this.treeListView1.OwnerDraw = true;
            this.treeListView1.ShowGroups = false;
            this.treeListView1.Size = new System.Drawing.Size(711, 394);
            this.treeListView1.TabIndex = 12;
            this.treeListView1.UseCompatibleStateImageBehavior = false;
            this.treeListView1.View = System.Windows.Forms.View.Details;
            this.treeListView1.VirtualMode = true;
            // 
            // scheduleTreeListView1
            // 
            this.scheduleTreeListView1.ContextMenuStrip = this.contextMenuStrip4;
            this.scheduleTreeListView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scheduleTreeListView1.FullRowSelect = true;
            this.scheduleTreeListView1.Location = new System.Drawing.Point(3, 3);
            this.scheduleTreeListView1.Name = "scheduleTreeListView1";
            this.scheduleTreeListView1.OwnerDraw = true;
            this.scheduleTreeListView1.ShowGroups = false;
            this.scheduleTreeListView1.Size = new System.Drawing.Size(711, 394);
            this.scheduleTreeListView1.TabIndex = 14;
            this.scheduleTreeListView1.UseCompatibleStateImageBehavior = false;
            this.scheduleTreeListView1.View = System.Windows.Forms.View.Details;
            this.scheduleTreeListView1.VirtualMode = true;
            // 
            // contextMenuStrip4
            // 
            this.contextMenuStrip4.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveScheduleToFileToolStripMenuItem});
            this.contextMenuStrip4.Name = "contextMenuStrip4";
            this.contextMenuStrip4.Size = new System.Drawing.Size(246, 26);
            // 
            // saveScheduleToFileToolStripMenuItem
            // 
            this.saveScheduleToFileToolStripMenuItem.Name = "saveScheduleToFileToolStripMenuItem";
            this.saveScheduleToFileToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.saveScheduleToFileToolStripMenuItem.Text = "Save GU Loss for Schedule to file";
            this.saveScheduleToFileToolStripMenuItem.Click += new System.EventHandler(this.saveScheduleToFileToolStripMenuItem_Click);
            // 
            // gridConExp1
            // 
            this.gridConExp1.AllowUserToAddRows = false;
            this.gridConExp1.AllowUserToDeleteRows = false;
            this.gridConExp1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridConExp1.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridConExp1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridConExp1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridConExp1.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridConExp1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PrimeID,
            this.Payout,
            this.ProcessInfo});
            this.gridConExp1.ContextMenuStrip = this.contextMenuStrip1;
            this.gridConExp1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridConExp1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridConExp1.Location = new System.Drawing.Point(0, 0);
            this.gridConExp1.Name = "gridConExp1";
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Gray;
            this.gridConExp1.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.gridConExp1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridConExp1.Size = new System.Drawing.Size(711, 221);
            this.gridConExp1.TabIndex = 22;
            this.gridConExp1.SelectionChanged += new System.EventHandler(this.gridConExp1_SelectionChanged);
            this.gridConExp1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.gridConExp1_MouseDoubleClick);
            // 
            // PrimeID
            // 
            this.PrimeID.Frozen = true;
            this.PrimeID.HeaderText = "ID";
            this.PrimeID.Name = "PrimeID";
            this.PrimeID.ReadOnly = true;
            // 
            // Payout
            // 
            this.Payout.HeaderText = "Payout";
            this.Payout.Name = "Payout";
            this.Payout.ReadOnly = true;
            // 
            // ProcessInfo
            // 
            this.ProcessInfo.FillWeight = 468F;
            this.ProcessInfo.HeaderText = "Processing Info";
            this.ProcessInfo.Name = "ProcessInfo";
            this.ProcessInfo.ReadOnly = true;
            this.ProcessInfo.Width = 468;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabSDL1);
            this.tabControl1.Controls.Add(this.tabIR1);
            this.tabControl1.Controls.Add(this.tabTreeView1);
            this.tabControl1.Controls.Add(this.tabScedule1);
            this.tabControl1.Controls.Add(this.tabCOL1);
            this.tabControl1.Controls.Add(this.tabTimeSeries1);
            this.tabControl1.Controls.Add(this.tabBugLog1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(725, 428);
            this.tabControl1.TabIndex = 26;
            // 
            // tabSDL1
            // 
            this.tabSDL1.Controls.Add(this.txtbxCDL1);
            this.tabSDL1.Location = new System.Drawing.Point(4, 22);
            this.tabSDL1.Name = "tabSDL1";
            this.tabSDL1.Padding = new System.Windows.Forms.Padding(3);
            this.tabSDL1.Size = new System.Drawing.Size(717, 402);
            this.tabSDL1.TabIndex = 0;
            this.tabSDL1.Text = "CDL";
            this.tabSDL1.ToolTipText = "Contract Definition Language string";
            this.tabSDL1.UseVisualStyleBackColor = true;
            // 
            // tabIR1
            // 
            this.tabIR1.Controls.Add(this.txtbxIR1);
            this.tabIR1.Location = new System.Drawing.Point(4, 22);
            this.tabIR1.Name = "tabIR1";
            this.tabIR1.Padding = new System.Windows.Forms.Padding(3);
            this.tabIR1.Size = new System.Drawing.Size(717, 400);
            this.tabIR1.TabIndex = 1;
            this.tabIR1.Text = "IR";
            this.tabIR1.ToolTipText = "Intermediate Representation (Jason form of CDL)";
            this.tabIR1.UseVisualStyleBackColor = true;
            // 
            // tabTreeView1
            // 
            this.tabTreeView1.Controls.Add(this.treeListView1);
            this.tabTreeView1.Location = new System.Drawing.Point(4, 22);
            this.tabTreeView1.Name = "tabTreeView1";
            this.tabTreeView1.Padding = new System.Windows.Forms.Padding(3);
            this.tabTreeView1.Size = new System.Drawing.Size(717, 400);
            this.tabTreeView1.TabIndex = 2;
            this.tabTreeView1.Text = "Tree View";
            this.tabTreeView1.ToolTipText = "Contract Exposure Tree View";
            this.tabTreeView1.UseVisualStyleBackColor = true;
            // 
            // tabScedule1
            // 
            this.tabScedule1.Controls.Add(this.scheduleTreeListView1);
            this.tabScedule1.Location = new System.Drawing.Point(4, 22);
            this.tabScedule1.Name = "tabScedule1";
            this.tabScedule1.Padding = new System.Windows.Forms.Padding(3);
            this.tabScedule1.Size = new System.Drawing.Size(717, 400);
            this.tabScedule1.TabIndex = 3;
            this.tabScedule1.Text = "Schedule";
            this.tabScedule1.ToolTipText = "Schedule Tree View";
            this.tabScedule1.UseVisualStyleBackColor = true;
            // 
            // tabCOL1
            // 
            this.tabCOL1.Controls.Add(this.gridCOL1);
            this.tabCOL1.Location = new System.Drawing.Point(4, 22);
            this.tabCOL1.Name = "tabCOL1";
            this.tabCOL1.Padding = new System.Windows.Forms.Padding(3);
            this.tabCOL1.Size = new System.Drawing.Size(717, 400);
            this.tabCOL1.TabIndex = 4;
            this.tabCOL1.Text = "COL";
            this.tabCOL1.UseVisualStyleBackColor = true;
            // 
            // gridCOL1
            // 
            this.gridCOL1.AllowUserToAddRows = false;
            this.gridCOL1.AllowUserToDeleteRows = false;
            this.gridCOL1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridCOL1.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridCOL1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridCOL1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridCOL1.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridCOL1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.COL1,
            this.COL1Check});
            this.gridCOL1.ContextMenuStrip = this.contextMenuStrip1;
            this.gridCOL1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridCOL1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridCOL1.Location = new System.Drawing.Point(3, 3);
            this.gridCOL1.Name = "gridCOL1";
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.Gray;
            this.gridCOL1.RowsDefaultCellStyle = dataGridViewCellStyle3;
            this.gridCOL1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridCOL1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCOL1.Size = new System.Drawing.Size(711, 394);
            this.gridCOL1.TabIndex = 23;
            this.gridCOL1.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.gridCOL1_ColumnHeaderMouseClick);
            // 
            // COL1
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.COL1.DefaultCellStyle = dataGridViewCellStyle2;
            this.COL1.FillWeight = 250F;
            this.COL1.HeaderText = "COL";
            this.COL1.Name = "COL1";
            this.COL1.ReadOnly = true;
            this.COL1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.COL1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.COL1.ToolTipText = "Couse Of Loss";
            this.COL1.Width = 250;
            // 
            // COL1Check
            // 
            this.COL1Check.FillWeight = 80F;
            this.COL1Check.HeaderText = "Execute FM";
            this.COL1Check.Name = "COL1Check";
            this.COL1Check.ToolTipText = "Choose for FM Execution";
            this.COL1Check.Width = 80;
            // 
            // tabTimeSeries1
            // 
            this.tabTimeSeries1.Controls.Add(this.gridTimeSeries1);
            this.tabTimeSeries1.Location = new System.Drawing.Point(4, 22);
            this.tabTimeSeries1.Name = "tabTimeSeries1";
            this.tabTimeSeries1.Padding = new System.Windows.Forms.Padding(3);
            this.tabTimeSeries1.Size = new System.Drawing.Size(717, 400);
            this.tabTimeSeries1.TabIndex = 5;
            this.tabTimeSeries1.Text = "Time Series";
            this.tabTimeSeries1.UseVisualStyleBackColor = true;
            // 
            // gridTimeSeries1
            // 
            this.gridTimeSeries1.AllowUserToAddRows = false;
            this.gridTimeSeries1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridTimeSeries1.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridTimeSeries1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridTimeSeries1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridTimeSeries1.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridTimeSeries1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.time1,
            this.guLoss,
            this.dataGridViewTextBoxColumn2});
            this.gridTimeSeries1.ContextMenuStrip = this.contextMenuStrip2;
            this.gridTimeSeries1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTimeSeries1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridTimeSeries1.Location = new System.Drawing.Point(3, 3);
            this.gridTimeSeries1.Name = "gridTimeSeries1";
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.Gray;
            this.gridTimeSeries1.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.gridTimeSeries1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridTimeSeries1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridTimeSeries1.Size = new System.Drawing.Size(711, 394);
            this.gridTimeSeries1.TabIndex = 26;
            // 
            // time1
            // 
            this.time1.HeaderText = "Time";
            this.time1.Name = "time1";
            this.time1.ReadOnly = true;
            this.time1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.time1.Width = 262;
            // 
            // guLoss
            // 
            this.guLoss.HeaderText = "Subject Loss";
            this.guLoss.Name = "guLoss";
            this.guLoss.Width = 150;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "Payout";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn2.Width = 150;
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.appendToPositionToolStripMenuItem,
            this.updatePositionToolStripMenuItem,
            this.addNew2,
            this.removeSelected2});
            this.contextMenuStrip2.Name = "contextMenuStrip2";
            this.contextMenuStrip2.ShowCheckMargin = true;
            this.contextMenuStrip2.Size = new System.Drawing.Size(202, 92);
            // 
            // appendToPositionToolStripMenuItem
            // 
            this.appendToPositionToolStripMenuItem.Name = "appendToPositionToolStripMenuItem";
            this.appendToPositionToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.appendToPositionToolStripMenuItem.Text = "Append To Position";
            this.appendToPositionToolStripMenuItem.Click += new System.EventHandler(this.appendToPositionToolStripMenuItem_Click);
            // 
            // updatePositionToolStripMenuItem
            // 
            this.updatePositionToolStripMenuItem.Name = "updatePositionToolStripMenuItem";
            this.updatePositionToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.updatePositionToolStripMenuItem.Text = "Update Position";
            this.updatePositionToolStripMenuItem.Click += new System.EventHandler(this.updatePositionToolStripMenuItem_Click);
            // 
            // addNew2
            // 
            this.addNew2.Name = "addNew2";
            this.addNew2.Size = new System.Drawing.Size(201, 22);
            this.addNew2.Text = "Add New";
            this.addNew2.Click += new System.EventHandler(this.addNew2_Click);
            // 
            // removeSelected2
            // 
            this.removeSelected2.Name = "removeSelected2";
            this.removeSelected2.Size = new System.Drawing.Size(201, 22);
            this.removeSelected2.Text = "Remove Selected";
            this.removeSelected2.Click += new System.EventHandler(this.removeSelected2_Click);
            // 
            // tabBugLog1
            // 
            this.tabBugLog1.Controls.Add(this.textbxBugLog1);
            this.tabBugLog1.Location = new System.Drawing.Point(4, 22);
            this.tabBugLog1.Name = "tabBugLog1";
            this.tabBugLog1.Padding = new System.Windows.Forms.Padding(3);
            this.tabBugLog1.Size = new System.Drawing.Size(717, 400);
            this.tabBugLog1.TabIndex = 6;
            this.tabBugLog1.Text = "Bug Log";
            this.tabBugLog1.UseVisualStyleBackColor = true;
            // 
            // textbxBugLog1
            // 
            this.textbxBugLog1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textbxBugLog1.Location = new System.Drawing.Point(3, 3);
            this.textbxBugLog1.Multiline = true;
            this.textbxBugLog1.Name = "textbxBugLog1";
            this.textbxBugLog1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textbxBugLog1.Size = new System.Drawing.Size(711, 394);
            this.textbxBugLog1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu1,
            this.parseCompileMenu1,
            this.executeMenu1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(711, 24);
            this.menuStrip1.TabIndex = 27;
            this.menuStrip1.Text = "menuStrip2";
            // 
            // fileMenu1
            // 
            this.fileMenu1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFile1,
            this.addFile1,
            this.saveFile1,
            this.saveasFile1,
            this.closeFile1});
            this.fileMenu1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.fileMenu1.Name = "fileMenu1";
            this.fileMenu1.Size = new System.Drawing.Size(38, 20);
            this.fileMenu1.Text = "File";
            // 
            // openFile1
            // 
            this.openFile1.Name = "openFile1";
            this.openFile1.Size = new System.Drawing.Size(152, 22);
            this.openFile1.Text = "Open";
            this.openFile1.ToolTipText = "Choose Exposure Extract(s)";
            this.openFile1.Click += new System.EventHandler(this.openFile1_Click);
            // 
            // addFile1
            // 
            this.addFile1.Name = "addFile1";
            this.addFile1.Size = new System.Drawing.Size(152, 22);
            this.addFile1.Text = "Add";
            this.addFile1.Click += new System.EventHandler(this.addFile1_Click);
            // 
            // saveFile1
            // 
            this.saveFile1.Name = "saveFile1";
            this.saveFile1.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveFile1.Size = new System.Drawing.Size(152, 22);
            this.saveFile1.Text = "Save";
            this.saveFile1.ToolTipText = "Save All Exposure Extracts";
            this.saveFile1.Click += new System.EventHandler(this.saveFile1_Click);
            // 
            // saveasFile1
            // 
            this.saveasFile1.Name = "saveasFile1";
            this.saveasFile1.Size = new System.Drawing.Size(152, 22);
            this.saveasFile1.Text = "Save As";
            this.saveasFile1.ToolTipText = "Save All Exposure Extracts";
            this.saveasFile1.Click += new System.EventHandler(this.saveasFile1_Click);
            // 
            // closeFile1
            // 
            this.closeFile1.Name = "closeFile1";
            this.closeFile1.Size = new System.Drawing.Size(152, 22);
            this.closeFile1.Text = "Close";
            this.closeFile1.ToolTipText = "Save All Exposure Extracts and Clear";
            this.closeFile1.Click += new System.EventHandler(this.closeFile1_Click);
            // 
            // parseCompileMenu1
            // 
            this.parseCompileMenu1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.compileSelectedToolStripMenuItem,
            this.compileAllToolStripMenuItem});
            this.parseCompileMenu1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.parseCompileMenu1.Name = "parseCompileMenu1";
            this.parseCompileMenu1.Size = new System.Drawing.Size(108, 20);
            this.parseCompileMenu1.Text = "Parse + Compile";
            // 
            // compileSelectedToolStripMenuItem
            // 
            this.compileSelectedToolStripMenuItem.Name = "compileSelectedToolStripMenuItem";
            this.compileSelectedToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.compileSelectedToolStripMenuItem.Text = "Compile Selected";
            this.compileSelectedToolStripMenuItem.Click += new System.EventHandler(this.parseCompile1_Click);
            // 
            // compileAllToolStripMenuItem
            // 
            this.compileAllToolStripMenuItem.Name = "compileAllToolStripMenuItem";
            this.compileAllToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.compileAllToolStripMenuItem.Text = "Compile All";
            this.compileAllToolStripMenuItem.Click += new System.EventHandler(this.parseCompileAll1_Click);
            // 
            // executeMenu1
            // 
            this.executeMenu1.AutoSize = false;
            this.executeMenu1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.executeFM1,
            this.gULossToolStripMenuItem});
            this.executeMenu1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.executeMenu1.Name = "executeMenu1";
            this.executeMenu1.Size = new System.Drawing.Size(64, 20);
            this.executeMenu1.Text = "Execute";
            // 
            // executeFM1
            // 
            this.executeFM1.Name = "executeFM1";
            this.executeFM1.Size = new System.Drawing.Size(139, 22);
            this.executeFM1.Text = "Execute FM";
            this.executeFM1.Click += new System.EventHandler(this.executeFM1_Click);
            // 
            // gULossToolStripMenuItem
            // 
            this.gULossToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fromfileGULoss1,
            this.simulateGULoss1,
            this.simulateRandomToolStripMenuItem,
            this.saveToFileToolStripMenuItem});
            this.gULossToolStripMenuItem.Name = "gULossToolStripMenuItem";
            this.gULossToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.gULossToolStripMenuItem.Text = "GU Loss";
            // 
            // fromfileGULoss1
            // 
            this.fromfileGULoss1.Name = "fromfileGULoss1";
            this.fromfileGULoss1.Size = new System.Drawing.Size(180, 22);
            this.fromfileGULoss1.Text = "From File";
            this.fromfileGULoss1.ToolTipText = "Upload GU (per event) Result From File(s)";
            this.fromfileGULoss1.Click += new System.EventHandler(this.fromfileGULoss1_Click);
            // 
            // simulateGULoss1
            // 
            this.simulateGULoss1.Name = "simulateGULoss1";
            this.simulateGULoss1.Size = new System.Drawing.Size(180, 22);
            this.simulateGULoss1.Text = "Simulate";
            this.simulateGULoss1.ToolTipText = "Simulate Ground Up Loss";
            this.simulateGULoss1.Click += new System.EventHandler(this.simulateGULoss1_Click);
            // 
            // simulateRandomToolStripMenuItem
            // 
            this.simulateRandomToolStripMenuItem.Name = "simulateRandomToolStripMenuItem";
            this.simulateRandomToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.simulateRandomToolStripMenuItem.Text = "Simulate (Random)";
            this.simulateRandomToolStripMenuItem.Click += new System.EventHandler(this.simulateRandomToolStripMenuItem_Click);
            // 
            // saveToFileToolStripMenuItem
            // 
            this.saveToFileToolStripMenuItem.Name = "saveToFileToolStripMenuItem";
            this.saveToFileToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveToFileToolStripMenuItem.Text = "Save To File";
            this.saveToFileToolStripMenuItem.Click += new System.EventHandler(this.saveToFileToolStripMenuItem_Click);
            // 
            // gridTreaty2
            // 
            this.gridTreaty2.AllowUserToAddRows = false;
            this.gridTreaty2.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridTreaty2.BackgroundColor = System.Drawing.Color.AntiqueWhite;
            this.gridTreaty2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridTreaty2.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridTreaty2.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridTreaty2.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TreatyId,
            this.TreatyPayout,
            this.TreatyProcessInfo});
            this.gridTreaty2.ContextMenuStrip = this.contextMenuStrip2;
            this.gridTreaty2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTreaty2.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridTreaty2.Location = new System.Drawing.Point(0, 0);
            this.gridTreaty2.Name = "gridTreaty2";
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.AntiqueWhite;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.DarkOrange;
            this.gridTreaty2.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.gridTreaty2.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridTreaty2.Size = new System.Drawing.Size(711, 221);
            this.gridTreaty2.TabIndex = 24;
            this.gridTreaty2.SelectionChanged += new System.EventHandler(this.gridTreaty2_SelectionChanged);
            this.gridTreaty2.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.gridTreaty2_MouseDoubleClick);
            // 
            // TreatyId
            // 
            this.TreatyId.HeaderText = "ID";
            this.TreatyId.Name = "TreatyId";
            this.TreatyId.ReadOnly = true;
            // 
            // TreatyPayout
            // 
            this.TreatyPayout.HeaderText = "Payout";
            this.TreatyPayout.Name = "TreatyPayout";
            this.TreatyPayout.ReadOnly = true;
            // 
            // TreatyProcessInfo
            // 
            this.TreatyProcessInfo.FillWeight = 468F;
            this.TreatyProcessInfo.HeaderText = "Processing Info";
            this.TreatyProcessInfo.Name = "TreatyProcessInfo";
            this.TreatyProcessInfo.ReadOnly = true;
            this.TreatyProcessInfo.Width = 468;
            // 
            // gridPosition
            // 
            this.gridPosition.AllowUserToAddRows = false;
            this.gridPosition.AllowUserToDeleteRows = false;
            this.gridPosition.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridPosition.BackgroundColor = System.Drawing.Color.AntiqueWhite;
            this.gridPosition.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridPosition.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridPosition.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridPosition.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Position});
            this.gridPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPosition.Location = new System.Drawing.Point(0, 0);
            this.gridPosition.Name = "gridPosition";
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.AntiqueWhite;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.DarkOrange;
            this.gridPosition.RowsDefaultCellStyle = dataGridViewCellStyle6;
            this.gridPosition.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPosition.Size = new System.Drawing.Size(224, 252);
            this.gridPosition.TabIndex = 23;
            this.gridPosition.SelectionChanged += new System.EventHandler(this.gridPosition_SelectionChanged);
            // 
            // Position
            // 
            this.Position.Frozen = true;
            this.Position.HeaderText = "Position Title";
            this.Position.Name = "Position";
            this.Position.ReadOnly = true;
            this.Position.Width = 182;
            // 
            // menuStrip2
            // 
            this.menuStrip2.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu2,
            this.parseCompileMenu2,
            this.executeMenu2});
            this.menuStrip2.Location = new System.Drawing.Point(0, 0);
            this.menuStrip2.Name = "menuStrip2";
            this.menuStrip2.Size = new System.Drawing.Size(711, 24);
            this.menuStrip2.TabIndex = 0;
            this.menuStrip2.Text = "menuStrip2";
            // 
            // fileMenu2
            // 
            this.fileMenu2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFile2,
            this.addToolStripMenuItem,
            this.saveFile2,
            this.saveasFile2,
            this.closeFile2,
            this.clearFile2});
            this.fileMenu2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.fileMenu2.Name = "fileMenu2";
            this.fileMenu2.Size = new System.Drawing.Size(38, 20);
            this.fileMenu2.Text = "File";
            // 
            // openFile2
            // 
            this.openFile2.Name = "openFile2";
            this.openFile2.Size = new System.Drawing.Size(117, 22);
            this.openFile2.Text = "Open";
            this.openFile2.Click += new System.EventHandler(this.openFile2_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // saveFile2
            // 
            this.saveFile2.Name = "saveFile2";
            this.saveFile2.Size = new System.Drawing.Size(117, 22);
            this.saveFile2.Text = "Save";
            this.saveFile2.Click += new System.EventHandler(this.saveFile2_Click);
            // 
            // saveasFile2
            // 
            this.saveasFile2.Name = "saveasFile2";
            this.saveasFile2.Size = new System.Drawing.Size(117, 22);
            this.saveasFile2.Text = "Save As";
            this.saveasFile2.Click += new System.EventHandler(this.saveasFile2_Click);
            // 
            // closeFile2
            // 
            this.closeFile2.Name = "closeFile2";
            this.closeFile2.Size = new System.Drawing.Size(117, 22);
            this.closeFile2.Text = "Close";
            // 
            // clearFile2
            // 
            this.clearFile2.Name = "clearFile2";
            this.clearFile2.Size = new System.Drawing.Size(117, 22);
            this.clearFile2.Text = "Clear";
            // 
            // parseCompileMenu2
            // 
            this.parseCompileMenu2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.compileSelectedToolStripMenuItem1,
            this.compileAllToolStripMenuItem1});
            this.parseCompileMenu2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.parseCompileMenu2.Name = "parseCompileMenu2";
            this.parseCompileMenu2.Size = new System.Drawing.Size(108, 20);
            this.parseCompileMenu2.Text = "Parse + Compile";
            // 
            // compileSelectedToolStripMenuItem1
            // 
            this.compileSelectedToolStripMenuItem1.Name = "compileSelectedToolStripMenuItem1";
            this.compileSelectedToolStripMenuItem1.Size = new System.Drawing.Size(171, 22);
            this.compileSelectedToolStripMenuItem1.Text = "Compile Selected";
            this.compileSelectedToolStripMenuItem1.Click += new System.EventHandler(this.parseCompile2_Click);
            // 
            // compileAllToolStripMenuItem1
            // 
            this.compileAllToolStripMenuItem1.Name = "compileAllToolStripMenuItem1";
            this.compileAllToolStripMenuItem1.Size = new System.Drawing.Size(171, 22);
            this.compileAllToolStripMenuItem1.Text = "Compile All";
            this.compileAllToolStripMenuItem1.Click += new System.EventHandler(this.parseCompileAll2_Click);
            // 
            // executeMenu2
            // 
            this.executeMenu2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.executeFM2});
            this.executeMenu2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.executeMenu2.Name = "executeMenu2";
            this.executeMenu2.Size = new System.Drawing.Size(64, 20);
            this.executeMenu2.Text = "Execute";
            // 
            // executeFM2
            // 
            this.executeFM2.Name = "executeFM2";
            this.executeFM2.Size = new System.Drawing.Size(205, 22);
            this.executeFM2.Text = "Execute FM (Recursive)";
            this.executeFM2.Click += new System.EventHandler(this.executeFMToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.gridPosition);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.gridPositionContent);
            this.splitContainer1.Size = new System.Drawing.Size(447, 252);
            this.splitContainer1.SplitterDistance = 224;
            this.splitContainer1.TabIndex = 28;
            // 
            // gridPositionContent
            // 
            this.gridPositionContent.AllowUserToAddRows = false;
            this.gridPositionContent.AllowUserToDeleteRows = false;
            this.gridPositionContent.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.gridPositionContent.BackgroundColor = System.Drawing.Color.AntiqueWhite;
            this.gridPositionContent.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridPositionContent.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.gridPositionContent.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.gridPositionContent.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn4});
            this.gridPositionContent.ContextMenuStrip = this.contextMenuStrip3;
            this.gridPositionContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPositionContent.Location = new System.Drawing.Point(0, 0);
            this.gridPositionContent.Name = "gridPositionContent";
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.AntiqueWhite;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.DarkOrange;
            this.gridPositionContent.RowsDefaultCellStyle = dataGridViewCellStyle7;
            this.gridPositionContent.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPositionContent.Size = new System.Drawing.Size(219, 252);
            this.gridPositionContent.TabIndex = 24;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.Frozen = true;
            this.dataGridViewTextBoxColumn4.HeaderText = "Contract ID";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.ToolTipText = "Content of Position (Primary Contract Exposures)";
            this.dataGridViewTextBoxColumn4.Width = 176;
            // 
            // contextMenuStrip3
            // 
            this.contextMenuStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeSelectedToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.contextMenuStrip3.Name = "contextMenuStrip3";
            this.contextMenuStrip3.Size = new System.Drawing.Size(165, 48);
            // 
            // removeSelectedToolStripMenuItem
            // 
            this.removeSelectedToolStripMenuItem.Name = "removeSelectedToolStripMenuItem";
            this.removeSelectedToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.removeSelectedToolStripMenuItem.Text = "Remove Selected";
            this.removeSelectedToolStripMenuItem.Click += new System.EventHandler(this.removeSelectedToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.8F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.2F));
            this.tableLayoutPanel1.Controls.Add(this.toolStripContainer2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.toolStripContainer3, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.toolStripContainer4, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.tabControl3, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38.2F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 61.8F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1184, 742);
            this.tableLayoutPanel1.TabIndex = 29;
            // 
            // toolStripContainer2
            // 
            // 
            // toolStripContainer2.ContentPanel
            // 
            this.toolStripContainer2.ContentPanel.Controls.Add(this.tabControl1);
            this.toolStripContainer2.ContentPanel.Size = new System.Drawing.Size(725, 428);
            this.toolStripContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer2.Location = new System.Drawing.Point(3, 286);
            this.toolStripContainer2.Name = "toolStripContainer2";
            this.toolStripContainer2.Size = new System.Drawing.Size(725, 453);
            this.toolStripContainer2.TabIndex = 31;
            this.toolStripContainer2.Text = "toolStripContainer2";
            // 
            // toolStripContainer2.TopToolStripPanel
            // 
            this.toolStripContainer2.TopToolStripPanel.Controls.Add(this.toolStripProgress);
            // 
            // toolStripProgress
            // 
            this.toolStripProgress.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStripProgress.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buttonOutput,
            this.toolStripSeparator1,
            this.InfoNumberThreads,
            this.toolStripSeparator2,
            this.InfoNumberTasks,
            this.toolStripSeparator3});
            this.toolStripProgress.Location = new System.Drawing.Point(3, 0);
            this.toolStripProgress.Name = "toolStripProgress";
            this.toolStripProgress.Size = new System.Drawing.Size(366, 25);
            this.toolStripProgress.TabIndex = 0;
            // 
            // buttonOutput
            // 
            this.buttonOutput.CheckOnClick = true;
            this.buttonOutput.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.buttonOutput.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonOutput.Name = "buttonOutput";
            this.buttonOutput.Size = new System.Drawing.Size(132, 22);
            this.buttonOutput.Text = "Info while processing...";
            this.buttonOutput.Click += new System.EventHandler(this.buttonOutput_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // InfoNumberThreads
            // 
            this.InfoNumberThreads.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InfoNumberThreads.Name = "InfoNumberThreads";
            this.InfoNumberThreads.ReadOnly = true;
            this.InfoNumberThreads.Size = new System.Drawing.Size(100, 25);
            this.InfoNumberThreads.Click += new System.EventHandler(this.InfoNumberThreads_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // InfoNumberTasks
            // 
            this.InfoNumberTasks.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InfoNumberTasks.Name = "InfoNumberTasks";
            this.InfoNumberTasks.ReadOnly = true;
            this.InfoNumberTasks.Size = new System.Drawing.Size(100, 25);
            this.InfoNumberTasks.Click += new System.EventHandler(this.InfoNumberTasks_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripContainer3
            // 
            // 
            // toolStripContainer3.ContentPanel
            // 
            this.toolStripContainer3.ContentPanel.Controls.Add(this.splitContainer1);
            this.toolStripContainer3.ContentPanel.Size = new System.Drawing.Size(447, 252);
            this.toolStripContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer3.Location = new System.Drawing.Point(734, 3);
            this.toolStripContainer3.Name = "toolStripContainer3";
            this.toolStripContainer3.Size = new System.Drawing.Size(447, 277);
            this.toolStripContainer3.TabIndex = 32;
            this.toolStripContainer3.Text = "toolStripContainer3";
            // 
            // toolStripContainer3.TopToolStripPanel
            // 
            this.toolStripContainer3.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.txtbxCurrentContract,
            this.bttnFreeze});
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(249, 25);
            this.toolStrip1.TabIndex = 0;
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(135, 22);
            this.toolStripLabel1.Text = "Current Treaty Contract:";
            // 
            // txtbxCurrentContract
            // 
            this.txtbxCurrentContract.BackColor = System.Drawing.SystemColors.Window;
            this.txtbxCurrentContract.Name = "txtbxCurrentContract";
            this.txtbxCurrentContract.ReadOnly = true;
            this.txtbxCurrentContract.Size = new System.Drawing.Size(100, 25);
            this.txtbxCurrentContract.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // bttnFreeze
            // 
            this.bttnFreeze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bttnFreeze.Image = ((System.Drawing.Image)(resources.GetObject("bttnFreeze.Image")));
            this.bttnFreeze.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bttnFreeze.Name = "bttnFreeze";
            this.bttnFreeze.Size = new System.Drawing.Size(44, 22);
            this.bttnFreeze.Text = "Freeze";
            this.bttnFreeze.ToolTipText = "Push to freeze Positions view of current Treaty Contract";
            this.bttnFreeze.Visible = false;
            this.bttnFreeze.Click += new System.EventHandler(this.bttnFreeze_Click);
            // 
            // toolStripContainer4
            // 
            // 
            // toolStripContainer4.ContentPanel
            // 
            this.toolStripContainer4.ContentPanel.Controls.Add(this.splitContainer2);
            this.toolStripContainer4.ContentPanel.Size = new System.Drawing.Size(447, 428);
            this.toolStripContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer4.Location = new System.Drawing.Point(734, 286);
            this.toolStripContainer4.Name = "toolStripContainer4";
            this.toolStripContainer4.Size = new System.Drawing.Size(447, 453);
            this.toolStripContainer4.TabIndex = 33;
            this.toolStripContainer4.Text = "toolStripContainer4";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.textNodeResults);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.treeTreaty);
            this.splitContainer2.Size = new System.Drawing.Size(447, 428);
            this.splitContainer2.SplitterDistance = 25;
            this.splitContainer2.TabIndex = 0;
            // 
            // textNodeResults
            // 
            this.textNodeResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textNodeResults.Location = new System.Drawing.Point(0, 0);
            this.textNodeResults.Name = "textNodeResults";
            this.textNodeResults.Size = new System.Drawing.Size(447, 20);
            this.textNodeResults.TabIndex = 0;
            // 
            // treeTreaty
            // 
            this.treeTreaty.AllowDrop = true;
            this.treeTreaty.BackColor = System.Drawing.Color.AntiqueWhite;
            this.treeTreaty.CheckBoxes = true;
            this.treeTreaty.Cursor = System.Windows.Forms.Cursors.Hand;
            this.treeTreaty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeTreaty.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeTreaty.ForeColor = System.Drawing.Color.Navy;
            this.treeTreaty.HideSelection = false;
            this.treeTreaty.HotTracking = true;
            this.treeTreaty.Location = new System.Drawing.Point(0, 0);
            this.treeTreaty.Name = "treeTreaty";
            this.treeTreaty.Size = new System.Drawing.Size(447, 399);
            this.treeTreaty.TabIndex = 0;
            // 
            // tabControl3
            // 
            this.tabControl3.Controls.Add(this.tabID1);
            this.tabControl3.Controls.Add(this.tabID2);
            this.tabControl3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl3.Location = new System.Drawing.Point(3, 3);
            this.tabControl3.Name = "tabControl3";
            this.tabControl3.SelectedIndex = 0;
            this.tabControl3.Size = new System.Drawing.Size(725, 277);
            this.tabControl3.TabIndex = 34;
            this.tabControl3.Click += new System.EventHandler(this.tabControl3_Click);
            // 
            // tabID1
            // 
            this.tabID1.Controls.Add(this.toolStripContainer1);
            this.tabID1.Location = new System.Drawing.Point(4, 22);
            this.tabID1.Name = "tabID1";
            this.tabID1.Padding = new System.Windows.Forms.Padding(3);
            this.tabID1.Size = new System.Drawing.Size(717, 251);
            this.tabID1.TabIndex = 0;
            this.tabID1.Text = "Primary Contracts";
            this.tabID1.UseVisualStyleBackColor = true;
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.gridConExp1);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(711, 221);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(3, 3);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(711, 245);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
            // 
            // tabID2
            // 
            this.tabID2.Controls.Add(this.toolStripContainer5);
            this.tabID2.Location = new System.Drawing.Point(4, 22);
            this.tabID2.Name = "tabID2";
            this.tabID2.Padding = new System.Windows.Forms.Padding(3);
            this.tabID2.Size = new System.Drawing.Size(717, 251);
            this.tabID2.TabIndex = 1;
            this.tabID2.Text = "Treaty Contracts";
            this.tabID2.UseVisualStyleBackColor = true;
            // 
            // toolStripContainer5
            // 
            // 
            // toolStripContainer5.ContentPanel
            // 
            this.toolStripContainer5.ContentPanel.Controls.Add(this.gridTreaty2);
            this.toolStripContainer5.ContentPanel.Size = new System.Drawing.Size(711, 221);
            this.toolStripContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer5.Location = new System.Drawing.Point(3, 3);
            this.toolStripContainer5.Name = "toolStripContainer5";
            this.toolStripContainer5.Size = new System.Drawing.Size(711, 245);
            this.toolStripContainer5.TabIndex = 0;
            this.toolStripContainer5.Text = "toolStripContainer5";
            // 
            // toolStripContainer5.TopToolStripPanel
            // 
            this.toolStripContainer5.TopToolStripPanel.Controls.Add(this.menuStrip2);
            // 
            // BottomToolStripPanel
            // 
            this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // TopToolStripPanel
            // 
            this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // RightToolStripPanel
            // 
            this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Size = new System.Drawing.Size(586, 277);
            // 
            // NGFMPrototypeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1184, 742);
            this.Controls.Add(this.tableLayoutPanel1);
            this.HelpButton = true;
            this.MainMenuStrip = this.menuStrip2;
            this.Name = "NGFMPrototypeForm";
            this.Text = "NGFM Prototype";
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.treeListView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.scheduleTreeListView1)).EndInit();
            this.contextMenuStrip4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridConExp1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabSDL1.ResumeLayout(false);
            this.tabSDL1.PerformLayout();
            this.tabIR1.ResumeLayout(false);
            this.tabIR1.PerformLayout();
            this.tabTreeView1.ResumeLayout(false);
            this.tabScedule1.ResumeLayout(false);
            this.tabCOL1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridCOL1)).EndInit();
            this.tabTimeSeries1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridTimeSeries1)).EndInit();
            this.contextMenuStrip2.ResumeLayout(false);
            this.tabBugLog1.ResumeLayout(false);
            this.tabBugLog1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTreaty2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPosition)).EndInit();
            this.menuStrip2.ResumeLayout(false);
            this.menuStrip2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPositionContent)).EndInit();
            this.contextMenuStrip3.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.toolStripContainer2.ContentPanel.ResumeLayout(false);
            this.toolStripContainer2.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer2.TopToolStripPanel.PerformLayout();
            this.toolStripContainer2.ResumeLayout(false);
            this.toolStripContainer2.PerformLayout();
            this.toolStripProgress.ResumeLayout(false);
            this.toolStripProgress.PerformLayout();
            this.toolStripContainer3.ContentPanel.ResumeLayout(false);
            this.toolStripContainer3.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer3.TopToolStripPanel.PerformLayout();
            this.toolStripContainer3.ResumeLayout(false);
            this.toolStripContainer3.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.toolStripContainer4.ContentPanel.ResumeLayout(false);
            this.toolStripContainer4.ResumeLayout(false);
            this.toolStripContainer4.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl3.ResumeLayout(false);
            this.tabID1.ResumeLayout(false);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.tabID2.ResumeLayout(false);
            this.toolStripContainer5.ContentPanel.ResumeLayout(false);
            this.toolStripContainer5.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer5.TopToolStripPanel.PerformLayout();
            this.toolStripContainer5.ResumeLayout(false);
            this.toolStripContainer5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtbxCDL1;
        private System.Windows.Forms.TextBox txtbxIR1;
        private BrightIdeasSoftware.TreeListView treeListView1;
        private BrightIdeasSoftware.TreeListView scheduleTreeListView1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem addToPosition1;
        private ToolStripMenuItem rewritePosition1;
        private ToolStripMenuItem delete1;
        private TabControl tabControl1;
        private TabPage tabSDL1;
        private TabPage tabIR1;
        private TabPage tabTreeView1;
        private TabPage tabScedule1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileMenu1;
        private ToolStripMenuItem openFile1;
        private ToolStripMenuItem saveFile1;
        private ToolStripMenuItem saveasFile1;
        private ToolStripMenuItem closeFile1;
        private ToolStripMenuItem executeMenu1;
        private ToolStripMenuItem executeFM1;
        private ToolStripMenuItem parseCompileMenu1;
        private MenuStrip menuStrip2;
        private ToolStripMenuItem fileMenu2;
        private ToolStripMenuItem openFile2;
        private ToolStripMenuItem saveFile2;
        private ToolStripMenuItem saveasFile2;
        private ToolStripMenuItem closeFile2;
        private ToolStripMenuItem clearFile2;
        private ToolStripMenuItem parseCompileMenu2;
        private ToolStripMenuItem executeMenu2;
        public DataGridView gridConExp1;
        private ToolStripMenuItem compileSelectedToolStripMenuItem;
        private ToolStripMenuItem compileAllToolStripMenuItem;
        private ToolStripMenuItem compileSelectedToolStripMenuItem1;
        private ToolStripMenuItem compileAllToolStripMenuItem1;
        private ContextMenuStrip contextMenuStrip2;
        private ToolStripMenuItem addNew2;
        private ToolStripMenuItem removeSelected2;
        private ToolStripMenuItem addFile1;
        public DataGridView gridTreaty2;
        public DataGridView gridPosition;
        private TabPage tabCOL1;
        public DataGridView gridCOL1;
        private SplitContainer splitContainer1;
        private TableLayoutPanel tableLayoutPanel1;
        private ToolStripContainer toolStripContainer2;
        private ToolStripContainer toolStripContainer3;
        private ToolStripContainer toolStripContainer4;
        private ToolStripMenuItem gULossToolStripMenuItem;
        private ToolStripMenuItem fromfileGULoss1;
        private ToolStripMenuItem simulateGULoss1;
        private TabPage tabTimeSeries1;
        public DataGridView gridTimeSeries1;
        public DataGridView gridPositionContent;
        private TabControl tabControl3;
        private TabPage tabID1;
        private ToolStripContainer toolStripContainer1;
        private TabPage tabID2;
        private ToolStripContainer toolStripContainer5;
        private ToolStripPanel BottomToolStripPanel;
        private ToolStripPanel TopToolStripPanel;
        private ToolStripPanel RightToolStripPanel;
        private ToolStripPanel LeftToolStripPanel;
        private ToolStripContentPanel ContentPanel;
        private SplitContainer splitContainer2;
        private TextBox textNodeResults;
        private TreeView treeTreaty;
        private DataGridViewTextBoxColumn COL1;
        private DataGridViewCheckBoxColumn COL1Check;
        private ContextMenuStrip contextMenuStrip3;
        private ToolStripMenuItem removeSelectedToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private DataGridViewTextBoxColumn Position;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private ContextMenuStrip contextMenuStrip4;
        private ToolStripMenuItem saveScheduleToFileToolStripMenuItem;
        private DataGridViewTextBoxColumn time1;
        private DataGridViewTextBoxColumn guLoss;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private ToolStrip toolStrip1;
        private ToolStripLabel toolStripLabel1;
        private ToolStripTextBox txtbxCurrentContract;
        private ToolStripButton bttnFreeze;
        private ToolStripMenuItem appendToPositionToolStripMenuItem;
        private ToolStripMenuItem updatePositionToolStripMenuItem;
        private TabPage tabBugLog1;
        private TextBox textbxBugLog1;
        private ToolStripMenuItem executeFM2;
        private ToolStripMenuItem addToolStripMenuItem;
        private ToolStripMenuItem createCopiesToolStripMenuItem;
        private ToolStripMenuItem simulateRandomToolStripMenuItem;
        private ToolStripMenuItem saveToFileToolStripMenuItem;
        private DataGridViewTextBoxColumn PrimeID;
        private DataGridViewTextBoxColumn Payout;
        private DataGridViewTextBoxColumn ProcessInfo;
        private DataGridViewTextBoxColumn TreatyId;
        private DataGridViewTextBoxColumn TreatyPayout;
        private DataGridViewTextBoxColumn TreatyProcessInfo;
        private ToolStrip toolStripProgress;
        private ToolStripButton buttonOutput;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripTextBox InfoNumberThreads;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripTextBox InfoNumberTasks;
        private ToolStripSeparator toolStripSeparator3;
    }
}

