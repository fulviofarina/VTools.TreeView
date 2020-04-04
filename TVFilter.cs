using Rsx.Dumb;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

#region NOT USED

namespace VTools
{
    //copy, not used!
    public class ucTVFilter : System.Windows.Forms.TreeView
    {
        private string minFilter;

        public string MinFilter
        {
            get { return minFilter; }
            set
            {
                minFilter = value;
            }
        }

        private BindingSource bs;
        private DataColumn[] tofilter;

        private DateTime lastFetch = DateTime.Now.AddSeconds(-60);

        public bool ShouldReload
        {
            get
            {
                return (DateTime.Now - lastFetch).TotalSeconds >= 20;
            }
        }

        public DataColumn[] ColsToFilter
        {
            get { return tofilter; }
            set
            {
                tofilter = value;
            }
        }

        public BindingSource BS
        {
            get { return bs; }
            set
            {
                bs = value;

                dt = Rsx.DGV.Control.GetDataSource<DataTable>(ref bs);
            }
        }

        private DataTable dt;
        // private bool filtered = false;

        public ucTVFilter()
            : base()
        {
            this.CheckBoxes = true;
        }

        private void NodeAfterCheck(object sender, TreeViewEventArgs e)
        {
            // filtered = false;

            if (e.Node.Tag != null)
            {
                this.AfterCheck -= (NodeAfterCheck);
                foreach (TreeNode n in e.Node.Nodes) n.Checked = e.Node.Checked;
                this.AfterCheck += (NodeAfterCheck);
            }

            try
            {
                IEnumerable<TreeNode> nodescheked = this.Nodes.OfType<TreeNode>();

                string filter = string.Empty;
                if (!string.IsNullOrWhiteSpace(minFilter)) filter = minFilter;

                int maincnt = nodescheked.Count();
                if (maincnt == 0) return;
                foreach (TreeNode n in nodescheked)
                {
                    IEnumerable<TreeNode> subs = n.Nodes.OfType<TreeNode>().Where(o => o.Checked).ToList();
                    int cnt = subs.Count();
                    int cc = cnt;
                    if (maincnt >= 1 && cnt != 0 && !filter.Equals(string.Empty)) filter += " AND ";
                    foreach (TreeNode s in subs)
                    {
                        filter += n.Text.Trim() + " = '" + s.Text.Trim() + "'";
                        if (cc > 1) filter += " OR ";
                        cc--;
                    }
                    // if (maincnt >= 1 && cnt != 0 ) filter += ")";
                    maincnt--;
                }

                bs.Filter = filter;
                // filtered = true;
            }
            catch (SystemException ex)
            {
            }
        }

        public void Build()
        {
            this.AfterCheck -= (NodeAfterCheck);

            IEnumerable<string> cols = tofilter.Select(o => o.ColumnName);
            HashSet<string> hcols = new HashSet<string>(cols.ToList());
            this.Nodes.Clear();
            foreach (string c in hcols)
            {
                TreeNode node = new TreeNode(c);
                node.Tag = dt.Columns[c];
                this.Nodes.Add(node);
            }
            this.AfterCheck += (NodeAfterCheck);
        }

        public void Fill()
        {
            this.AfterCheck -= (NodeAfterCheck);

            string filterCurr = bs.Filter;  //save old filter
            bs.Filter = minFilter;  //use basic filter

            //extract info
            IEnumerable<DataRow> rows = bs.List.OfType<DataRowView>().Select(o => o.Row);

            foreach (TreeNode n in this.Nodes)
            {
                // n.Checked = true;
                n.Nodes.Clear();

                IList<string> vals = Hash.HashFrom<string>(rows, n.Text).Select(o => o.Trim()).ToList();

                foreach (string sub in vals)
                {
                    if (string.IsNullOrWhiteSpace(sub)) continue;
                    TreeNode subnonode = new TreeNode(sub);
                    subnonode.Checked = false;
                    n.Nodes.Add(subnonode);
                }
                vals.Clear();
                vals = null;
            }

            //recover old filter
            bs.Filter = filterCurr;

            //extract info
            rows = bs.List.OfType<DataRowView>().Select(o => o.Row);

            foreach (TreeNode n in this.Nodes)
            {
                IList<string> vals = Hash.HashFrom<string>(rows, n.Text).Select(o => o.Trim()).ToList();
                foreach (string sub in vals)
                {
                    TreeNode subnode = n.Nodes.OfType<TreeNode>().FirstOrDefault(o => (o.Text.CompareTo(sub) == 0));
                    if (subnode != null) subnode.Checked = true;
                }
                vals.Clear();
                vals = null;
            }
            rows = null;

            lastFetch = DateTime.Now;

            this.AfterCheck += (NodeAfterCheck);
        }
    }
}

#endregion NOT USED

namespace VTools
{
    public class TVFilter : System.Windows.Forms.TreeView
    {
        private string resultFilter = string.Empty;

        public string ResultFilter
        {
            get { return resultFilter; }
            set
            {
                resultFilter = value;
            }
        }

        private string minFilter = string.Empty;

        public string MinFilter
        {
            get
            {
                return minFilter;
            }
            set
            {
                minFilter = value;
            }
        }

        private BindingSource bs;

        private DateTime lastFetch = DateTime.Now.AddSeconds(-60);

        public bool ShouldReload
        {
            get
            {
                return (DateTime.Now - lastFetch).TotalSeconds >= 20;
            }
        }

        public void Filter()
        {
            DataGridView dgv = this.Tag as DataGridView;
            if (dgv.CurrentCell == null) return;

            int colInd = dgv.CurrentCell.ColumnIndex;
            IEnumerable<DataGridViewColumn> others = dgv.Columns.OfType<DataGridViewColumn>();
            others = others.Where(c => c.Tag != null);

            IEnumerable<string> filters = null;
            if (others.Count() != 0)
            {
                Func<DataGridViewColumn, string> conv = x =>
                {
                    VTools.TVFilter t = x.Tag as VTools.TVFilter;
                    return t.ResultFilter;
                };
                // others = others.Where(c => c != col);
                filters = others.Select(conv).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            }
            others = null;

            string minimumfilter = this.minFilter;
            //auxiliar
            string newminFilt = minimumfilter;

            //put other filters...
            if (filters != null)
            {
                int cnt = filters.Count();
                if (cnt != 0) newminFilt += " AND ";
                foreach (string f in filters)
                {
                    newminFilt += f;
                    if (cnt > 1) newminFilt += " AND ";
                    cnt--;
                }
            }

            if (!resultFilter.Equals(string.Empty)) newminFilt += " AND (" + resultFilter + ")";
            if (bs.Filter.CompareTo(newminFilt) != 0)
            {
                bs.Filter = newminFilt;
            }
            if (dgv.CurrentRow == null) return;
            DataGridViewCell old = dgv[colInd, dgv.CurrentRow.Index];
            if (dgv.CurrentCell != old) dgv.CurrentCell = old;
        }

        private DataView view = null;

        public BindingSource BS
        {
            get { return bs; }
            set
            {
                bs = value;
                DataViewManager mgr = null;

                if (bs.List.GetType().Equals(typeof(DataViewManager)))
                {
                    mgr = bs.List as DataViewManager;

                    DataTable table = mgr.DataSet.Tables[bs.DataMember];
                    view = mgr.CreateDataView(table);
                }
                else view = bs.List as DataView;
            }
        }

        // private DataTable dt;

        private ToolStripItem all = null;
        private ToolStripItem none = null;
        private ToolStripItem fill = null;

        public ToolStripItem Fill
        {
            get { return fill; }
            set { fill = value; }
        }

        public TVFilter()
            : base()
        {
            this.CheckBoxes = true;

            ContextMenuStrip s = new ContextMenuStrip();
            fill = s.Items.Add("Fill");
            fill.Click += this.FillClick;
            s.Items.Add(new ToolStripSeparator());
            all = s.Items.Add("All");
            all.Click += cmsClick;
            none = s.Items.Add("None");
            none.Click += cmsClick;
            this.ContextMenuStrip = s;
        }

        /// <summary>
        /// This fills the tree nodes with posssible values and checks them...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">     </param>
        private void FillClick(object sender, EventArgs e)
        {
            if (field.Equals(string.Empty)) return;
            this.AfterCheck -= (NodeAfterCheck);
            IEnumerable<TreeNode> nodes = this.Nodes.OfType<TreeNode>();

            //original dataview
            view.RowFilter = minFilter;

            //extract info
            IEnumerable<DataRow> rows = view.OfType<DataRowView>().Select(o => o.Row);
            IList<string> vals = Hash.HashFrom<string>(rows, field).Select(o => o.Trim()).ToList();

            foreach (string sub in vals)
            {
                if (string.IsNullOrWhiteSpace(sub)) continue;
                TreeNode subnonode = nodes.FirstOrDefault(o => o.Text.CompareTo(sub) == 0);
                if (subnonode == null)
                {
                    subnonode = new TreeNode(sub);
                    this.Nodes.Add(subnonode);
                }
                subnonode.Checked = false;
            }
            vals.Clear();
            vals = null;

            //extract info from ACTUAL dataview
            rows = bs.List.OfType<DataRowView>().Select(o => o.Row);

            vals = Hash.HashFrom<string>(rows, field).Select(o => o.Trim()).ToList();
            foreach (string sub in vals)
            {
                TreeNode subnode = nodes.FirstOrDefault(o => (o.Text.CompareTo(sub) == 0));
                if (subnode != null) subnode.Checked = true;
            }
            vals.Clear();
            vals = null;

            rows = null;

            lastFetch = DateTime.Now;

            this.AfterCheck += (NodeAfterCheck);
        }

        private void cmsClick(object sender, EventArgs e)
        {
            this.AfterCheck -= (NodeAfterCheck);
            bool check = true;
            if (sender.Equals(none)) check = false;

            foreach (TreeNode n in this.Nodes) n.Checked = check;

            MakeFilter();
            Filter();

            this.AfterCheck += (NodeAfterCheck);
        }

        private void NodeAfterCheck(object sender, TreeViewEventArgs e)
        {
            MakeFilter();
            Filter();
        }

        private void MakeFilter()
        {
            try
            {
                string filter = string.Empty;
                // if (!string.IsNullOrWhiteSpace(minFilter)) filter = minFilter + " AND ";
                IEnumerable<TreeNode> nodescheked = this.Nodes.OfType<TreeNode>().Where(o => o.Checked).ToList();
                int maincnt = nodescheked.Count();
                // if (maincnt == 0) filter = minFilter;
                foreach (TreeNode n in nodescheked)
                {
                    filter += field + " = '" + n.Text.Trim() + "'";
                    if (maincnt > 1) filter += " OR ";
                    maincnt--;
                }
                this.resultFilter = filter;
            }
            catch (SystemException ex)
            {
            }
        }

        private string field = string.Empty;

        public string Field
        {
            get { return field; }
            set { field = value; }
        }
    }
}