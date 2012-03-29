﻿#region License
//The contents of this file are subject to the Mozilla Public License
//Version 1.1 (the "License"); you may not use this file except in
//compliance with the License. You may obtain a copy of the License at
//http://www.mozilla.org/MPL/
//Software distributed under the License is distributed on an "AS IS"
//basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
//License for the specific language governing rights and limitations
//under the License.
#endregion

using System;
using System.Reflection;
using Migrator.Framework;
using Migrator.Tools;
using System.Collections.Generic;

namespace DbMigrations
{
    /// <summary>
    /// Commande line utility to run the migrations
    /// </summary>
    /// </remarks>
    public class MigrationManager
    {
        private string _provider;
        private string _connectionString;
        private string _migrationsAssembly;
        private bool _list = false;
        private bool _trace = false;
        private bool _dryrun = false;
        private string _dumpTo;
        private long _migrateTo = -1;
        private string[] args;

        Migrator.Migrator _migrator;

        /// <summary>
        /// Builds a new console
        /// </summary>
        /// <param name="argv">Command line arguments</param>
        public MigrationManager()
        {
            string fullPath = System.Reflection.Assembly.GetAssembly(typeof(MigratorConsole)).Location;

            args = new string[] { "sqlite",
                String.Format(@"Data Source={0};Version=3;", ObLib.Domain.NHibernateHelper.DbFile),
                fullPath};

            ParseArguments(args);
        }

        public bool hasDetectedNewMigrations()
        {
            Migrator.Migrator mig = GetMigrator();
            if (mig.AppliedMigrations.Count < mig.MigrationsTypes.Count)
            {
                return true;
            }

            return false;
        }

        public void MigrateToLastRevision()
        {
            Migrator.Migrator mig = GetMigrator();

            if (mig.AppliedMigrations.Count == 0 &&
                databaseContainsProductionData())
            {
                CreateInitialSchema.pretendMigrationHasRunForProductionDatabasesOfPreviousVersion = true;
            }

            mig.MigrateToLastVersion();
        }

        protected bool databaseContainsProductionData()
        {
            bool flag = false;
            try
            {
                // if table researchers does not exist the command below will throw an exception
                var researchers = ObLib.Domain.Researcher.All();
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        public void Perform()
        {
            _migrateTo = -1;
            Run();
            Console.ReadLine();
        }

        /// <summary>
        /// Run the migrator's console
        /// </summary>
        /// <returns>-1 if error, else 0</returns>
        private int Run()
        {
            try
            {
                if (_list)
                    List();
                else if (_dumpTo != null)
                    Dump();
                else
                    Migrate();
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine("Invalid argument '{0}' : {1}", aex.ParamName, aex.Message);
                Console.WriteLine();
                PrintUsage();
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Runs the migrations.
        /// </summary>
        private void Migrate()
        {
            CheckArguments();

            Migrator.Migrator mig = GetMigrator();
            if (mig.DryRun)
                mig.Logger.Log("********** Dry run! Not actually applying changes. **********");

            if (_migrateTo == -1)
                mig.MigrateToLastVersion();
            else
                mig.MigrateTo(_migrateTo);
        }

        /// <summary>
        /// List migrations.
        /// </summary>
        private void List()
        {
            CheckArguments();

            Migrator.Migrator mig = GetMigrator();
            List<long> appliedMigrations = mig.AppliedMigrations;

            Console.WriteLine("Available migrations:");
            foreach (Type t in mig.MigrationsTypes)
            {
                long v = Migrator.MigrationLoader.GetMigrationVersion(t);
                Console.WriteLine("{0} {1} {2}",
                                  appliedMigrations.Contains(v) ? "=>" : "  ",
                                  v.ToString().PadLeft(3),
                                  StringUtils.ToHumanName(t.Name)
                                 );
            }
        }

        private void Dump()
        {
            CheckArguments();

            SchemaDumper dumper = new SchemaDumper(_provider, _connectionString);

            dumper.DumpTo(_dumpTo);
        }

        /// <summary>
        /// Show usage information and help.
        /// </summary>
        private void PrintUsage()
        {
            int tab = 17;
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;

            Console.WriteLine("Database migrator - v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Revision);
            Console.WriteLine();
            Console.WriteLine("usage:\nMigrator.Console.exe provider connectionString migrationsAssembly [options]");
            Console.WriteLine();
            Console.WriteLine("\t{0} {1}", "provider".PadRight(tab), "The database provider (SqlServer, MySql, Postgre)");
            Console.WriteLine("\t{0} {1}", "connectionString".PadRight(tab), "Connection string to the database");
            Console.WriteLine("\t{0} {1}", "migrationAssembly".PadRight(tab), "Path to the assembly containing the migrations");
            Console.WriteLine("Options:");
            Console.WriteLine("\t-{0}{1}", "version NO".PadRight(tab), "To specific version to migrate the database to");
            Console.WriteLine("\t-{0}{1}", "list".PadRight(tab), "List migrations");
            Console.WriteLine("\t-{0}{1}", "trace".PadRight(tab), "Show debug informations");
            Console.WriteLine("\t-{0}{1}", "dump FILE".PadRight(tab), "Dump the database schema as migration code");
            Console.WriteLine("\t-{0}{1}", "dryrun".PadRight(tab), "Simulation mode (don't actually apply/remove any migrations)");
            Console.WriteLine();
        }

        #region Private helper methods
        private void CheckArguments()
        {
            if (_connectionString == null)
                throw new ArgumentException("Connection string missing", "connectionString");
            if (_migrationsAssembly == null)
                throw new ArgumentException("Migrations assembly missing", "migrationsAssembly");
        }

        private Migrator.Migrator GetMigrator()
        {
            if (_migrator == null)
            {
                Assembly asm = Assembly.LoadFrom(_migrationsAssembly);

                _migrator = new Migrator.Migrator(_provider, _connectionString, asm, _trace);
                _migrator.args = args;
                _migrator.DryRun = _dryrun;
            }

            return _migrator;
        }

        private void ParseArguments(string[] argv)
        {
            for (int i = 0; i < argv.Length; i++)
            {
                if (argv[i].Equals("-list"))
                {
                    _list = true;
                }
                else if (argv[i].Equals("-trace"))
                {
                    _trace = true;
                }
                else if (argv[i].Equals("-dryrun"))
                {
                    _dryrun = true;
                }
                else if (argv[i].Equals("-version"))
                {
                    _migrateTo = long.Parse(argv[i + 1]);
                    i++;
                }
                else if (argv[i].Equals("-dump"))
                {
                    _dumpTo = argv[i + 1];
                    i++;
                }
                else
                {
                    if (i == 0) _provider = argv[i];
                    if (i == 1) _connectionString = argv[i];
                    if (i == 2) _migrationsAssembly = argv[i];
                }
            }
        }
        #endregion
    }
}
