using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using WinForms = System.Windows.Forms;

namespace HPDTool
{
    public enum PipeAlignment
    {
        Center,
        Top,
        Bottom
    }

    internal class SystemTypeItem
    {
        public PipingSystemType SystemType { get; private set; }

        public SystemTypeItem(PipingSystemType systemType)
        {
            SystemType = systemType;
        }

        public override string ToString()
        {
            return SystemType.Name + " (" + SystemType.SystemClassification + ")";
        }
    }

    public class PipeForm : WinForms.Form
    {
        public string SelectedLayer { get; private set; }
        public PipeType SelectedPipeType { get; private set; }
        public PipingSystemType SelectedSystemType { get; private set; }
        public double Diameter { get; private set; }
        public PipeAlignment Alignment { get; private set; }

        private WinForms.ComboBox layerBox;
        private WinForms.ComboBox pipeTypeBox;
        private WinForms.ComboBox systemTypeBox;
        private WinForms.ComboBox diameterBox;
        private WinForms.ComboBox alignBox;

        private readonly Document _doc;

        public PipeForm(
            List<string> layers,
            List<PipeType> pipeTypes,
            List<PipingSystemType> systemTypes,
            Document doc)
        {
            _doc = doc;

            Text = "Create Pipes from CAD";
            Width = 340;
            Height = 380;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // CAD Layer
            Controls.Add(new WinForms.Label()
            {
                Text = "CAD Layer",
                Left = 10,
                Top = 15,
                Width = 120
            });

            layerBox = new WinForms.ComboBox()
            {
                Left = 10,
                Top = 35,
                Width = 300,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };

            foreach (string l in layers)
                layerBox.Items.Add(l);

            if (layerBox.Items.Count > 0)
                layerBox.SelectedIndex = 0;

            Controls.Add(layerBox);

            // Pipe Type
            Controls.Add(new WinForms.Label()
            {
                Text = "Pipe Type",
                Left = 10,
                Top = 70,
                Width = 120
            });

            pipeTypeBox = new WinForms.ComboBox()
            {
                Left = 10,
                Top = 90,
                Width = 300,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };

            foreach (PipeType p in pipeTypes)
                pipeTypeBox.Items.Add(p);

            pipeTypeBox.DisplayMember = "Name";

            if (pipeTypeBox.Items.Count > 0)
                pipeTypeBox.SelectedIndex = 0;

            Controls.Add(pipeTypeBox);

            // System Type
            Controls.Add(new WinForms.Label()
            {
                Text = "System Type",
                Left = 10,
                Top = 125,
                Width = 120
            });

            systemTypeBox = new WinForms.ComboBox()
            {
                Left = 10,
                Top = 145,
                Width = 300,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };

            foreach (PipingSystemType systemType in systemTypes)
                systemTypeBox.Items.Add(new SystemTypeItem(systemType));

            if (systemTypeBox.Items.Count > 0)
                systemTypeBox.SelectedIndex = 0;

            Controls.Add(systemTypeBox);

            // Diameter
            Controls.Add(new WinForms.Label()
            {
                Text = "Diameter (mm)",
                Left = 10,
                Top = 180,
                Width = 120
            });

            diameterBox = new WinForms.ComboBox()
            {
                Left = 10,
                Top = 200,
                Width = 300,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };

            Controls.Add(diameterBox);

            // Pipe Position
            Controls.Add(new WinForms.Label()
            {
                Text = "Pipe Position",
                Left = 10,
                Top = 235,
                Width = 120
            });

            alignBox = new WinForms.ComboBox()
            {
                Left = 10,
                Top = 255,
                Width = 300,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };

            alignBox.Items.Add("Center");
            alignBox.Items.Add("Top");
            alignBox.Items.Add("Bottom");
            alignBox.SelectedIndex = 0;

            Controls.Add(alignBox);

            // Pipe type change => reload diameters
            pipeTypeBox.SelectedIndexChanged += (s, e) =>
            {
                LoadDiameters();
            };

            // Initial load
            LoadDiameters();

            // Button
            WinForms.Button btn = new WinForms.Button()
            {
                Text = "Create Pipes",
                Left = 10,
                Top = 300,
                Width = 120,
                Height = 30
            };

            btn.Click += (s, e) =>
            {
                if (layerBox.SelectedItem == null ||
                    pipeTypeBox.SelectedItem == null ||
                    systemTypeBox.SelectedItem == null ||
                    diameterBox.SelectedItem == null)
                {
                    WinForms.MessageBox.Show("Please select all inputs.");
                    return;
                }

                SelectedLayer = layerBox.SelectedItem.ToString();
                SelectedPipeType = pipeTypeBox.SelectedItem as PipeType;
                SelectedSystemType = ((SystemTypeItem)systemTypeBox.SelectedItem).SystemType;
                Diameter = Convert.ToDouble(diameterBox.SelectedItem);
                Alignment = (PipeAlignment)alignBox.SelectedIndex;

                DialogResult = WinForms.DialogResult.OK;
                Close();
            };

            Controls.Add(btn);
        }

        private void LoadDiameters()
        {
            diameterBox.Items.Clear();

            PipeType pt = pipeTypeBox.SelectedItem as PipeType;
            if (pt == null)
                return;

            List<double> sizes = GetPipeSizes(pt);

            foreach (double d in sizes)
                diameterBox.Items.Add(d);

            if (diameterBox.Items.Count > 0)
                diameterBox.SelectedIndex = 0;
        }

        private List<double> GetPipeSizes(PipeType pipeType)
        {
            List<double> sizes = new List<double>();

            RoutingPreferenceManager rpm = pipeType.RoutingPreferenceManager;
            int count = rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

            for (int i = 0; i < count; i++)
            {
                RoutingPreferenceRule rule =
                    rpm.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                PipeSegment seg = _doc.GetElement(rule.MEPPartId) as PipeSegment;

                if (seg != null)
                {
                    foreach (MEPSize size in seg.GetSizes())
                    {
                        double dia = UnitUtils.ConvertFromInternalUnits(
                            size.NominalDiameter,
                            UnitTypeId.Millimeters);

                        double rounded = Math.Round(dia);

                        if (!sizes.Contains(rounded))
                            sizes.Add(rounded);
                    }
                }
            }

            return sizes.OrderBy(x => x).ToList();
        }
    }
}