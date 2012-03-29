﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ObLib.Domain;

namespace observador
{
    static class Program
    {
        public static string GetTitle()
        {
            Version version = new Version(Application.ProductVersion);
            return string.Format(
                Properties.Settings.Default.TitleFormat,
                Application.ProductName,
                version.Major,
                version.Minor,
                version.Build);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Cursor.Current = Cursors.WaitCursor;
            if (!NHibernateHelper.DatabaseExists)
            {
                if (MessageBox.Show("No Database Found. Click ok to Create new database. or cancel to exit.", GetTitle(), MessageBoxButtons.OKCancel) != DialogResult.OK)
                {
                    return;
                }
                NHibernateHelper.CreateDatabaseWithSeedData();
            }
            // hack to seed epm behaviors on test type on databases < 0.1.6
            if (BehavioralTestType.Epm == null)
            {
                ObLib.SeedData.PlusMazeBehavioralTestTypeAndBehaviors();
            }

            DbMigrations.MigrationManager migrationManager = new DbMigrations.MigrationManager();

            if (migrationManager.hasDetectedNewMigrations())
            {
                if (MessageBox.Show(@"The database schema has changed.
Click OK to backup the existing database and upgrade to the newer version.
NOTE: After the upgrade the database will no longer be accessible through previous versions of the Application", 
                "Upgrade Database", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    NHibernateHelper.BackupDatabase();
                    migrationManager.MigrateToLastRevision();
                }
            }
            Application.Run(new DashBoard());
        }
    }
}
