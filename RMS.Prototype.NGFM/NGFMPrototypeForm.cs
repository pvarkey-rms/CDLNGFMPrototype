using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BrightIdeasSoftware;
using RMS.ContractGraphModel;
using PrototypeContract = RMS.ContractObjectModel.Contract;
using CompiledResult = System.Tuple<string, RMS.ContractObjectModel.Contract, RMS.ContractGraphModel.IContractGraph>;
using RMS.ContractObjectModel;
using Rms.DataServices.DataObjects.CDL;
using Rms.DataServices.DataObjects;
using Rms.Cdl.DataObjects.DataObjects;
using Rms.Utilities;
using System.Diagnostics;

using Noesis.Javascript;

namespace RMS.Prototype.NGFM
{
    public partial class NGFMPrototypeForm : Form
    {
        public abstract class Variant
        {
            #region Fields

            public long SelectedId = 0;
            public string SaveFileName { set; get; }
            private bool IsCOLChecked { set; get; }

            public DataGridView GridID { set; get; }
            public DataGridView GridCOL { set; get; }
            public DataGridView GridTimeSeries { set; get; }

            public TextBox TextBoxCDL { set; get; }
            public TextBox TextBoxIR { set; get; }
            public TextBox TextBoxBugLog { set; get; }

            public TreeListView TreeSchedule { set; get; }
            public TreeListView TreeView { set; get; }

            public MenuStrip Menu { set; get; }
            public ToolStripMenuItem BtnExecuteFM { set; get; }

            ToolStrip MenuProgress { set; get; }
            ToolStripTextBox InfoNumberThreads { set; get; }
            ToolStripTextBox InfoNumberTasks { set; get; }

            #endregion

            #region Static Fields

            protected static NGFMPrototype ngfmPrototype;
            protected static bool simulate = true;
            protected bool IsFirstUpload = true;
            protected static ContractExposureData GetContractData(long id)
            {
                ContractExposureData ced = null;
                lock (ngfmPrototype)
                    ced = ngfmPrototype.GetContractData(id);
                return ced;
            }
            protected static ResultPosition GetResultPosition(long id)
            {
                if (null != ngfmPrototype)
                {
                    ConcurrentDictionary<long, ResultPosition> res = null;
                    lock (ngfmPrototype)
                        res = ngfmPrototype.GetResultPositions(0, id);

                    if (res != null && res.ContainsKey(id))
                        return res[id];
                }
                return null;
            }
            protected static ConcurrentDictionary<long, List<TreeListModelNode>> CacheTreeSchedule { set; get; }
            protected static ConcurrentDictionary<long, List<TreeListModelNode>> CacheTreeView { set; get; }

            protected static int maxDegreeOfParallelism = 4;

            #region Task Process fields

            protected static long TaskQueueID = 0;//ID of Common Tasks: Upload files, Upload GULosses, 
            protected static long TaskInfoID = long.MinValue;//ID of Info update task
            protected static CancellationTokenSource cts = null;
            protected static void CancelJobs()
            {
                // cancel old jobs; re-create cancellation token
                if (cts != null)
                    cts.Cancel();
                cts = new CancellationTokenSource();
            }

            #endregion

            #endregion

            #region Constructor and Initialization

            public Variant(
                DataGridView gridID,
                DataGridView gridCOL,
                DataGridView gridTimeSeries,
                TextBox textBoxCDL,
                TextBox textBoxIR,
                TextBox textbxBugLog,
                TreeListView treeSchedule,
                TreeListView treeView,
                MenuStrip menu,
                ToolStripMenuItem executeFM,
                ToolStrip toolStripProgress,
                ToolStripTextBox infoNumberThreads,
                ToolStripTextBox infoNumberTasks)
            {
                TaskManager.Initialize(maxDegreeOfParallelism);

                if (null == ngfmPrototype)
                    ngfmPrototype = new NGFMPrototype();

                SelectedId = 0;
                SaveFileName = "";
                this.GridID = gridID;
                this.GridCOL = gridCOL;
                this.GridTimeSeries = gridTimeSeries;
                this.TextBoxCDL = textBoxCDL;
                this.TextBoxIR = textBoxIR;
                this.TextBoxBugLog = textbxBugLog;
                this.TreeSchedule = treeSchedule;
                SetScheduleTreeView(this.TreeSchedule);
                this.TreeView = treeView;
                SetExposureTreeView(this.TreeView);
                if (null == CacheTreeSchedule)
                    CacheTreeSchedule = new ConcurrentDictionary<long, List<TreeListModelNode>>();
                if (null == CacheTreeView)
                    CacheTreeView = new ConcurrentDictionary<long, List<TreeListModelNode>>();
                this.Menu = menu;
                this.BtnExecuteFM = executeFM;
                //IsAllChecked = false;
                IsCOLChecked = true;

                this.MenuProgress = toolStripProgress;
                this.InfoNumberThreads = infoNumberThreads;
                this.InfoNumberTasks = infoNumberTasks;
            }
            
            protected void SetExposureTreeView(BrightIdeasSoftware.TreeListView treeView)
            {   // CONTRACT-EXPOSURE TREELISTVIEW 
                treeView.Clear();
                // set the delegate that the tree uses to know if a node is expandable
                treeView.CanExpandGetter = x => (x as TreeListModelNode).Children.Count > 0;
                // set the delegate that the tree uses to know the children of a node
                treeView.ChildrenGetter = x => (x as TreeListModelNode).Children;
                treeView.Scrollable = true;

                // create the tree columns and set the delegates to print the desired object proerty
                var propCol = new BrightIdeasSoftware.OLVColumn("Property", "Property");
                propCol.FillsFreeSpace = true;
                propCol.AspectGetter = x => (x as TreeListModelNode).Property;

                var valCol = new BrightIdeasSoftware.OLVColumn("Value", "Value");
                valCol.FillsFreeSpace = true;
                valCol.AspectGetter = x => (x as TreeListModelNode).Value;

                // add the columns to the tree
                treeView.Columns.Add(propCol);
                treeView.Columns.Add(valCol);

                treeView.Invalidate();
            }

            protected void SetScheduleTreeView(BrightIdeasSoftware.TreeListView treeView)
            {   // SCHEDULE TREELISTVIEW
                treeView.Clear();
                // set the delegate that the tree uses to know if a node is expandable
                treeView.CanExpandGetter = x => (x as TreeListModelNode).Children.Count > 0;
                // set the delegate that the tree uses to know the children of a node
                treeView.ChildrenGetter = x => (x as TreeListModelNode).Children;
                treeView.Scrollable = true;

                // create the tree columns and set the delegates to print the desired object property
                var schedCol = new BrightIdeasSoftware.OLVColumn("Schedule", "Schedule");
                schedCol.FillsFreeSpace = false;
                schedCol.Width = (int)(treeView.Width * 0.25);
                schedCol.AspectGetter = x => (x as TreeListModelNode).Property;

                var typeCol = new BrightIdeasSoftware.OLVColumn("Type / Time, GU LOSS, COL(s)", "Type");
                typeCol.FillsFreeSpace = true;
                typeCol.Width = (int)(treeView.Width * 0.25);
                typeCol.AspectGetter = x => (x as TreeListModelNode).Value;

                // add the columns to the tree
                treeView.Columns.Add(schedCol);
                treeView.Columns.Add(typeCol);

                treeView.Invalidate();
            }

            #endregion

            #region ProcessInfo

            protected void UpdateProcessInfo(Dictionary<long, Tuple<int, string>> infoDict, Dictionary<long, double> payOuts)
            {
                if (this.GridID.InvokeRequired)
                    this.GridID.Invoke(new Action<Dictionary<long, Tuple<int, string>>, Dictionary<long, double>>(UpdateProcessInfo), infoDict, payOuts);
                else if (null != this.GridID.Rows && this.GridID.Rows.Count > 0 && null != infoDict && infoDict.Count > 0)
                {
                    for (int idx = 0; idx < this.GridID.Rows.Count; idx++)
                    {
                        long id = Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, idx])).Value);

                        if (infoDict.ContainsKey(id))
                        {
                            #region Update Status Info Cell

                            DataGridViewTextBoxCell cell = (DataGridViewTextBoxCell)(this.GridID[2, idx]);

                            switch (infoDict[id].Item1)
                            {
                                case 1://None
                                    cell.Style.Font = new Font(cell.InheritedStyle.Font, FontStyle.Regular);
                                    cell.Style.ForeColor = Color.Black;
                                    break;
                                case 0://Done
                                    cell.Style.Font = new Font(cell.InheritedStyle.Font, FontStyle.Regular);
                                    cell.Style.ForeColor = Color.Blue;
                                    break;
                                case -1://Failed
                                    cell.Style.Font = new Font(cell.InheritedStyle.Font, FontStyle.Regular);
                                    cell.Style.ForeColor = Color.Red;
                                    break;
                                case 2://Processing
                                    cell.Style.Font = new Font(cell.InheritedStyle.Font, FontStyle.Italic);
                                    cell.Style.ForeColor = Color.Green;
                                    break;
                                default:
                                    cell.Style.Font = new Font(cell.InheritedStyle.Font, FontStyle.Regular);
                                    cell.Style.ForeColor = Color.Black;
                                    break;
                            }
                            cell.Value = infoDict[id].Item2;

                            #endregion
                        }

                        if(payOuts != null && payOuts.ContainsKey(id))
                            ((DataGridViewTextBoxCell)(this.GridID[1, idx])).Value = string.Format("{0:f}", payOuts[id]);
                    }
                    this.GridID.Update();
                }
            }
            public static void UpdateProcessInfo(int tabIdx, VariantPrimary vp, VariantTreaty vt, Dictionary<long, Task> tasks)
            {
                Action<object> action = (obj) =>
                {
                    Dictionary<long, Tuple<int, string>> infoDict = null, infoDict1 = null, infoDict2 = null;
                    bool currentPopulated = false;
                    bool currentCDLPopulated = false;
                    List<long> ids = null;
                    do
                    {
                        vp.InfoNamberThreadsAndTasks();

                        ids = tasks.Where(kv => !TaskManager.IsTaskCompleted(kv.Value)).Select(kv => kv.Key).ToList();

                        #region Populate tables for carrent Contract

                        if (!currentPopulated)
                        {
                            if (tabIdx == 1)
                            {
                                var ced = GetContractData(vp.SelectedId);
                                if (!ids.Contains(vp.SelectedId))
                                {
                                    vp.Populate(ced);
                                    currentPopulated = true;
                                }
                                else if (!currentCDLPopulated)
                                {
                                    vp.PopulateCDL(ced);
                                    vp.PopulateTreeList(ced);
                                    currentCDLPopulated = true;
                                }
                            }
                            else
                            {
                                var ced = GetContractData(vt.SelectedId);
                                if (!ids.Contains(vt.SelectedId))
                                {
                                    vt.Populate(ced);
                                    currentPopulated = true;
                                }
                                else if (!currentCDLPopulated)
                                {
                                    vt.PopulateCDL(ced);
                                    vt.PopulateTreeList(ced);
                                    currentCDLPopulated = true;
                                }
                            }
                        }

                        #endregion

                        Dictionary<long, double> payOuts = null;
                        lock (ngfmPrototype)
                        {
                            infoDict2 = ngfmPrototype.CacheContractData.ToDictionary(kv => kv.Key, kv => kv.Value.StateToString());
                            if (ngfmPrototype.CacheEventIdContractIdToResult.ContainsKey(0))
                                payOuts = ngfmPrototype.CacheEventIdContractIdToResult[0].
                                    Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value.PayOut);
                        }

                        infoDict = (infoDict1 == null) ? infoDict2
                            : infoDict2.Where(kv => !infoDict1.ContainsKey(kv.Key) || !infoDict1[kv.Key].Equals(kv.Value))
                            .ToDictionary(kv => kv.Key, kv => kv.Value);

                        if (infoDict.Count > 0)
                        {
                            vp.UpdateProcessInfo(infoDict, payOuts);
                            vt.UpdateProcessInfo(infoDict, payOuts);
                        }

                        infoDict1 = infoDict2;
                    }
                    while (ids != null && ids.Count > 0);
                    vp.InfoNamberThreadsAndTasks();
                };

                TaskManager.Start(TaskInfoID, 0, action, TaskInfoID);
            }
            public static void UpdateProcessInfo(int tabIdx, VariantPrimary vp, VariantTreaty vt)
            {
                Action<object> action = (obj) =>
                {
                    #region Populate tables for carrent Contract

                    Variant v;
                    if (tabIdx == 1)
                        v = vp;
                    else v = vt;

                    var ced = GetContractData(v.SelectedId);
                    v.PopulateScheduleTree(ced, true);
                    v.PopulateTimeSeries(ced);

                    #endregion

                    #region Update Process Info

                    Dictionary<long, Tuple<int, string>> infoDict = null;
                    lock (ngfmPrototype)
                        infoDict = ngfmPrototype.CacheContractData.ToDictionary(kv => kv.Key, kv => kv.Value.StateToString());

                    if (infoDict != null && infoDict.Count > 0)
                    {
                        vp.UpdateProcessInfo(infoDict, null);
                        vt.UpdateProcessInfo(infoDict, null);
                    }

                    #endregion

                    vp.InfoNamberThreadsAndTasks();

                };

                TaskManager.Start(TaskInfoID, 0, action, TaskInfoID);
            }

            public void InfoNamberThreadsAndTasks()
            {
                if (this.MenuProgress.InvokeRequired)
                    this.MenuProgress.Invoke(new Action(InfoNamberThreadsAndTasks));
                else
                {
                    this.InfoNumberThreads.Text = string.Format("Threads:{0}", TaskManager.NumberThreads);

                    int nTasks = TaskManager.GetNumberOfRunningTasksAndRemoveCompleted();
                    this.InfoNumberTasks.Text = string.Format("Tasks:{0}",nTasks);
                }
            }

            #endregion

            #region PayOut

            protected void ClearPayOut(params long[] ids)
            {
                if (this.GridID.InvokeRequired)
                    this.GridID.Invoke(new Action<long[]>(ClearPayOut), ids);
                else if (null != this.GridID.Rows && this.GridID.Rows.Count > 0)
                {
                    if (ids.Length == 0)
                    {
                        for (int idx = 0; idx < this.GridID.Rows.Count; idx++)
                        {
                            ((DataGridViewTextBoxCell)(this.GridID[1, idx])).Value = "";
                            ((DataGridViewTextBoxCell)(this.GridID[2, idx])).Value = "";
                        }
                    }
                    else
                    {
                        for (int idx = 0; idx < this.GridID.Rows.Count; idx++)
                        {
                            long id = Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, idx])).Value);
                            if (ids.Contains(id))
                            {
                                ((DataGridViewTextBoxCell)(this.GridID[1, idx])).Value = "";
                                ((DataGridViewTextBoxCell)(this.GridID[2, idx])).Value = "";
                            }
                        }
                    }
                    this.GridID.Update();
                }
            }

            #endregion

            #region Common

            protected void SetBackgroundColor(TextBox tbx, Color col)
            {
                if (tbx.InvokeRequired)
                    tbx.Invoke(new Action<TextBox, Color>(SetBackgroundColor), tbx, col);
                else
                {
                    tbx.BackColor = col;
                    tbx.Invalidate();
                }
            }
            protected void SetBackgroundColor(TreeListView tlv, Color col)
            {
                if (tlv.InvokeRequired)
                    tlv.Invoke(new Action<TreeListView, Color>(SetBackgroundColor), tlv, col);
                else
                {
                    tlv.BackColor = col;
                    tlv.Invalidate();
                }
            }
            protected void SetBackgroundColor(DataGridView dgv, Color col)
            {
                if (dgv.InvokeRequired)
                    dgv.Invoke(new Action<DataGridView, Color>(SetBackgroundColor), dgv, col);
                else
                {
                    dgv.BackgroundColor = col;

                    #region Cell Style
                    var cellStyle = new DataGridViewCellStyle();
                    cellStyle.BackColor = col;
                    cellStyle.ForeColor = Color.Black;
                    if (col == Color.AntiqueWhite)
                        cellStyle.SelectionBackColor = Color.DarkOrange;
                    else if (col == Color.PowderBlue)
                        cellStyle.SelectionBackColor = Color.DeepSkyBlue;
                    else //if (col == Color.White)
                        cellStyle.SelectionBackColor = Color.Gray;
                    dgv.RowsDefaultCellStyle = cellStyle;
                    #endregion

                    #region Header Style
                    //if (col == Color.AntiqueWhite)
                    //{
                    //    dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Orange;
                    //    dgv.RowHeadersDefaultCellStyle.BackColor = Color.Orange;
                    //    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.DarkOrange;
                    //    dgv.RowHeadersDefaultCellStyle.SelectionBackColor = Color.DarkOrange;
                    //}
                    //else
                    //{
                    //    dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkGray;
                    //    dgv.RowHeadersDefaultCellStyle.BackColor = Color.DarkGray;
                    //    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Gray;
                    //    dgv.RowHeadersDefaultCellStyle.SelectionBackColor = Color.Gray;
                    //}
                    //dgv.EnableHeadersVisualStyles = false;
                    #endregion

                    dgv.Update();
                }
            }
            private void PopulateTxtbx(TextBox tbx, string str)
            {
                if (tbx.InvokeRequired)
                    tbx.Invoke(new Action<TextBox, string>(PopulateTxtbx), tbx, str);
                else
                {
                    tbx.Text = (!string.IsNullOrEmpty(str)) ? str : "";
                    tbx.Invalidate();
                }
            }
            private string GetTxtbx(TextBox tbx)
            {
                if (tbx.InvokeRequired)
                    return (string)(tbx.Invoke(new Func<TextBox, string>(GetTxtbx), tbx));
                else
                    return tbx.Text;
            }
            protected void ClearGrid(DataGridView dgv)
            {
                if (dgv.InvokeRequired)
                    dgv.Invoke(new Action<DataGridView>(ClearGrid), dgv);
                else if (null != dgv.Rows && dgv.Rows.Count > 0)
                {
                    dgv.Rows.Clear();
                    dgv.Update();
                }
            }
            protected long[] Get(DataGridView dgv)
            {
                if (dgv.InvokeRequired)
                    return (long[])(dgv.Invoke(new Func<DataGridView, long[]>(Get), dgv));
                else if (null != dgv.Rows && dgv.Rows.Count > 0)
                {
                    return Enumerable.Range(0, dgv.Rows.Count).Select(id => (int)id)
                        .Select(idx => Convert.ToInt64(((DataGridViewTextBoxCell)(dgv[0, idx])).Value)).ToArray();
                }
                else { return new long[] { }; }
            }
            protected void Populate(DataGridView dgv, params long[] ids)
            {
                if (dgv.InvokeRequired)
                    dgv.Invoke(new Action<DataGridView, long[]>(Populate), dgv, ids);
                else if (null != ids && ids.Length != 0)
                {
                    foreach (long id in ids)
                        dgv.Rows.Add(id.ToString());
                    dgv.Update();
                }
            }
            protected long GetSelected(DataGridView dgv)
            {
                if (dgv.InvokeRequired)
                    return (long)(dgv.Invoke(new Func<DataGridView, long>(GetSelected), dgv));
                else if (null != dgv.Rows && dgv.Rows.Count > 0)
                    return Convert.ToInt64(((DataGridViewTextBoxCell)(dgv[0, dgv.CurrentRow.Index])).Value);
                else return 0;
            }
            protected HashSet<long> RemoveMultiSelected(DataGridView dgv)
            {
                if (dgv.InvokeRequired)
                    return (HashSet<long>)(dgv.Invoke(new Func<DataGridView, HashSet<long>>(RemoveMultiSelected), dgv));
                else if (null != dgv.Rows && dgv.Rows.Count > 0)
                {
                    var rmv = new HashSet<long>();
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                    {
                        rmv.Add(Convert.ToInt64(((DataGridViewTextBoxCell)(dgv[0, row.Index])).Value));
                        dgv.Rows.RemoveAt(row.Index);
                    }
                    dgv.Update();
                    return rmv;
                }
                else { return new HashSet<long>(); }
            }
            protected void InvertChecked(DataGridView dgv)
            {
                if (dgv.InvokeRequired)
                    dgv.Invoke(new Action<DataGridView>(InvertChecked), dgv);
                else if (null != dgv.Rows && dgv.Rows.Count > 0)
                {
                    IsCOLChecked = !IsCOLChecked;
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        ((DataGridViewCheckBoxCell)row.Cells[1]).Value = IsCOLChecked;
                    }
                    dgv.Update();
                }
            }

            #endregion

            #region Read / Save File

            public static void UploadContractExposureDialog(int tabIdx, VariantPrimary vp, VariantTreaty vt, bool outputInProcess, bool append = false)
            {
                #region Open File Dialog

                string[] fileNames = null;

                // Create an instance of the open Contract file(s) dialog box.
                OpenFileDialog Dlg = new OpenFileDialog();
                Dlg.Filter = "RMS ContractExposure Extract (.dat, .dat2)|*.dat;*.dat2";
                Dlg.FilterIndex = 1;
                Dlg.CheckFileExists = false;
                Dlg.ValidateNames = false;
                Dlg.Multiselect = true;

                if (Dlg.ShowDialog() == DialogResult.OK)
                    fileNames = Dlg.FileNames;

                if (fileNames == null || fileNames.Count() == 0)
                    return;

                #endregion

                #region Filtering of new Files

                HashSet<string> newFiles = null;

                if (!append)
                {
                    TaskManager.CancelAll();
                    vp.ClearTotal();
                    vt.ClearTotal();
                    newFiles = new HashSet<string>(fileNames);
                }
                else
                {
                    newFiles = new HashSet<string>();
                    var realFiles = GetFileNames();
                    foreach (string fn in fileNames)
                        if (!realFiles.Contains(fn))
                            newFiles.Add(fn);
                }

                #endregion

                if (newFiles.Count() > 0)
                {
                    if (!append)
                    {
                        TaskManager.CancelAll();
                        ngfmPrototype = new NGFMPrototype();
                    }

                    Action<object> action = (obj) =>
                    {
                        List<long[]> ids = null;
                        lock (ngfmPrototype)
                            ids = ngfmPrototype.UploadContractExposures(newFiles.ToArray(), append, cts);

                        if (null != ids)
                        {
                            #region Populate Contract IDs

                            vp.UploadContractExposures(ids[0], tabIdx == 1, append);

                            if (ids[1].Count() != 0)
                                vt.UploadContractExposures(ids[1], tabIdx == 2, append);

                            #endregion

                            ContractExposureData.totalTime = new TimeSpan(0, 0, 0, 0, 0);

                            Dictionary<long, Task> tasks = null;
                            lock (ngfmPrototype)
                            {
                                ngfmPrototype.BuildParsingContext();
                                tasks = ngfmPrototype.Prepare_OLDAPI();
                            }

                            if (tasks != null)
                            {
                                if (outputInProcess)
                                    UpdateProcessInfo(tabIdx, vp, vt, tasks);
                                Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());
                                if (!outputInProcess)
                                    UpdateProcessInfo(tabIdx, vp, vt, tasks);
                                MessageBox.Show(null, string.Format("{0}", ContractExposureData.totalTime.ToString()), "Total Elapsed Time:");
                                UpdateProcessInfo(tabIdx, vp, vt);
                            }

                            lock (ngfmPrototype)
                            {
                                ngfmPrototype.DisposeParsingContext();
                            }
                        }

                    };

                    TaskManager.Start(TaskQueueID, 0, action, TaskQueueID);
                }
            }
            private void UploadContractExposures(long[] ids, bool needClear, bool append = false)
            {
                if (null != ids && ids.Count() > 0)
                {
                    int count = CountID();

                    if (!append && (count > 0) && needClear)
                        ClearTotal();

                    IsFirstUpload = !append || count == 0;

                    PopulateID(ids);
                }
            }
            protected static HashSet<string> GetFileNames()
            {
                lock (ngfmPrototype)
                    return ngfmPrototype.GetContractFileNames();
            }
            public void SaveScheduleTreeToFile()
            {
                string filePath = "";
                #region Save File Dialog
                // Create an instance of the open Contract file(s) dialog box.
                var Dlg = new SaveFileDialog();
                Dlg.Filter = "RMS ContractExposure Extract (.csv)|*.csv";
                Dlg.FilterIndex = 1;
                Dlg.CheckFileExists = false;
                Dlg.ValidateNames = false;
                Dlg.FileName = SaveFileName;

                if (Dlg.ShowDialog() == DialogResult.OK)
                {
                    filePath = Dlg.FileName;
                }
                #endregion

                if (string.IsNullOrEmpty(filePath))
                    return;

                var ced = GetContractData(this.SelectedId);
                if (null != ced && ced.ResolvedSchedule != null && ced.ResolvedSchedule.Count() > 0)
                {
                    #region Damage Ratios
                    Dictionary<long, Loss> dr = null;
                    if (null != ced._GULoss)
                    {
                        //dr = ced.GULosses.Values.Aggregate(new Dictionary<long, Loss>(), (a, b) =>
                        //{
                        //    foreach (var p in b)
                        //        if (!a.ContainsKey(p.Key))
                        //            a.Add(p.Key, p.Value);
                        //    return a;
                        //});
                        dr = ced._GULoss.FlattenedGULosses;
                    }
                    #endregion
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
                    {
                        file.WriteLine("Schedule,Timestamp,CoverageID,GroundUpLoss,CauseOfLosses");
                        foreach (var schedule in ced.ResolvedSchedule)
                        {
                            var ids = schedule.Value;

                            if (dr != null && null != ids)
                            {

                                var temp = dr.Where(elem => ids.Contains(elem.Key) && null != elem.Value)
                                    .Select(kv =>
                                            new Tuple<DateTime, string, string, string, string>
                                            (
                                                kv.Value.Timestamp,
                                                kv.Value.Timestamp.ToString("MM/dd/yyyy"),
                                                kv.Key.ToString(),
                                                kv.Value.Amount.ToString("#.##"),
                                                string.Join(";", kv.Value.CausesOfLoss.Select(col => col.ToString()).ToArray()))
                                            ).ToList();

                                temp.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                                foreach (var elem in temp)
                                {
                                    string row = string.Format("{0},{1},{2},{3},{4}", schedule.Key.ToString(), elem.Item2, elem.Item3, elem.Item4, elem.Item5);
                                    file.WriteLine(row);
                                }
                            }
                            else
                            {
                                foreach (long coverageId in schedule.Value)
                                    file.WriteLine(string.Format("{0},{1}", schedule.Key, coverageId.ToString()));
                            }
                        }
                    }
                }
            }
            public void SaveToFile(bool IsSaveAs = false)
            {
                #region Save File Dialog
                if (IsSaveAs || string.IsNullOrEmpty(SaveFileName) || !File.Exists(SaveFileName))
                {
                    // Create an instance of the open Contract file(s) dialog box.
                    var Dlg = new SaveFileDialog();
                    Dlg.Filter = "RMS ContractExposure Extract (.dat, .dat2)|*.dat;*.dat2";
                    Dlg.FilterIndex = 1;
                    Dlg.CheckFileExists = false;
                    Dlg.ValidateNames = false;
                    Dlg.FileName = SaveFileName;

                    if (Dlg.ShowDialog() == DialogResult.OK)
                    {
                        SaveFileName = Dlg.FileName;
                    }
                }
                #endregion

                var ced = GetContractData(this.SelectedId);
                // Save (CDL, COL, Positions)
                Save(ced);

                if (!string.IsNullOrEmpty(SaveFileName))
                {
                    lock (ngfmPrototype)
                        ngfmPrototype.SaveContractsToFile(SaveFileName);
                }
            }

            #endregion

            #region CDL, IR, BugLog

            protected void SetCDLBackgroundColor(Color col) { SetBackgroundColor(this.TextBoxCDL, col); }
            protected void SetIRBackgroundColor(Color col) { SetBackgroundColor(this.TextBoxIR, col); }
            protected void SetBugLogBackgroundColor(Color col) { SetBackgroundColor(this.TextBoxBugLog, col); }
            public string StrCDL
            {
                get { return GetTxtbx(this.TextBoxCDL); }
                set { PopulateTxtbx(this.TextBoxCDL, value); }
            }
            public string IR
            {
                get { return GetTxtbx(this.TextBoxIR); }
                set { PopulateTxtbx(this.TextBoxIR, value); }
            }
            public string BugLog
            {
                get { return GetTxtbx(this.TextBoxBugLog); }
                set { PopulateTxtbx(this.TextBoxBugLog, value); }
            }
            protected bool SaveCDL(ContractExposureData ced)
            {
                string strCDL = this.StrCDL;
                bool isChanged = (!string.IsNullOrEmpty(strCDL) && !strCDL.Equals(ced.strCDL));

                if (isChanged)
                {
                    ced.strCDL = strCDL;

                    var infoDict = new Dictionary<long, Tuple<int, string>>();
                    infoDict.Add(ced.Id, ced.StateToString());

                    UpdateProcessInfo(infoDict, null);
                }

                return isChanged;
            }
            protected void PopulateCDL(ContractExposureData ced)
            {
                this.StrCDL = (null != ced) ? ced.strCDL : "";
            }
            protected void PopulateIR(ContractExposureData ced)
            {
                this.IR = (null != ced) ? ced.IR : "";
            }
            protected void PopulateBugLog(ContractExposureData ced)
            {
                this.BugLog = (null != ced) ? ced.GetBugLog() : "";
            }

            #endregion

            #region Schedule Tree

            protected void SetScheduleBackgroundColor(Color col) { SetBackgroundColor(this.TreeSchedule, col); }
            
            protected void ClearScheduleTree()
            {
                if (this.TreeSchedule.InvokeRequired)
                    this.TreeSchedule.Invoke(new Action(ClearScheduleTree));
                else if (null != this.TreeSchedule.Roots)
                {
                    this.TreeSchedule.Roots = new List<TreeListModelNode>();
                    this.TreeSchedule.Invalidate();
                }
            }
            
            protected void RemoveFromCacheTreeSchedule()
            {
                List<TreeListModelNode> rmv;
                CacheTreeSchedule.TryRemove(this.SelectedId, out rmv);
            }
            
            protected virtual void PopulateScheduleTree(ContractExposureData ced, bool update = false)
            {
                if (this.TreeSchedule.InvokeRequired)
                    this.TreeSchedule.Invoke(new Action<ContractExposureData, bool>(PopulateScheduleTree), ced, update);
                else if (!update && CacheTreeSchedule.ContainsKey(this.SelectedId))//get from cache
                    this.TreeSchedule.Roots = CacheTreeSchedule[this.SelectedId];
                else if (null != ced && ced.Id == this.SelectedId && ced.CanShowSchedules())
                { //calculate and add to cache
                    long id = this.SelectedId;

                    var sTree = CreateScheduleTree(ced, update);

                    List<TreeListModelNode> rm;
                    if (CacheTreeSchedule.ContainsKey(id))
                        CacheTreeSchedule.TryRemove(id, out rm);

                    if (null != sTree && sTree.Count() > 0)
                    {
                        CacheTreeSchedule.TryAdd(id, sTree);//add to cache
                        if (id == this.SelectedId)
                        {
                            this.TreeSchedule.Roots = sTree;
                            //this.TreeSchedule.ExpandAll();
                            this.TreeSchedule.Invalidate();
                        }
                    }
                }
            }

            private List<TreeListModelNode> CreateScheduleTree(ContractExposureData ced, bool update = false)
            {
                var sTree = new List<TreeListModelNode>();

                if (ced.ResolvedSchedule != null && ced.ResolvedSchedule.Count() > 0)
                {
                    #region GULosses
                    Dictionary<long, Loss> guLoss = null;
                    if (null != ced._GULoss)
                    {
                        //guLoss = ced.GULosses.Values.Aggregate(new Dictionary<long, Loss>(), (a, b) =>
                        //{
                        //    foreach (var p in b)
                        //        if (!a.ContainsKey(p.Key))
                        //            a.Add(p.Key, p.Value);
                        //        else
                        //            a[p.Key].AddOrReplaceAmountByCOL(p.Value);
                        //    return a;
                        //});
                        guLoss = ced._GULoss.FlattenedGULosses;
                    }
                    #endregion

                    foreach (var schedule in ced.ResolvedSchedule)
                    {
                        var children = new List<TreeListModelNode>();
                        var ids = schedule.Value;

                        if (guLoss != null && null != ids)
                        {

                            var temp = guLoss.Where(elem => ids.Contains(elem.Key) && null != elem.Value)
                                .Select(kv =>
                                        new Tuple<DateTime, long, string>
                                        (
                                            kv.Value.Timestamp,
                                            kv.Key,
                                            string.Format("{0,-30}{1,-30}{2,-30}"
                                            , kv.Value.Timestamp.ToString("MM/dd/yyyy")
                                            , kv.Value.Amount.ToString("#.##")
                                            , string.Join(",", kv.Value.CausesOfLoss.Select(col => col.ToString()).ToArray()))
                                        )
                                ).ToList();

                            temp.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                            foreach (var elem in temp)
                            {
                                children.Add(new TreeListModelNode(elem.Item2.ToString(), elem.Item3));
                            }
                        }
                        else
                        {
                            foreach (long coverageId in schedule.Value)
                                children.Add(new TreeListModelNode(coverageId.ToString(), ""));
                        }

                        Subschedule s = null;
                        if (null != ced.SubSchedule)
                            s = (ced.SubSchedule.ContainsKey(schedule.Key)) ? ced.SubSchedule[schedule.Key]
                                : (ced.SubSchedule.ContainsKey(ScheduleSymbolToHashedPerRisk(schedule.Key))) ? ced.SubSchedule[ScheduleSymbolToHashedPerRisk(schedule.Key)]
                                        : ced.SubSchedule[ScheduleSymbolToPerRisk(schedule.Key)];

                        var node = new TreeListModelNode(schedule.Key, ((s != null) ? s.Type.ToString() : ""));
                        node.Children.AddRange(children);
                        sTree.Add(node);
                    }
                }
                return sTree;
            }
            private string ScheduleSymbolToHashedPerRisk(string str)
            { // EQ.G1.165748 -> EQ.G1.#
                if (!string.IsNullOrEmpty(str))
                {
                    int i = str.LastIndexOf('.');
                    if (i >= 0)
                        return str.Replace(str.Substring(i + 1, str.Length - i - 1), "#");
                }
                return str;
            }
            private string ScheduleSymbolToPerRisk(string str)
            { // EQ.G1.165748 -> EQ.G1.#
                if (!string.IsNullOrEmpty(str))
                {
                    int i = str.LastIndexOf('.');
                    if (i >= 0)
                        return str.Replace(str.Substring(i, str.Length - i), "");
                }
                return str;
            }

            #endregion

            #region TreeList

            protected void SetTreeListBackgroundColor(Color col) { SetBackgroundColor(this.TreeView, col); }
            public void ClearTreeList()
            {
                if (this.TreeView.InvokeRequired)
                    this.TreeView.Invoke(new Action(ClearTreeList));
                else if (null != this.TreeView.Roots)
                {
                    this.TreeView.Roots = new List<TreeListView>();
                    this.TreeView.Invalidate();
                }
            }
            public void PopulateTreeList(ContractExposureData ced)
            {
                if (this.TreeView.InvokeRequired)
                    this.TreeView.Invoke(new Action<ContractExposureData>(PopulateTreeList), ced);
                else if (null != ced)
                {
                    long id = this.SelectedId;

                    if (CacheTreeView.ContainsKey(id))//get from cache
                        this.TreeView.Roots = CacheTreeView[id];
                    else
                    { //calculate and add to cache

                        ContractExposure ce = null;
                        if (ced is PrimaryContractExposureData)
                            ce = ((PrimaryContractExposureData)ced).ConExp;

                        if (ce != null)
                        {
                            #region Extract Exposed Party ID
                            string partyID = (ce.ExposedParty != null && ce.ExposedParty.PartyID != null) ? ce.ExposedParty.PartyID.ToString() : "";
                            var exposedPartyID = new TreeListModelNode("Exposed Party ID", partyID);
                            #endregion

                            #region Extract Contract Subject Exposures
                            var CSEs = new TreeListModelNode("Contract Subject Exposures", "");

                            try
                            {
                                if (ce.ContractSubjectExposures != null)
                                {
                                    foreach (ContractSubjectExposureOfRiteSchedule cseRites in ce.ContractSubjectExposures)
                                    {
                                        string exposureTypeId = (cseRites.RITExposureTypeID != null) ? cseRites.RITExposureTypeID.ToString() : "";
                                        var CSE = new TreeListModelNode("Exposure Type Id", exposureTypeId);

                                        if (cseRites.SubjectFilter != null)
                                        {
                                            string sf = (cseRites.SubjectFilter.Name != null) ? cseRites.SubjectFilter.Name : "";
                                            string sfd = (cseRites.SubjectFilter.Description != null) ? cseRites.SubjectFilter.Description : "";
                                            CSE.Children.Add(new TreeListModelNode("Subject Filter", sf + " : " + sfd));

                                            if (cseRites.SubjectFilter.ChildFilters != null)
                                            {
                                                var childFilters = new TreeListModelNode("Child Filters (" + cseRites.SubjectFilter.ChildFilters.Count + ")", "");
                                                foreach (Filter childFilter in cseRites.SubjectFilter.ChildFilters)
                                                {
                                                    var childFilterNode = new TreeListModelNode("Child Filter", "");
                                                    string fname = (childFilter.Name != null) ? childFilter.Name : "";
                                                    string ftag = (string.IsNullOrEmpty(childFilter.Tag)) ? "-1" : childFilter.Tag;
                                                    childFilterNode.Children.Add(new TreeListModelNode("Name, Tag", fname + "," + ftag));
                                                    childFilters.Children.Add(childFilterNode);
                                                }
                                                CSE.Children.Add(childFilters);
                                            }
                                        }

                                        bool f = (cseRites.RITECollectionExposure != null);
                                        string ename = (f && cseRites.RITECollectionExposure.ExposureName != null) ? cseRites.RITECollectionExposure.ExposureName : "";
                                        var RITEs = new TreeListModelNode("RITEs", ename);
                                        if (f && cseRites.RITECollectionExposure.RITExposures != null)
                                        {
                                            foreach (RITExposure ritExposure in cseRites.RITECollectionExposure.RITExposures)
                                            {
                                                var RITE = new TreeListModelNode("RITE", "");

                                                string eid = (ritExposure.ExposureID != null) ? ritExposure.ExposureID.ToString() : "";
                                                RITE.Children.Add(new TreeListModelNode("Exposure Id / Location Id", eid));

                                                string sarname = "";
                                                if (ritExposure.SubjectAtRisk != null && ritExposure.SubjectAtRisk.SubjectAtRiskName != null)
                                                    sarname = ritExposure.SubjectAtRisk.SubjectAtRiskName;
                                                RITE.Children.Add(new TreeListModelNode("Subject At Risk", sarname));

                                                string numb = "";
                                                if (ritExposure.CommonCharacteristics != null && ritExposure.CommonCharacteristics.NumBuildings != null)
                                                    numb = ritExposure.CommonCharacteristics.NumBuildings.ToString();
                                                RITE.Children.Add(new TreeListModelNode("# Buildings", numb));

                                                var RITECharacteristics = new TreeListModelNode("RITE Characteristics", "");
                                                if (ritExposure.RiskitemCharacteristicsList != null && ritExposure.RiskitemCharacteristicsList.Items != null)
                                                {
                                                    foreach (RiskItemCharacteristicsValuation idxEntry in ritExposure.RiskitemCharacteristicsList.Items)
                                                    {
                                                        string ritech = (idxEntry.Id != null) ? idxEntry.Id.ToString() : "";
                                                        var RITECharacteristic = new TreeListModelNode("RITE Characteristic", ritech);

                                                        string reval = "";
                                                        if (idxEntry.RITExposureValuationList != null
                                                            && idxEntry.RITExposureValuationList.First() != null
                                                            && idxEntry.RITExposureValuationList.First().Value != null)
                                                            reval = idxEntry.RITExposureValuationList.First().Value.ToString();
                                                        RITECharacteristic.Children.Add(new TreeListModelNode("Value", reval));

                                                        string rtid = (idxEntry.RiteTypeId != null) ? idxEntry.RiteTypeId.ToString() : "";
                                                        RITECharacteristic.Children.Add(new TreeListModelNode("RITE Type Id", rtid));

                                                        RITECharacteristics.Children.Add(RITECharacteristic);
                                                    }
                                                }
                                                RITE.Children.Add(RITECharacteristics);
                                                RITEs.Children.Add(RITE);
                                            }
                                        }

                                        CSE.Children.Add(RITEs);
                                        CSEs.Children.Add(CSE);
                                    }
                                }
                            }
                            catch (Exception ex) { }

                            #endregion

                            #region Extract Filters
                            var subSchedule = ced.SubSchedule;
                            var filters = new TreeListModelNode("Filters", "");
                            if (subSchedule != null)
                            {
                                SubjectFilterSet subjectFilterData = new SubjectFilterSet() { Filters = new Dictionary<string, Subschedule>(), Universe = new HashSet<long>() };
                                subjectFilterData.Filters = subSchedule.Where(p => !(p.Value.Ids == null && p.Value.CompressedIds == null)).ToDictionary(p => p.Key, p => p.Value);

                                filters.Children.AddRange(
                                    subSchedule.Select(p =>
                                    {
                                        var filter = new TreeListModelNode("Key", p.Key);
                                        string RITEList = "";
                                        if (p.Value.Ids != null)
                                            RITEList = string.Join(",", p.Value.Ids);
                                        else if (p.Value.CompressedIds != null)
                                            RITEList = string.Join(",", p.Value.CompressedIds);
                                        filter.Children.Add(new TreeListModelNode("RITE List", RITEList));
                                        filter.Children.Add(new TreeListModelNode("CDLTag", p.Value.CDLTag));
                                        filter.Children.Add(new TreeListModelNode("Type", p.Value.Type.ToString()));
                                        return filter;
                                    }).ToList<TreeListModelNode>());
                            }
                            #endregion

                            var ceTree = new List<TreeListModelNode> { exposedPartyID, CSEs, filters };
                            CacheTreeView.TryAdd(id, ceTree);//add to cache
                            this.TreeView.Roots = ceTree;
                        }
                    }
                    this.TreeView.Invalidate();
                }
            }

            #endregion

            #region COL

            protected void SetCOLBackgroundColor(Color col) { SetBackgroundColor(this.GridCOL, col); }
            protected void SaveCOL(ContractExposureData ced)
            {
                if (this.GridCOL.InvokeRequired)
                    this.GridCOL.Invoke(new Action<ContractExposureData>(SaveCOL), ced);
                else if (null != this.GridCOL.Rows && this.GridCOL.Rows.Count > 0 && null != ced && null != ced.COLs && ced.COLs.Count() > 0)
                {
                    //Save from View to Cache
                    this.GridCOL.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    foreach (DataGridViewRow row in this.GridCOL.Rows)
                    {
                        string col = ((DataGridViewTextBoxCell)row.Cells[0]).Value.ToString();
                        if (ced.COLs.ContainsKey(col))
                            ced.COLs[col] = Convert.ToBoolean(((DataGridViewCheckBoxCell)row.Cells[1]).Value);
                    }
                }
            }
            protected void PopulateCOL(ContractExposureData ced)
            {
                if (this.GridCOL.InvokeRequired)
                    this.GridCOL.Invoke(new Action<ContractExposureData>(PopulateCOL), ced);
                else if (this.SelectedId != 0 && null != this.GridCOL.Rows && null != ced && null != ced.COLs && ced.COLs.Count() > 0)
                {
                    if (this.GridCOL.Rows.Count > 0)
                        this.GridCOL.Rows.Clear();

                    // Populate View from Cache
                    foreach (var p in ced.COLs)
                        this.GridCOL.Rows.Add(p.Key, Convert.ToBoolean(p.Value));

                    this.GridCOL.Update();
                }
            }
            protected List<string> CheckedCOL()
            {
                if (this.GridCOL.InvokeRequired)
                    return (List<string>)(this.GridCOL.Invoke(new Func<List<string>>(CheckedCOL)));
                else
                {
                    var chCOLs = new List<string>();
                    if (null != this.GridCOL.Rows && this.GridCOL.Rows.Count > 0)
                    {
                        //Committing of checkbox changes before CellEndEdit event
                        this.GridCOL.CommitEdit(DataGridViewDataErrorContexts.Commit);

                        foreach (DataGridViewRow row in this.GridCOL.Rows)
                        {
                            if (Convert.ToBoolean((DataGridViewCheckBoxCell)row.Cells[1].Value))
                                chCOLs.Add(((DataGridViewTextBoxCell)row.Cells[0]).Value.ToString());
                        }
                    }
                    return chCOLs;
                }
            }
            public void ClearCOL() { ClearGrid(this.GridCOL); }
            public void InvertCheckedCOL() { InvertChecked(this.GridCOL); }
            public void COLColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
            {
                if (e.ColumnIndex == 1)
                {
                    InvertCheckedCOL();
                }
            }

            #endregion

            #region ID

            protected void ClearGridID() { ClearGrid(this.GridID); SelectedId = 0; IsFirstUpload = true; }
            protected int CountID()
            {
                if (this.GridID.InvokeRequired)
                    return (int)(this.GridID.Invoke(new Func<int>(CountID)));
                else
                    return (null != this.GridID.Rows) ? this.GridID.Rows.Count : 0;
            }
            public virtual HashSet<long> RemoveSelectedID()
            {
                if (this.GridID.InvokeRequired)
                    return (HashSet<long>)(this.GridID.Invoke(new Func<HashSet<long>>(RemoveSelectedID)));
                else
                {
                    var IDs = new HashSet<long>();

                    if (null != this.GridID.Rows && this.GridID.Rows.Count > 0)
                    {
                        CancelJobs();

                        int count = this.GridID.Rows.Count;

                        foreach (DataGridViewRow row in this.GridID.SelectedRows)
                        {
                            IDs.Add(Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, row.Index])).Value));
                            this.GridID.Rows.RemoveAt(row.Index);
                            count--;
                        }

                        this.GridID.Update();

                        Clear(IDs.ToArray());

                        if (null == this.GridID.CurrentRow)
                        { // Clear all
                            this.SelectedId = 0;
                            IsFirstUpload = true;
                            ngfmPrototype.Initialize();
                        }
                        else
                        {
                            this.SelectedId = Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, this.GridID.CurrentRow.Index])).Value);
                            if (this.SelectedId != 0)
                                PopulateSelected();
                        }
                    }

                    TaskManager.CancelAll(IDs.ToArray());
                    InfoNamberThreadsAndTasks();
                    return IDs;
                }
            }
            protected long GetSelectedId(int idx)
            {
                if (this.GridID.InvokeRequired)
                    return (long)(this.GridID.Invoke(new Func<int, long>(GetSelectedId), idx));
                else if (null != this.GridID.Rows && this.GridID.Rows.Count > 0 && 0 <= idx && idx < this.GridID.Rows.Count)
                    return Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, idx])).Value);
                else return 0;
            }
            protected long GetSelectedId()
            {
                return GetSelected(this.GridID);
            }
            protected abstract long GetFreeId();
            public HashSet<long> GetMultiSelectID()
            {
                if (this.GridID.InvokeRequired)
                    return (HashSet<long>)(this.GridID.Invoke(new Func<HashSet<long>>(GetMultiSelectID)));
                else
                {
                    var IDs = new HashSet<long>();
                    foreach (DataGridViewRow row in this.GridID.SelectedRows)
                        IDs.Add(Convert.ToInt64(((DataGridViewTextBoxCell)(this.GridID[0, row.Index])).Value));
                    return IDs;
                }
            }
            protected long[] GetAllID()
            {
                return Get(this.GridID);
            }
            protected virtual void PopulateID(params long[] ids)
            {
                Populate(this.GridID, ids);
            }

            #endregion

            #region Damage Ratios

            public static void UpdateDamageRatios(int tabIdx, VariantPrimary vp, VariantTreaty vt,
                bool _simulate = false, int seed = 17317)
            {
                #region OpenFileDialog

                simulate = _simulate;
                string drFile = "";
                if (!simulate)
                {
                    // Create an instance of the open file dialog box.
                    OpenFileDialog Dlg = new OpenFileDialog();

                    //Uncertainty Module GU Per Event Result (s)
                    Dlg.Filter = "Ground Up Losses (.dat, .dat1, .txt, .csv)|*.dat;*.dat1;*.txt;*.csv|All Files (*.*)|*.*";
                    Dlg.FilterIndex = 1;
                    Dlg.CheckFileExists = false;
                    Dlg.ValidateNames = false;
                    Dlg.Multiselect = false;

                    if (Dlg.ShowDialog() == DialogResult.OK)
                    {
                        drFile = Dlg.FileName;
                    }
                    else { return; }

                    if (!File.Exists(drFile)) return;
                }

                #endregion

                Action<object> action = (obj) =>
                {
                    #region Get Damage Ratios

                    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> dr = null;
                    lock (ngfmPrototype)
                    {
                        dr = (simulate) ? ngfmPrototype.SimulateDamageRatios(seed)
                            : ngfmPrototype.ReadDamageRatiosFromFile(drFile);
                    }

                    #endregion

                    #region Clear tables and Cache

                    CacheTreeSchedule.Clear();

                    vp.ClearPayOut();
                    vt.ClearPayOut();

                    vp.ClearScheduleTree();
                    vt.ClearScheduleTree();

                    vp.ClearTimeSeries();
                    vt.ClearTimeSeries();

                    #endregion

                    #region Transform Damage Ratios

                    if (null != dr && dr.Count() > 0)
                    {
                        lock (ngfmPrototype)
                            ngfmPrototype.TransformDamageRatios(dr, simulate);
                    }

                    #endregion

                    UpdateProcessInfo(tabIdx, vp, vt);
                };

                TaskManager.Start(TaskQueueID, 0, action, TaskQueueID);
            }

            public static void SaveDamageRatios()
            {
                string drFile = "";
                #region SaveFileDialog

                // Create an instance of the open file dialog box.
                SaveFileDialog Dlg = new SaveFileDialog();

                //Uncertainty Module GU Per Event Result (s)
                Dlg.Filter = "Ground Up Losses (.dat, .txt)|*.dat;*.txt;";
                Dlg.FilterIndex = 1;
                Dlg.CheckFileExists = false;
                Dlg.ValidateNames = false;

                if (Dlg.ShowDialog() == DialogResult.OK)
                {
                    drFile = Dlg.FileName;
                }
                if (string.IsNullOrEmpty(drFile)) return;
                #endregion

                Action<object> action = (obj) =>
                {
                    lock (ngfmPrototype)
                    {
                        ngfmPrototype.WriteDamageRatiosToFile(drFile);
                    }
                };

                TaskManager.Start(TaskQueueID, 0, action, TaskQueueID);
            }

            #endregion

            #region Time Series

            protected void SetTimeSeriesBackgroundColor(Color col) { SetBackgroundColor(this.GridTimeSeries, col); }
            protected void ClearTimeSeries() { ClearGrid(this.GridTimeSeries); }
            protected void PopulateTimeSeries(ContractExposureData ced)
            {
                if (this.GridTimeSeries.InvokeRequired)
                    this.GridTimeSeries.Invoke(new Action<ContractExposureData>(PopulateTimeSeries), ced);
                else if (this.SelectedId != 0 && null != this.GridTimeSeries.Rows && null != ced)
                {
                    if (this.GridTimeSeries.Rows.Count > 0)
                        this.GridTimeSeries.Rows.Clear();

                    SortedDictionary<DateTime, double[]> timeSeries = CombineInputAndOutputTimeSeries(ced);

                    if (timeSeries != null)
                    {
                        // Populate View from Cache
                        int i = 0;
                        foreach (var p in timeSeries.Where(elem => null != elem.Value))
                        {
                            this.GridTimeSeries.Rows.Add(p.Key.ToString("MM/dd/yyyy"), p.Value[0].ToString("#.##"), p.Value[1].ToString("#.##"));

                            var po = GetResultPosition(ced.Id);

                            if (po != null && po.PayOut > 0.0 && po.IsInsideHoursClause(p.Key))
                            {
                                this.GridTimeSeries.Rows[i].DefaultCellStyle.BackColor = Color.Khaki;
                            }
                            i++;
                        }

                        this.GridTimeSeries.Update();
                    }
                }
            }

            #endregion

            #region Operation Stack

            public void SetTablesBackgroundColor(Color col)
            {
                SetCDLBackgroundColor(col);
                SetIRBackgroundColor(col);
                SetBugLogBackgroundColor(col);
                SetScheduleBackgroundColor(col);
                SetTreeListBackgroundColor(col);
                SetCOLBackgroundColor(col);
                SetTimeSeriesBackgroundColor(col);
            }
            protected bool Save(ContractExposureData ced)
            {
                if (null == ced)
                    return false;
                else
                {
                    SaveCOL(ced);
                    return SaveCDL(ced);
                }
            }
            public virtual void SaveSelected()
            {
                Save(GetContractData(this.SelectedId));
            }
            public virtual void ClearTabs()
            {
                this.StrCDL = "";
                this.IR = "";
                this.BugLog = "";
                ClearCOL();
                ClearScheduleTree();
                ClearTreeList();
                ClearTimeSeries();
            }
            protected bool SaveSelectedClearTabs()
            {
                if (Save(GetContractData(this.SelectedId)))
                {
                    ClearTabs();
                    return true;
                }else 
                    return false;
            }
            public virtual void ClearAllExceptGridID()
            {
                Clear(GetAllID());
            }
            public void ClearTotal()
            {
                ClearAllExceptGridID();
                ClearGridID();
            }
            protected virtual void Clear(params long[] ids)
            {
                if (ids.Length > 0)
                {
                    lock (ngfmPrototype)
                        ngfmPrototype.Remove(ids);

                    this.SelectedId = 0;
                    this.SaveFileName = "";
                    CacheTreeSchedule.Remove<long, List<TreeListModelNode>>(ids.ToList());
                    CacheTreeView.Remove<long, List<TreeListModelNode>>(ids.ToList());
                    ClearTabs();
                }
            }
             protected virtual void Populate(ContractExposureData ced)
            {
                if (null != ced)
                {
                    PopulateCDL(ced);
                    PopulateIR(ced);
                    PopulateBugLog(ced);
                    PopulateCOL(ced);
                    PopulateScheduleTree(ced);
                    PopulateTreeList(ced);
                    PopulateTimeSeries(ced);
                }
            }
            protected SortedDictionary<DateTime, double[]> CombineInputAndOutputTimeSeries(ContractExposureData ced)
            {
                SortedDictionary<DateTime, double[]> timeSeries = null;

                if (null != ced)
                {
                    bool flagIn = (null != ced.InputTimeSeries && ced.InputTimeSeries.Count() > 0);

                    var po = GetResultPosition(ced.Id);

                    bool flagOut = (null != po && null != po.TimeAllocation && po.TimeAllocation.Count() > 0);

                    if (flagIn)
                    {
                        timeSeries = new SortedDictionary<DateTime, double[]>(
                             ced.InputTimeSeries.ToDictionary(kv => kv.Key, kv => new double[] { kv.Value, 0.0 }));
                        if (flagOut)
                        {
                            foreach (var kv in po.TimeAllocation)
                            {
                                if (timeSeries.ContainsKey(kv.Key))
                                    timeSeries[kv.Key][1] = kv.Value;
                                else
                                    timeSeries.Add(kv.Key, new double[] { 0.0, kv.Value });
                            }
                        }
                    }
                    else if (flagOut)
                    {
                        timeSeries = new SortedDictionary<DateTime, double[]>(
                             po.InputTimeSeries.ToDictionary(kv => kv.Key, kv => new double[] { kv.Value, 0.0 }));
                        foreach (var kv in po.TimeAllocation)
                        {
                            if (timeSeries.ContainsKey(kv.Key))
                                timeSeries[kv.Key][1] = kv.Value;
                            else
                                timeSeries.Add(kv.Key, new double[] { 0.0, kv.Value });
                        }
                    }
                }
                return timeSeries;

            }
            public void PopulateSelected()
            {
                Populate(GetContractData(this.SelectedId));
            }
            public static void Process(int tabIdx, VariantPrimary vp, VariantTreaty vt, bool outputInProcess, params long[] contractIDs)
            {
                TaskManager.CancelAll();

                Action<object> action = (obj) =>
                {
                    #region Save current CDL (if it was changed) and Clear tables

                    if(tabIdx == 1)
                        vp.SaveSelectedClearTabs();
                    else
                        vt.SaveSelectedClearTabs();

                    #endregion

                    #region Build Contract Graphs if CDL was changed 

                    Dictionary<long, Task> tasks = null;
                    lock (ngfmPrototype)
                    {
                        ngfmPrototype.BuildParsingContext();
                        if(ngfmPrototype.CacheContractData.Any(kv => kv.Value.IsGraphNotYetBuilt()))
                            tasks = ngfmPrototype.Prepare_OLDAPI();
                    }
                    if (tasks != null)
                    {
                        if (outputInProcess)
                            UpdateProcessInfo(tabIdx, vp, vt, tasks);
                        Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());
                    }
                    lock (ngfmPrototype)
                    {
                        ngfmPrototype.DisposeParsingContext();
                    }

                    #endregion

                    #region FM Execution

                    ContractExposureData.totalTime = new TimeSpan(0, 0, 0, 0, 0);
                    
                    lock (ngfmPrototype)
                    {
                        //This version is like in NGDLM (with transformation of damage ratios at each FM execution)

                        ngfmPrototype.ProcessEvent_OLDAPI(0, ngfmPrototype.DamageRatiosPerSubPeril, out tasks, contractIDs);

                        if (tasks != null)
                        {
                            if (outputInProcess)
                                UpdateProcessInfo(tabIdx, vp, vt, tasks);
                            Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());
                            if (!outputInProcess)
                                UpdateProcessInfo(tabIdx, vp, vt, tasks);
                            MessageBox.Show(null, string.Format("{0}", ContractExposureData.totalTime.ToString()), "FM Execution total time:");
                            UpdateProcessInfo(tabIdx, vp, vt);
                        }
                    }

                    #endregion

                };

                TaskManager.Start(TaskQueueID, 0, action, TaskQueueID);
            }

            public static void ProcessAll(int tabIdx, VariantPrimary vp, VariantTreaty vt, bool outputInProcess)
            {
                if (tabIdx == 2)
                    Process(tabIdx, vp, vt, outputInProcess);
                else
                {
                    var ids = ngfmPrototype.CacheContractData.Where(kv => kv.Value is PrimaryContractExposureData)
                        .Select(kv => kv.Key).ToArray();

                    Process(tabIdx, vp, vt, outputInProcess, ids);
                }
            }
            public static void ProcessSelectedId(int tabIdx, VariantPrimary vp, VariantTreaty vt, bool outputInProcess)
            {
                long id = 0;

                if (tabIdx == 1)
                {
                    if(vp.SelectedId == 0)
                        vp.GridID_CreateNew();
                    id = vp.SelectedId;
                }
                else
                {
                    if (vt.SelectedId == 0)
                        vt.GridID_CreateNew();
                    id = vt.SelectedId;
                }

                Process(tabIdx, vp, vt, outputInProcess, id);
            }
            public virtual void SelectionChangedID(object sender, EventArgs e)
            {
                DataGridView dgv = (DataGridView)sender;

                if (Control.ModifierKeys != Keys.None || null == dgv.CurrentRow)
                    return;

                long id = GetSelected(dgv);

                // 1. Save (CDL, COL, Positions) and Clear (CDL, IR, COL, ScheduleTree, TreeList, Positions)
                if (this.SelectedId != 0)
                {
                    SaveSelected();
                    ClearTabs();
                }

                // 2. Update SelectedId
                this.SelectedId = id;
                //Interlocked.CompareExchange(ref this.SelectedId, id, this.SelectedId);

                // 3. Populate Selected if it isn't a first upload
                if (!IsFirstUpload)
                    PopulateSelected();
                else
                    IsFirstUpload = false;

            }
            public virtual void GridID_CreateNew() { }
            public virtual long CreateCopy()
            {
                long id = ngfmPrototype.CopyContractData(this.SelectedId);
                if(id != 0)
                    PopulateID(id);
                return id;
            }

            #endregion

            #region FM

            public bool FM { set { FM_Enabled(value); } get { return IsFM_Enabled(); } }
            private void FM_Enabled(bool enabled)
            {
                if (this.Menu.InvokeRequired)
                    this.Menu.Invoke(new Action<bool>(FM_Enabled), enabled);
                else
                    this.BtnExecuteFM.Enabled = enabled;
            }
            private bool IsFM_Enabled()
            {
                if (this.Menu.InvokeRequired)
                    return (bool)(this.Menu.Invoke(new Func<bool>(IsFM_Enabled)));
                else
                    return this.BtnExecuteFM.Enabled;
            }
            
            #endregion
        }
        //**************************************************
        public class VariantPrimary : Variant
        {
            #region Fields

            public ToolStripMenuItem BtnSimulateGULoss { set; get; }
            public ToolStripMenuItem BtnFromfileGULoss { set; get; }
            
            #endregion

            #region Constructor

            public VariantPrimary(
                DataGridView gridID,
                DataGridView gridCOL,
                DataGridView gridTimeSeries,
                TextBox textBoxCDL,
                TextBox textBoxIR,
                TextBox textbxBugLog,
                TreeListView treeSchedule,
                TreeListView treeView,
                MenuStrip menu,
                ToolStripMenuItem executeFM,
                ToolStripMenuItem btnSimulateGULoss,
                ToolStripMenuItem btnFromfileGULoss,
                ToolStrip toolStripProgress,
                ToolStripTextBox infoNumberThreads,
                ToolStripTextBox infoNumberTasks)
                : base(gridID, gridCOL, gridTimeSeries, textBoxCDL, textBoxIR, textbxBugLog, treeSchedule, treeView,
                menu, executeFM, toolStripProgress, infoNumberThreads, infoNumberTasks)
            {
                this.BtnSimulateGULoss = btnSimulateGULoss;
                this.BtnFromfileGULoss = btnFromfileGULoss;
            }
            
            #endregion

            #region ID

            protected override void PopulateID(long[] ids)
            {
                base.PopulateID(ids);
                DamageRatios_Enabled(null != ids && ids.Length > 0);
            }
            
            #endregion

            #region Damage Ratios

            private void DamageRatios_Enabled(bool enabled)
            {
                if (this.Menu.InvokeRequired)
                    this.Menu.Invoke(new Action<bool>(DamageRatios_Enabled), enabled);
                else
                {
                    this.BtnSimulateGULoss.Enabled = enabled;
                    this.BtnFromfileGULoss.Enabled = enabled;
                }
            }

            #endregion

            #region Operation Stack

            public override void ClearAllExceptGridID()
            {
                //GULoss_Enabled(false);
                base.ClearAllExceptGridID();
            }
            public override long CreateCopy()
            {
                long id = base.CreateCopy();
                //if(id != 0)
                //    Process(true, true, true, id);
                return id;
            }
            protected override long GetFreeId()
            {
                long[] ids = GetAllID();

                if (null == ids || ids.Length == 0)
                    return 1;

                long id = 1;
                while (true)
                {
                    if (!ids.Contains(id))
                        return id;
                    id++;
                }
            }
            public override HashSet<long> RemoveSelectedID()
            {
                var rmvIDs = base.RemoveSelectedID();
                InfoNamberThreadsAndTasks();
                return rmvIDs;
            }
            
            #endregion

        }
        //**************************************************
        public class VariantTreaty : Variant
        {
            #region Fields

            public DataGridView GridPos { set; get; }
            private DataGridView gridPosContent { set; get; }
            public ToolStrip ToolStripCurrentContract { set; get; }
            public ToolStripTextBox TxtbxCurrentContract { set; get; }
            public bool IsFreeze { set; get; }
            public ToolStripButton BtnFreeze { set; get; }
            
            #endregion

            #region Constructor

            public VariantTreaty(
                DataGridView gridID,
                DataGridView gridCOL,
                DataGridView gridTimeSeries,
                TextBox textBoxCDL,
                TextBox textBoxIR,
                TextBox textbxBugLog,
                TreeListView treeSchedule,
                TreeListView treeView,
                MenuStrip menu,
                ToolStripMenuItem executeFM,
                DataGridView gridPos,
                DataGridView gridPositionContent,
                ToolStripButton bttnFreeze,
                ToolStripTextBox txtbxCurrentContract,
                ToolStrip toolStrip1,
                ToolStrip toolStripProgress,
                ToolStripTextBox infoNumberThreads,
                ToolStripTextBox infoNumberTasks)
                : base(gridID, gridCOL, gridTimeSeries, textBoxCDL, textBoxIR, textbxBugLog, treeSchedule, treeView, menu,
                executeFM, toolStripProgress, infoNumberThreads, infoNumberTasks)
            {
                this.GridPos = gridPos;
                this.gridPosContent = gridPositionContent;
                this.IsFreeze = false;
                this.BtnFreeze = bttnFreeze;
                this.TxtbxCurrentContract = txtbxCurrentContract;
                this.ToolStripCurrentContract = toolStrip1;
            }
            
            #endregion

            #region Positions

            private void PopulatePositions(ContractExposureData ced)
            {
                Dictionary<string, HashSet<long>> positions = (null != ced) ? ((TreatyContractExposureData)ced).GetPositions() : null;
                if (null != ced && null != positions)
                {
                    PopulatePositions(new HashSet<string>(positions.Keys));
                    string pos = GetSelectedPosition();

                    if (null != positions && !string.IsNullOrEmpty(pos) && positions.ContainsKey(pos))
                    {
                        ClearGrid(this.gridPosContent);
                        Populate(this.gridPosContent, positions[pos].ToArray());
                    }

                    PopulateCurrentContractId(ced.Id);
                    VisibleCurrentContractFreeze(true);
                }
            }
            public void PopulatePositions(HashSet<string> poss)
            {
                if (this.GridPos.InvokeRequired)
                    this.GridPos.Invoke(new Action<HashSet<string>>(PopulatePositions), poss);
                else if (null != poss && poss.Count() > 0)
                {
                    this.GridPos.Rows.Clear();
                    foreach (string pos in poss)
                        this.GridPos.Rows.Add(pos);
                    this.GridPos.Update();
                }
            }
            public void gridPosition_SelectionChanged(object sender, EventArgs e)
            {
                DataGridView dgv = (DataGridView)sender;

                if (Control.ModifierKeys != Keys.None || null == dgv.CurrentRow)
                    return;

                var ced = GetContractData(GetCurrentContractId());

                Dictionary<string, HashSet<long>> positions = (null != ced) ? ((TreatyContractExposureData)ced).GetPositions() : null;
                string pos = GetSelectedPosition();

                if (null != positions && !string.IsNullOrEmpty(pos) && positions.ContainsKey(pos))
                {
                    ClearGrid(this.gridPosContent);
                    Populate(this.gridPosContent, positions[pos].ToArray());
                }
            }
            public void ClearPositions()
            {
                ClearGrid(this.GridPos);
                ClearPositionContent();
            }
            private string GetSelectedPosition()
            {
                if (this.GridPos.InvokeRequired)
                    return (string)(this.GridPos.Invoke(new Func<string>(GetSelectedPosition)));
                else if (null != this.GridPos.Rows && this.GridPos.Rows.Count > 0)
                    return ((DataGridViewTextBoxCell)(this.GridPos[0, this.GridPos.CurrentRow.Index])).Value.ToString();
                else return "";
            }
            public void ClearPositionContent()
            {
                ClearGrid(this.gridPosContent);
                ClearCurrentContractId();
                VisibleCurrentContractFreeze(false);
            }
            public long[] GetPositionContent() { return Get(this.gridPosContent); }
            public long GetSelectedPositionContent() { return GetSelected(this.gridPosContent); }
            public void ClearBeforUpdatePositionContent()
            {
                if (!IsFreeze)
                {
                    ClearScheduleTree();
                    ClearTimeSeries();
                    ClearPayOut(this.SelectedId);
                }
            }
            public void UpdatePositionContent(VariantPrimary varPrimary, bool append)
            {
                RemoveFromCacheTreeSchedule();

                long id = GetCurrentContractId();
                var ced = (TreatyContractExposureData)(GetContractData(id));
                if (null != ced)
                {
                    string pos = GetSelectedPosition();

                    if (!string.IsNullOrEmpty(pos))
                    {
                        HashSet<long> newContent = (null != varPrimary) ? varPrimary.GetMultiSelectID() : GetMultiSelectID();
                        if (append)
                            newContent.UnionWith(GetPositionContent());

                        if (newContent.Contains(id))
                            newContent.Remove(id);

                        ClearGrid(this.gridPosContent);
                        Populate(this.gridPosContent, newContent.ToArray());

                        lock(ngfmPrototype)
                            ngfmPrototype.RemovePayOutAttributes(0, ced.Id);

                        ced.PopulatePosition(pos, newContent);
                    }
                }
            }
            public void RemoveMultiSelectedPositionContent(bool removeAll = false)
            {
                RemoveFromCacheTreeSchedule();

                if (removeAll)
                    ClearGrid(this.gridPosContent);
                else
                    RemoveMultiSelected(this.gridPosContent);

                var ced = (TreatyContractExposureData)(GetContractData(this.SelectedId));
                if (null != ced)
                {
                    string pos = GetSelectedPosition();

                    if (!string.IsNullOrEmpty(pos))
                    {
                        var newContent = new HashSet<long>(GetPositionContent());

                        lock (ngfmPrototype)
                            ngfmPrototype.RemovePayOutAttributes(0, ced.Id);

                        ced.PopulatePosition(pos, newContent);
                    }
                }
            }
            
            #endregion

            #region ToolStrip CurrentContract

            private void ClearCurrentContractId()
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    this.ToolStripCurrentContract.Invoke(new Action(ClearCurrentContractId));
                else
                {
                    this.TxtbxCurrentContract.Text = "";
                    this.TxtbxCurrentContract.Invalidate();
                }
            }
            private void PopulateCurrentContractId(long id)
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    this.ToolStripCurrentContract.Invoke(new Action<long>(PopulateCurrentContractId), id);
                else
                {
                    this.TxtbxCurrentContract.Text = id.ToString();
                    this.TxtbxCurrentContract.Invalidate();
                }
            }
            private long GetCurrentContractId()
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    return (long)(this.ToolStripCurrentContract.Invoke(new Func<long>(GetCurrentContractId)));
                else
                    return (!string.IsNullOrEmpty(this.TxtbxCurrentContract.Text)) ? Convert.ToInt64(this.TxtbxCurrentContract.Text) : 0;
            }
            private void VisibleCurrentContractFreeze(bool flag)
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    this.ToolStripCurrentContract.Invoke(new Action<bool>(VisibleCurrentContractFreeze), flag);
                else
                {
                    this.BtnFreeze.Visible = flag;
                    this.BtnFreeze.Invalidate();
                }
            }
            private void TextCurrentContractFreeze(string txt)
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    this.ToolStripCurrentContract.Invoke(new Action<string>(TextCurrentContractFreeze), txt);
                else
                {
                    this.BtnFreeze.Text = txt;
                    this.BtnFreeze.Invalidate();
                }
            }
            protected void SetColorCurrentContractId(Color col)
            {
                if (this.ToolStripCurrentContract.InvokeRequired)
                    this.ToolStripCurrentContract.Invoke(new Action<Color>(SetColorCurrentContractId), col);
                else
                {
                    this.TxtbxCurrentContract.BackColor = col;
                    this.TxtbxCurrentContract.Invalidate();
                }
            }
            
            #endregion

            #region Operation Stack

            public override void ClearTabs()
            {
                if (!IsFreeze)
                {
                    ClearPositions();
                }

                base.ClearTabs();
            }
            protected override void Populate(ContractExposureData ced)
            {
                if (!IsFreeze && null != ced)
                {
                    PopulatePositions(ced);
                }

                base.Populate(ced);
            }
            public override void GridID_CreateNew()
            {
                long id = GetFreeId();
                lock (ngfmPrototype)
                    ngfmPrototype.AddNewContractData(id, "");//StrCDL
                PopulateID(id);
            }
            protected override long GetFreeId()
            {
                long[] ids = GetAllID();

                if(null == ids || ids.Length == 0)
                    return -1;

                long id = -1;
                while(true)
                {
                    if(!ids.Contains(id))
                        return id;
                    id--;
                }
            }
            public override HashSet<long> RemoveSelectedID()
            {
                var rmvIDs = base.RemoveSelectedID();

                long id = GetCurrentContractId();
                if (0 != id)
                {
                    long[] ids = GetAllID();
                    if (!ids.Contains(id))
                    {
                        IsFreeze = false;
                        ClearPositions();
                        ClearPositionContent();
                        ClearCurrentContractId();
                        VisibleCurrentContractFreeze(false);
                    }
                }
                
                InfoNamberThreadsAndTasks();
                return rmvIDs;
            }
            public void UpdateCurrentPosition()
            {
                long id = GetCurrentContractId();
                PopulatePositions(GetContractData(id));
            }
            public void ChangeFreeze()
            {
                IsFreeze = !IsFreeze;

                if (IsFreeze)
                {
                    TextCurrentContractFreeze("Unfreeze");

                    SetColorCurrentContractId(Color.PowderBlue);
                    SetBackgroundColor(this.GridPos, Color.PowderBlue);
                    SetBackgroundColor(this.gridPosContent, Color.PowderBlue);
                }
                else
                {
                    TextCurrentContractFreeze("Freeze");

                    SetColorCurrentContractId(Color.AntiqueWhite);
                    SetBackgroundColor(this.GridPos, Color.AntiqueWhite);
                    SetBackgroundColor(this.gridPosContent, Color.AntiqueWhite);

                    long id = GetCurrentContractId();
                    if (this.SelectedId != 0 && this.SelectedId != id)
                    {
                        var ced = GetContractData(this.SelectedId);
                        PopulatePositions(ced);
                    }
                }
            }
            public override long CreateCopy()
            {
                long id = base.CreateCopy();
                //if (id != 0)
                //    Process(true, true, true, id);
                return id;
            }
            
            #endregion

        }
        //==================================================

        #region Fields

        private VariantPrimary VarPrimary;
        private VariantTreaty VarTreaty;
        private int tabIdx = 1;
        private bool OutputInProcess = true;

        #endregion

        #region Constructor

        public NGFMPrototypeForm()
        {
            InitializeComponent();

            VarPrimary = new VariantPrimary(this.gridConExp1, this.gridCOL1, this.gridTimeSeries1, this.txtbxCDL1, this.txtbxIR1, this.textbxBugLog1, this.scheduleTreeListView1,
                this.treeListView1, this.menuStrip1, this.executeFM1, this.simulateGULoss1, this.fromfileGULoss1, this.toolStripProgress,
                this.InfoNumberThreads, this.InfoNumberTasks);

            VarTreaty = new VariantTreaty(this.gridTreaty2, this.gridCOL1, this.gridTimeSeries1, this.txtbxCDL1, this.txtbxIR1, this.textbxBugLog1, this.scheduleTreeListView1,
                this.treeListView1, this.menuStrip2, this.executeFM2, this.gridPosition, this.gridPositionContent, this.bttnFreeze, this.txtbxCurrentContract,
                this.toolStrip1, this.toolStripProgress, this.InfoNumberThreads, this.InfoNumberTasks);
        }
        
        #endregion

        #region Common Functions

        private void txtbx_MouseEnter(object sender, EventArgs e)
        {
            var tbx = (TextBox)sender;
            if (tbx.InvokeRequired)
                tbx.Invoke(new Action<object, EventArgs>(txtbx_MouseEnter), sender, e);
            else
                tbx.Focus();
        }
        private void txtbx_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                if (sender != null)
                    ((TextBox)sender).SelectAll();
                e.Handled = true;
            }
        }
        
        #endregion

        #region TABS ID

        private void tabControl3_Click(object sender, EventArgs e)
        {
            string name = ((TabControl)sender).SelectedTab.Name;
            if (name.Equals("tabID1"))
            {
                if (tabIdx == 2)
                {
                    VarTreaty.SaveSelected();
                    VarPrimary.ClearTabs();
                    VarPrimary.PopulateSelected();
                    tabIdx = 1;
                }
                VarPrimary.SetTablesBackgroundColor(Color.White);
            }
            else if (name.Equals("tabID2"))
            {
                if (tabIdx == 1)
                {
                    VarPrimary.SaveSelected();
                    VarPrimary.ClearTabs();
                    VarTreaty.PopulateSelected();
                    tabIdx = 2;
                }
                VarTreaty.SetTablesBackgroundColor(Color.AntiqueWhite);
            }

        }
        
        #endregion

        #region Grid Views

        // *** PRIMARY CONTRACT EXPOSURES (left) ***
        private void gridConExp1_SelectionChanged(object sender, EventArgs e)
        {
            VarPrimary.SelectionChangedID(sender, e);
        }
        // *** TREATIY CONTRACT EXPOSURES (right) ***
        private void gridTreaty2_SelectionChanged(object sender, EventArgs e)
        {
            VarTreaty.SelectionChangedID(sender, e);
        }
        private void gridPosition_SelectionChanged(object sender, EventArgs e)
        {
            VarTreaty.gridPosition_SelectionChanged(sender, e);
        }
        
        #endregion

        #region Menu (Primary)

        // File -> Open
        private void openFile1_Click(object sender, EventArgs e)
        {
            Variant.UploadContractExposureDialog(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        // File -> Add
        private void addFile1_Click(object sender, EventArgs e)
        {
            Variant.UploadContractExposureDialog(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess, true);
        }
        // File -> Save
        private void saveFile1_Click(object sender, EventArgs e)
        {
            VarPrimary.SaveToFile();
        }
        // File -> Save As
        private void saveasFile1_Click(object sender, EventArgs e)
        {
            VarPrimary.SaveToFile(true);
        }
        // File -> Close
        private void closeFile1_Click(object sender, EventArgs e)
        {
            VarPrimary.SaveToFile();
            VarPrimary.ClearTotal();
        }
        // "Parse + Compile" -> "Compile Selected"
        private void parseCompile1_Click(object sender, EventArgs e)
        {
            Variant.ProcessSelectedId(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        private void txtbxCDL1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            VarTreaty.FM = VarPrimary.FM;
            Variant.ProcessSelectedId(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        private void txtbxIR1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Variant.ProcessSelectedId(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        // "Parse + Compile" -> "Compile All"
        private void parseCompileAll1_Click(object sender, EventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        private void gridConExp1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
         // "GU Loss" -> "From File"
        private void fromfileGULoss1_Click(object sender, EventArgs e)
        {
            Variant.UpdateDamageRatios(tabIdx, VarPrimary, VarTreaty, false);
        }
        // "GU Loss" -> "Simulate" 
        private void simulateGULoss1_Click(object sender, EventArgs e)
        {
            Variant.UpdateDamageRatios(tabIdx, VarPrimary, VarTreaty, true);
        }
        // "GU Loss" -> "Simulate (Random)"
        private void simulateRandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Variant.UpdateDamageRatios(tabIdx, VarPrimary, VarTreaty, true, DateTime.Now.Millisecond);
        }
        // "GU Loss" -> "Save To File"
        private void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Variant.SaveDamageRatios();
        }
        // Execute -> "Execute FM"
        private void executeFM1_Click(object sender, EventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
            VarTreaty.FM = VarPrimary.FM;
        }
        
        #endregion

        #region Menu (Treaty)

        // File -> Open
        private void openFile2_Click(object sender, EventArgs e)
        {
            Variant.UploadContractExposureDialog(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        // File -> Add
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Variant.UploadContractExposureDialog(tabIdx, VarPrimary, VarTreaty, true);
        }
        // File -> Save
        private void saveFile2_Click(object sender, EventArgs e)
        {
            VarTreaty.SaveToFile();
        }
        // File -> Save As
        private void saveasFile2_Click(object sender, EventArgs e)
        {
            VarTreaty.SaveToFile(true);
        }
        // "Parse + Compile" -> "Compile Selected"
        private void parseCompile2_Click(object sender, EventArgs e)
        {
            Variant.ProcessSelectedId(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        // "Parse + Compile" -> "Compile All"
        private void parseCompileAll2_Click(object sender, EventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        // "Execute" -> "Execute FM (Recursive)"
        private void executeFMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }
        private void gridTreaty2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Variant.ProcessAll(tabIdx, VarPrimary, VarTreaty, this.OutputInProcess);
        }

        #endregion

        #region CDL, COL, ...

        private void gridCOL1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (tabIdx == 1)
                VarTreaty.COLColumnHeaderMouseClick(sender, e);
            else if (tabIdx == 2)
                VarPrimary.COLColumnHeaderMouseClick(sender, e);
        }
        
        #endregion

        #region Context Menu 1 (Primary)

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VarPrimary.RemoveSelectedID();
            VarTreaty.UpdateCurrentPosition();
        }
        private void addToPosition1_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.UpdatePositionContent(VarPrimary, true);
        }
        private void rewritePosition1_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.UpdatePositionContent(VarPrimary, false);
        }
        private void createCopiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VarPrimary.CreateCopy();
        }

        #endregion

        #region Context Menu 2 (Treaty)

        private void addNew2_Click(object sender, EventArgs e)
        {
            VarTreaty.GridID_CreateNew();
        }
        private void removeSelected2_Click(object sender, EventArgs e)
        {
            VarTreaty.RemoveSelectedID();
            VarTreaty.UpdateCurrentPosition();
        }
        private void appendToPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.UpdatePositionContent(null, true);
        }
        private void updatePositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.UpdatePositionContent(null, false);
        }

        #endregion

        #region Context Menu 3 (Position Content)

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.RemoveMultiSelectedPositionContent();
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabIdx == 2)
                VarTreaty.ClearBeforUpdatePositionContent();
            VarTreaty.RemoveMultiSelectedPositionContent(true);
        }

        #endregion

        #region Others

        private void saveScheduleToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabIdx == 1)
                VarPrimary.SaveScheduleTreeToFile();
            else if (tabIdx == 2)
                VarTreaty.SaveScheduleTreeToFile();
        }
        private void bttnFreeze_Click(object sender, EventArgs e)
        {
            VarTreaty.ChangeFreeze();
        }
        private void buttonOutput_Click(object sender, EventArgs e)
        {
            this.OutputInProcess = !this.OutputInProcess;

            ChangeOutputInProcess();
        }

        private void ChangeOutputInProcess()
        {
            if (this.toolStripProgress.InvokeRequired)
                this.toolStripProgress.Invoke(new Action(ChangeOutputInProcess));
            else
                this.buttonOutput.Text = (this.OutputInProcess) ? "Info while processing..." : "Info after completed";
        }

        #endregion

        private void InfoNumberThreads_Click(object sender, EventArgs e)
        {
            VarPrimary.InfoNamberThreadsAndTasks();
        }

        private void InfoNumberTasks_Click(object sender, EventArgs e)
        {
            VarPrimary.InfoNamberThreadsAndTasks();
        }

    }
}
