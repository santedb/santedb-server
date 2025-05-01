/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 */
using SanteDB.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    [ExcludeFromCodeCoverage]
    public partial class frmProgress : Form
    {

        /// <summary>
        /// Completed
        /// </summary>
        private List<object> m_completed = new List<object>();

        public frmProgress()
        {
            InitializeComponent();
            this.PopulateTasks();
            var image = imlImage.Images[0].Clone() as Image;
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            imlImage.Images[1] = image.Clone() as Image;
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            imlImage.Images[2] = image.Clone() as Image;
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            imlImage.Images[3] = image.Clone() as Image;

        }

        /// <summary>
        /// Populate the initial tasks
        /// </summary>
        private void PopulateTasks()
        {
            lsvStatus.Items.Clear();
            foreach (var tsk in ConfigurationContext.Current.ConfigurationTasks)
            {
                var lvi = lsvStatus.Items.Add(tsk.Name);
                lvi.SubItems.Add("");
                lvi.Tag = tsk;
            }

            var itm = lsvStatus.Items.OfType<ListViewItem>().FirstOrDefault(o => o.Tag == ConfigurationContext.Current.ConfigurationTasks.First());
            if(itm != null)
            {
                itm.ImageIndex = 0;
            }
            ConfigurationContext.Current.ConfigurationTasks.CollectionChanged += ConfigurationTasks_CollectionChanged;
        }

        /// <summary>
        /// Task list has changed
        /// </summary>
        private void ConfigurationTasks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // The removed item gets a check
                    this.m_completed.AddRange(e.OldItems.OfType<Object>());
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public int ActionStatus { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        public string ActionStatusText { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal int OverallStatus { get; set; }

        /// <summary>
        /// Gets or sets the status text
        /// </summary>
        internal string OverallStatusText { get; set; }

        /// <summary>
        /// Timer has ticked
        /// </summary>
        private void tmrPB_Tick(object sender, EventArgs e)
        {

            this.lblOverall.Text = $"{this.OverallStatusText} ({this.OverallStatus}%)";
            this.pgMain.Value = this.OverallStatus;

            foreach (var itm in lsvStatus.Items.OfType<ListViewItem>().Where(o => this.m_completed.IndexOf(o.Tag) > -1 && o.ImageIndex != 4))
            {
                itm.SubItems[1].Text = "100";
                itm.ImageIndex = 4;
            }

            if (ConfigurationContext.Current.ConfigurationTasks.Count > 0)
            {
                var lvi = lsvStatus.Items.OfType<ListViewItem>().FirstOrDefault(o => o.Tag == ConfigurationContext.Current.ConfigurationTasks.First());
                if (lvi.ImageIndex < 0)
                {
                    lvi.ImageIndex = 0;
                }
                else
                {
                    lvi.ImageIndex = ((lvi.ImageIndex + 1) % 4);
                }

                lvi.EnsureVisible();
                lvi.SubItems[1].Text = this.ActionStatus.ToString();
            }
            else
            {
                pgMain.Value = 100;
            }
        }

        /// <summary>
        /// Draw the sub-item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lsvStatus_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 1 && !String.IsNullOrEmpty(e.SubItem.Text))
            {
                var txtSize = e.Graphics.MeasureString($"100%", SystemFonts.DefaultFont);
                var pbWidth = e.Bounds.Width - txtSize.Width - 10;

                if (ProgressBarRenderer.IsSupported)
                {
                    ProgressBarRenderer.DrawHorizontalBar(e.Graphics, new Rectangle(e.Bounds.Left + 2, e.Bounds.Top + 2, (int)pbWidth, e.Bounds.Height - 4));
                    ProgressBarRenderer.DrawHorizontalChunks(e.Graphics, new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 4, (int)((pbWidth - 4) * (float.Parse(e.SubItem.Text) / 100.0f)), e.Bounds.Height - 8));
                }
                else
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 4, (int)((pbWidth - 4) * (float.Parse(e.SubItem.Text) / 100.0f)), e.Bounds.Height - 8));
                    e.Graphics.DrawRectangle(SystemPens.ControlDarkDark, e.Bounds.Left, e.Bounds.Top + 2, pbWidth, e.Bounds.Height - 3);
                }
                e.Graphics.DrawString($"{e.SubItem.Text}%", SystemFonts.DefaultFont, SystemBrushes.ControlText, e.Bounds.Left + pbWidth + 10, e.Bounds.Top + ((e.Bounds.Height - txtSize.Height) / 2));
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        /// <summary>
        /// Draw column header
        /// </summary>
        private void lsvStatus_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
    }
}
