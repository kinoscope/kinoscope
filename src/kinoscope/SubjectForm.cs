﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ObLib.Domain;
using ObLib;

namespace kinoscope
{
    public partial class SubjectForm : ObWin.Form
    {
        private Subject _subject = null;

        public SubjectForm()
        {
            InitializeComponent();

            List<SubjectGroup> comboBoxSubjectGroups = new List<SubjectGroup>();

            SubjectGroup emptySubjectGroup = new SubjectGroup();
            emptySubjectGroup.Id = -1;
            emptySubjectGroup.Name = "<None>";
            comboBoxSubjectGroups.Add(emptySubjectGroup);
            comboBoxSubjectGroups.AddRange(Researcher.Current.ActiveProject.SubjectGroups);

            cbSubjectGroup.DataSource = comboBoxSubjectGroups;
            cbSex.SelectedIndex = 1;
        }

        public SubjectForm(Subject subject)
            : this()
        {
            if (subject != null)
            {
                _subject = subject;
                txtCode.Text = subject.Code;

                cbSubjectGroup.SelectedItem = subject.SubjectGroup ?? cbSubjectGroup.Items[0];

                txtStrain.Text = subject.Strain;
                cbSex.SelectedItem = subject.Sex;
                dtDob.Value = subject.DateOfBirth;
                txtOrigin.Text = subject.Origin;
                txtWeight.Text = subject.Weight.ToString();

                bSave.Enabled = false;
                AcceptButton = bSaveAndClose;
            }
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            if (Save())
            {
                txtCode.Text = SuggestedNextSubjectCode(txtCode.Text);
                txtCode.Focus();
            }
        }

        private void bSaveAndClose_Click(object sender, EventArgs e)
        {
            if (Save())
            {
                Close();
            }
        }

        private bool Save()
        {
            try
            {
                if (!ValidateChildren())
                {
                    ShowInputError();
                    return false;
                }

                Subject subject = _subject ?? new Subject();

                subject.Code = txtCode.Text;
                subject.Strain = txtStrain.Text;
                subject.Sex = cbSex.SelectedItem.ToString();
                subject.DateOfBirth = dtDob.Value;
                subject.Origin = txtOrigin.Text;
                subject.Weight = txtWeight.Text == "" ? (Decimal?)null : Decimal.Parse(txtWeight.Text);

                if (_subject == null)
                {
                    Researcher.Current.ActiveProject.AddSubject(subject);
                    Researcher.Current.ActiveProject.Save();
                    if (CallerForm is ListForm<Subject>)
                    {
                        (CallerForm as ListForm<Subject>).OrderRefresh(subject);
                    }
                }
                else
                {
                    _subject.Save();
                }

                UpdateSubjectGroupMemberships(subject);

                return true;
            }
            catch (Exception ex)
            {
                FailWithError(ex);
                return false;
            }
        }

        private void UpdateSubjectGroupMemberships(Subject subject)
        {
            SubjectGroup oldSubjectGroup = subject.SubjectGroup;
            SubjectGroup newSubjectGroup = (SubjectGroup)cbSubjectGroup.SelectedItem;
            if (newSubjectGroup.Id == -1)
            {
                newSubjectGroup = null;
            }

            if (subject.SubjectGroup == null)
            {
                if (newSubjectGroup != null)
                {
                    newSubjectGroup.AddSubject(subject);
                    newSubjectGroup.Save();
                }
            }
            else if (newSubjectGroup == null)
            {
                subject.SubjectGroup = null;
                oldSubjectGroup.Subjects.Remove(subject);
                oldSubjectGroup.Save();
            }
            else if (subject.SubjectGroup.Id != newSubjectGroup.Id)
            {
                oldSubjectGroup.Subjects.Remove(subject);
                newSubjectGroup.AddSubject(subject);

                oldSubjectGroup.Save();
                newSubjectGroup.Save();
            }
        }

        private string SuggestedNextSubjectCode(string subjectCode)
        {
            int numericSubjectCode;
            if (int.TryParse(subjectCode, out numericSubjectCode))
            {
                subjectCode = (++numericSubjectCode).ToString();
            }

            return subjectCode;
        }
        private void txtWeight_Validating(object sender, CancelEventArgs e)
        {
            Decimal weight;
            if (txtWeight.Text != ""
                && (!Decimal.TryParse(txtWeight.Text, System.Globalization.NumberStyles.Float, null, out weight)
                    || weight < 0))
            {
                e.Cancel = true;
                errorProvider.SetError(txtWeight, "Invalid weight. Must be a non-negative number.");
            }
            else
            {
                errorProvider.SetError(txtWeight, "");
            }
        }

        private void txtCode_Validating(object sender, CancelEventArgs e)
        {
            if (txtCode.Text == "")
            {
                e.Cancel = true;
                errorProvider.SetError(txtCode, "Subject code is required.");
            }
            else if (Researcher.Current.ActiveProject.Subjects.FirstOrDefault(
                (item) => item.Code == txtCode.Text && (_subject == null || item.Id != _subject.Id)
                ) != null)
            {
                e.Cancel = true;
                errorProvider.SetError(txtCode, "A subject with the same code already exists in the active project.");
            }
            else
            {
                errorProvider.SetError(txtCode, "");
            }
        }
    }
}
