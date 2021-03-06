﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ObLib.Domain;

namespace kinoscope
{
    public partial class AdminResearcherForm : ObWin.Form
    {
        private Researcher _researcher = null;

        public AdminResearcherForm()
        {
            InitializeComponent();
        }

        public AdminResearcherForm(Researcher researcher)
            : this()
        {
            if (researcher != null)
            {
                _researcher = researcher;
                txtUsername.Text = researcher.Username;
                txtPassword.Text = researcher.Password;
                txtConfirmPassword.Text = researcher.Password;
                if (researcher.IsAdmin)
                {
                    txtUsername.Enabled = false;
                }
            }
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateChildren())
                {
                    ShowInputError();
                    return;
                }

                Researcher researcher = _researcher ?? new Researcher();
                researcher.Username = txtUsername.Text;
                researcher.Password = txtPassword.Text;
                researcher.Save();

                if (_researcher == null)
                {
                    if (CallerForm is ListForm<Researcher>)
                    {
                        (CallerForm as ListForm<Researcher>).OrderRefresh(researcher);
                    }
                }
                this.Close();
            }
            catch (Exception ex)
            {
                FailWithError(ex);
            }
        }

        private void txtUsername_Validating(object sender, CancelEventArgs e)
        {
            Researcher researcher;

            if (txtUsername.Text.Length < 4)
            {
                e.Cancel = true;
                errorProvider.SetError(txtUsername, "Username is not long enough.");
            }
            else if ((researcher = Researcher.Find(txtUsername.Text)) != null
                && (_researcher == null || researcher.Id != _researcher.Id))
            {
                e.Cancel = true;
                errorProvider.SetError(txtUsername, "A researcher with the same user name already exists.");
            }
            else
            {
                errorProvider.SetError(txtUsername, "");
            }
        }

        private void txtConfirmPassword_Validating(object sender, CancelEventArgs e)
        {
            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                e.Cancel = true;
                errorProvider.SetError(txtConfirmPassword, "Passwords don't match.");
            }
            else
            {
                errorProvider.SetError(txtConfirmPassword, "");
            }
        }
    }
}
