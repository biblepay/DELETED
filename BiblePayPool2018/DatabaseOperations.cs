using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace BiblePayPool2018
{
    public class DatabaseOperations
    {
        public struct DataColumn
        {
            public string Name;
            public string DataType;
        }

        public struct DataTable
        {
            public string Name;
            public List<DataColumn> Columns;
        }

        // BiblePay - The purpose of this class is to : Add ability to Migrate data from Prod to Test, and to make backups and restores
        public List<DataTable> GetSchemas(string sFromDatabaseName)
        {
            Microsoft.SqlServer.Management.Common.ServerConnection se = new ServerConnection();
            USGDFramework.Data d = new USGDFramework.Data();
            se.ConnectionString = d.sSQLConn;
            se.Connect();
            Server s = new Server(se);
            Database db = new Database();
            db = s.Databases[sFromDatabaseName];
            StringBuilder sb = new StringBuilder();
            List<DataTable> lTables = new List<DataTable>();

            foreach (Table tbl in db.Tables)
            {
                DataTable dt = new DataTable();
                dt.Name = tbl.Name;
                List<DataColumn> dcs = new List<DataColumn>();
                for (int i = 0; i < tbl.Columns.Count; i++)
                {
                    DataColumn dc = new DataColumn();
                    dc.Name = tbl.Columns[i].Name;
                    dc.DataType = tbl.Columns[i].DataType.ToString();
                    dcs.Add(dc);
                }
                dt.Columns = dcs;
                lTables.Add(dt);
               
            }
            return lTables;
        }
        public string GetDelimitedFieldList(DataTable lt, bool IncludeSingleQuote)
        {
            string sFields = "";
            string sSingleQuote = IncludeSingleQuote ? "'" : "";
            foreach (DataColumn dc in lt.Columns)
            {

                sFields += sSingleQuote + dc.Name + sSingleQuote + ",";
            }
            sFields = sFields.Substring(0, sFields.Length - 1);
            return sFields;
        }


        public void ReplicateData(string sFromDatabaseName, string ToDatabaseName)
        {
            // Create the target Database
            string sql = "CREATE DATABASE " + ToDatabaseName;
            USGDFramework.Data d = new USGDFramework.Data();
            d.Exec(sql);
            string sSchema = ScriptDatabase(sFromDatabaseName, ToDatabaseName);
            sql = "USE " + ToDatabaseName;
            d.Exec(sql);
            d.Exec(sSchema);
            // For each table in Source database, insert a mirror image of data in the destination, that is NOT in the destination already.
            List<DataTable> lTables = GetSchemas(sFromDatabaseName);
            foreach (DataTable lTable in lTables)
            {
                string sTargetTable = ToDatabaseName + "..[" + lTable.Name + "]";
                string sSourceTable = sFromDatabaseName + "..[" + lTable.Name + "]";
                string sSourceFields = GetDelimitedFieldList(lTable,false);
                string sTargetFields = GetDelimitedFieldList(lTable,true);
                string sInsert = "INSERT INTO " + sTargetTable + " (" + sSourceFields + ")";
                string sFrom = "SELECT " + sSourceFields + " FROM " + sSourceTable + " WHERE ID NOT IN (Select ID from " + sTargetTable + ")";
                string sSQL = sInsert + "\r\n" + sFrom;
                d.Exec(sSQL);
            }
        }

        private string SqlCopyTable(string sFromDatabaseName,string ToDatabaseName, string sTable)
        {
            return String.Empty;
        }


        public string ScriptDatabase(string sCurrentDatabaseName, string sNewDatabaseName)
        {
            Microsoft.SqlServer.Management.Common.ServerConnection  se = new ServerConnection();
            USGDFramework.Data d = new USGDFramework.Data();
            se.ConnectionString = d.sSQLConn;
            se.Connect();
            Server s = new Server(se);
            Database db = new Database();
            db = s.Databases[sCurrentDatabaseName];
            StringBuilder sb = new StringBuilder();

            foreach(Table tbl in db.Tables)
            {
                ScriptingOptions options = new ScriptingOptions();
                options.ClusteredIndexes = true;
                options.Default = true;
                options.DriAll = true;
                options.Indexes = true;
                options.IncludeHeaders = true;

                StringCollection coll = tbl.Script(options);
                foreach (string str in coll)
                {
                    sb.Append(str);
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);

                }
            }
            System.IO.StreamWriter fs = System.IO.File.CreateText("c:\\temp\\output.txt");
            fs.Write(sb.ToString());
            fs.Close();
            return sb.ToString();

        }


        public void FullBackup()
        {
            Backup bkpDBFull = new Backup();
            bkpDBFull.Action = BackupActionType.Database;
            bkpDBFull.Database = "biblepaypool";
            bkpDBFull.Devices.AddDevice(@"c:\MyBiblePayPackup.bak", DeviceType.File);
            bkpDBFull.BackupSetName = "Bible Db Backup";
            bkpDBFull.BackupSetDescription = "Bible Db backup - Full Backup";
            bkpDBFull.Initialize = false;
            bkpDBFull.PercentComplete += CompletionStatusInPercent;
            bkpDBFull.Complete += Backup_Completed;
            Microsoft.SqlServer.Management.Common.ServerConnection  se = new ServerConnection();
            USGDFramework.Data d = new USGDFramework.Data();
            se.ConnectionString = d.sSQLConn;
            se.Connect();
            Server s = new Server(se);
            bkpDBFull.SqlBackup(s);
        }


        public void RestoreDatabase()
        {
            Restore restDb = new Restore();
            restDb.Action = RestoreActionType.Database;
            restDb.Database = "biblepaypool";
            restDb.Devices.AddDevice(@"c:\MyBiblePayPackup.bak", DeviceType.File);
            restDb.PercentComplete  += CompletionStatusInPercent;
            restDb.Complete += Backup_Completed;
            Microsoft.SqlServer.Management.Common.ServerConnection se = new ServerConnection();
            USGDFramework.Data d = new USGDFramework.Data();
            se.ConnectionString = d.sSQLConn;
            se.Connect();
            Server s = new Server(se);
            restDb.SqlRestore(s);
        }

        private static void CompletionStatusInPercent(object sender, PercentCompleteEventArgs args)
        {
            Console.Clear();
            Console.WriteLine("Percent completed: {0}%.", args.Percent);
        }
        private static void Backup_Completed(object sender, ServerMessageEventArgs args)
        {
            Console.WriteLine("..Backup completed.");
            Console.WriteLine(args.Error.Message);
        }
        private static void Restore_Completed(object sender, ServerMessageEventArgs args)
        {
            Console.WriteLine("..Restore completed.");
            Console.WriteLine(args.Error.Message);
        }

    }
}