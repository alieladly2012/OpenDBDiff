﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DBDiff.Schema;
using DBDiff.Schema.Attributes;
using DBDiff.Schema.Model;

namespace DBDiff.Front
{
    public partial class SchemaTreeView : UserControl
    {
        private ISchemaBase databaseSource;

        public delegate void SchemaHandler(string ObjectFullName);
        public event SchemaHandler OnSelectItem;

        public SchemaTreeView()
        {
            InitializeComponent();
        }

        public ISchemaBase DatabaseDestination { get; set; }

        public ISchemaBase DatabaseSource
        {
            get { return databaseSource; }
            set
            {
                databaseSource = value;
                if (value != null)
                {
                    RebuildSchemaTree();
                }
            }
        }
        public List<ISchemaBase> GetCheckedSchemas()
        {
            List<ISchemaBase> schemas = new List<ISchemaBase>();
            if (treeView1.CheckBoxes)
            {
                GetCheckedNodesToList(schemas, treeView1.Nodes);
            }
            return schemas;
        }
        public void SetCheckedSchemas(List<ISchemaBase> schemas)
        {
            SetCheckedNodesFromList(schemas, treeView1.Nodes);
        }
        private void GetCheckedNodesToList(List<ISchemaBase> schemas, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag != null)
                {
                    if (node.Checked)
                    {
                        schemas.Add(node.Tag as ISchemaBase);
                    }
                }
                GetCheckedNodesToList(schemas, node.Nodes);
            }
        }
        private void SetCheckedNodesFromList(List<ISchemaBase> schemas, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag != null)
                {
                    node.Checked = schemas.FirstOrDefault(sch => sch.Id == (node.Tag as ISchemaBase).Id) != null;
                }
                SetCheckedNodesFromList(schemas, node.Nodes);
            }
        }

        private void ReadProperties(Type item, TreeNodeCollection nodes, ISchemaBase schema)
        {
            PropertyInfo[] pi = item.GetProperties();
            nodes.Clear();
            foreach (PropertyInfo p in pi)
            {
                object[] attrs = p.GetCustomAttributes(typeof(ShowItemAttribute), true);
                if (attrs.Length > 0)
                {
                    ShowItemAttribute show = (ShowItemAttribute)attrs[0];
                    TreeNode node = nodes.Add(p.Name, show.Name);
                    node.ImageKey = "Folder";
                    ReadPropertyDetail(node, p, schema, show);
                }
            }
        }

        private void ReadPropertyDetail(TreeNode node, PropertyInfo p, ISchemaBase schema, ShowItemAttribute attr)
        {
            Color NodeColor = Color.Black;
            IList items = (IList)p.GetValue(schema, null);
            node.Text = node.Text + " (" + items.Count + ")";
            node.Nodes.Clear();
            foreach (ISchemaBase item in items)
            {
                if (CanNodeAdd(item))
                {
                    TreeNode subnode = node.Nodes.Add((attr.IsFullName ? item.FullName : item.Name));
                    if (item.Status == Enums.ObjectStatusType.DropStatus)
                    {
                        subnode.ForeColor = Color.Red;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Red ? Color.Red : Color.Plum);
                    }
                    if (item.Status == Enums.ObjectStatusType.CreateStatus)
                    {
                        subnode.ForeColor = Color.Green;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Green ? Color.Green : Color.Plum);
                    }
                    if ((item.HasState(Enums.ObjectStatusType.AlterStatus)) || (item.HasState(Enums.ObjectStatusType.DisabledStatus)))
                    {
                        subnode.ForeColor = Color.Blue;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Blue ? Color.Blue : Color.Plum);
                    }
                    if (item.HasState(Enums.ObjectStatusType.AlterWhitespaceStatus))
                    {
                        subnode.ForeColor = Color.DarkGoldenrod;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.DarkGoldenrod ? Color.DarkGoldenrod : Color.Plum);
                    }
                    if (item.HasState(Enums.ObjectStatusType.RebuildStatus))
                    {
                        subnode.ForeColor = Color.Purple;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Purple ? Color.Purple : Color.Plum);
                    }
                    subnode.Tag = item;
                    subnode.ImageKey = attr.Image;
                    subnode.SelectedImageKey = attr.Image;
                }
            }

            node.ForeColor = NodeColor;
        }

        private void RebuildSchemaTree()
        {
            treeView1.Visible = false;
            treeView1.Nodes.Clear();
            TreeNode databaseNode = treeView1.Nodes.Add(databaseSource.Name);
            ReadProperties(databaseSource.GetType(), databaseNode.Nodes, databaseSource);
            treeView1.Sort();
            databaseNode.ImageKey = "Database";
            databaseNode.Expand();
            treeView1.Visible = true;
        }

        private Boolean CanNodeAdd(ISchemaBase item)
        {
            if ((item.Status == Enums.ObjectStatusType.DropStatus) && (FilterMissingObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.CreateStatus) && (FilterNewObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.AlterStatus) && (FilterDiferentObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.AlterWhitespaceStatus) && (FilterDiferentObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.RebuildStatus) && (FilterDiferentObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.DisabledStatus) && (FilterDiferentObjects)) return true;
            if ((item.Status == Enums.ObjectStatusType.UpdateStatus) && (FilterDiferentObjects)) return true;
            return true;
        }

        public Boolean FilterNewObjects
        {
            get { return chkNew.Checked; }
            set { chkNew.Checked = value; }
        }

        public Boolean FilterMissingObjects
        {
            get { return chkOld.Checked; }
            set { chkOld.Checked = value; }
        }

        public Boolean FilterDiferentObjects
        {
            get { return chkDiferent.Checked; }
            set { chkDiferent.Checked = value; }
        }

        private void chkDiferent_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void chkOld_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void chkNew_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ISchemaBase item = ((ISchemaBase)e.Node.Tag);
            if (item != null)
            {
                if (item.ObjectType == Enums.ObjectType.Table
                    || item.ObjectType == Enums.ObjectType.View)
                    ReadProperties(item.GetType(), e.Node.Nodes, item);
                if (OnSelectItem != null) OnSelectItem(item.FullName);
            }
        }
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                foreach (TreeNode node in e.Node.Nodes)
                {
                    node.Checked = e.Node.Checked;
                }
            }
        }
    }
}
