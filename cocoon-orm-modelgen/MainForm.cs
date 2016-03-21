﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CSharp;

namespace Cocoon.ORM.ModelGen
{
    public partial class MainForm : Form
    {

        CocoonORM db;
        Dictionary<string, Type> sqlToCLRMappings;
        Dictionary<Type, string> CLRToPrimitiveMappings;

        public MainForm()
        {
            InitializeComponent();

            sqlToCLRMappings = new Dictionary<string, Type>();
            sqlToCLRMappings.Add("bigint", typeof(Int64));
            sqlToCLRMappings.Add("binary", typeof(Byte[]));
            sqlToCLRMappings.Add("bit", typeof(Boolean));
            sqlToCLRMappings.Add("char", typeof(String));
            sqlToCLRMappings.Add("date", typeof(DateTime));
            sqlToCLRMappings.Add("datetime", typeof(DateTime));
            sqlToCLRMappings.Add("datetime2", typeof(DateTime));
            sqlToCLRMappings.Add("datetimeoffset", typeof(DateTimeOffset));
            sqlToCLRMappings.Add("decimal", typeof(Decimal));
            sqlToCLRMappings.Add("float", typeof(Double));
            sqlToCLRMappings.Add("image", typeof(Byte[]));
            sqlToCLRMappings.Add("int", typeof(Int32));
            sqlToCLRMappings.Add("money", typeof(Decimal));
            sqlToCLRMappings.Add("nchar", typeof(String));
            sqlToCLRMappings.Add("ntext", typeof(String));
            sqlToCLRMappings.Add("numeric", typeof(Decimal));
            sqlToCLRMappings.Add("nvarchar", typeof(String));
            sqlToCLRMappings.Add("real", typeof(Single));
            sqlToCLRMappings.Add("rowversion", typeof(Byte[]));
            sqlToCLRMappings.Add("smalldatetime", typeof(DateTime));
            sqlToCLRMappings.Add("smallint", typeof(Int16));
            sqlToCLRMappings.Add("smallmoney", typeof(Decimal));
            sqlToCLRMappings.Add("text", typeof(String));
            sqlToCLRMappings.Add("time", typeof(TimeSpan));
            sqlToCLRMappings.Add("timestamp", typeof(Byte[]));
            sqlToCLRMappings.Add("tinyint", typeof(Byte));
            sqlToCLRMappings.Add("uniqueidentifier", typeof(Guid));
            sqlToCLRMappings.Add("varbinary", typeof(Byte[]));
            sqlToCLRMappings.Add("varchar", typeof(String));

            CLRToPrimitiveMappings = new Dictionary<Type, string>();
            CLRToPrimitiveMappings.Add(typeof(Int64), "long");
            CLRToPrimitiveMappings.Add(typeof(Byte[]), "byte[]");
            CLRToPrimitiveMappings.Add(typeof(Boolean), "bool");
            CLRToPrimitiveMappings.Add(typeof(Decimal), "decimal");
            CLRToPrimitiveMappings.Add(typeof(Int32), "int");
            CLRToPrimitiveMappings.Add(typeof(String), "string");
            CLRToPrimitiveMappings.Add(typeof(Int16), "short");
            CLRToPrimitiveMappings.Add(typeof(Byte), "byte");

        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {

            Cursor = Cursors.WaitCursor;

            try
            {

                Ping ping = new Ping();
                string server = ConnectionStringParser.GetServerName(ConnectionStringTextBox.Text);

                if (server.Contains(","))
                    server = server.Substring(0, server.LastIndexOf(","));
                else if (server.Contains(":"))
                    server = server.Substring(0, server.LastIndexOf(":"));

                PingReply reply = ping.Send(server);

                db = new CocoonORM(ConnectionStringTextBox.Text);

                var tables = db.ExecuteSQLList<SysTable>("select name, object_id from sys.tables order by Name");

                TablesListBox.Items.Clear();
                foreach (var table in tables)
                    TablesListBox.Items.Add(table);


            }
            catch
            {
                MessageBox.Show("Failed to connect");
            }

            Cursor = Cursors.Default;


        }

        private void TablesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (TablesListBox.SelectedItems.Count == 0)
                return;

            Cursor = Cursors.WaitCursor;

            List<string> classes = new List<string>();
            foreach (SysTable table in TablesListBox.SelectedItems)
            {

                if(table.columns == null)
                    table.columns = db.ExecuteSQLList<SysColumn>(@"
                    select sys.columns.name, sys.columns.is_identity, sys.default_constraints.definition, sys.columns.is_nullable, sys.types.name as [type], cast(case when sys.indexes.is_primary_key is null then 0 else 1 end as bit) as is_primary_key from sys.columns 
                    join sys.types on sys.types.system_type_id = sys.columns.system_type_id
                    left join sys.index_columns on sys.index_columns.object_id = sys.columns.object_id and sys.index_columns.column_id = sys.columns.column_id
                    left join sys.indexes on sys.indexes.object_id = sys.columns.object_id and sys.indexes.index_id = sys.index_columns.index_id
                    left join sys.default_constraints on sys.default_constraints.object_id = sys.columns.default_object_id
                    where sys.columns.object_id = @object_id order by is_primary_key desc, sys.columns.name", new { object_id = table.object_id });

                List<string> properties = new List<string>();
                foreach (SysColumn column in table.columns)
                {

                    string type = "unknown";

                    if (sqlToCLRMappings.ContainsKey(column.type))
                    {

                        if (CLRToPrimitiveMappings.ContainsKey(sqlToCLRMappings[column.type]))
                            type = CLRToPrimitiveMappings[sqlToCLRMappings[column.type]];
                        else
                            type = sqlToCLRMappings[column.type].Name;

                        if (column.is_nullable && sqlToCLRMappings[column.type].IsValueType)
                            type = type + "?";

                    }

                    properties.Add(string.Format("\t[Column{0}{1}{2}]\r\n\tpublic {3} {4} {{ get; set; }}",
                        column.is_primary_key ? ", PrimaryKey" : "",
                        column.is_identity ? ", IgnoreOnInsert, IgnoreOnUpdate" : "",
                        column.definition != null ? ", IgnoreOnInsert, IgnoreOnUpdate" : "",
                        type,
                        column.name));

                }

                classes.Add(string.Format("[Table]\r\npublic class {0}\r\n{{\r\n\r\n{1}\r\n\r\n}}", table.name, string.Join("\r\n\r\n", properties)));

            }

            ClassTextBox.Text = string.Join("\r\n\r\n", classes);

            Cursor = Cursors.Default;

        }
    }

    [Table]
    class SysTable
    {

        [Column]
        public string name { get; set; }

        [Column]
        public int object_id { get; set; }

        public override string ToString()
        {
            return name;
        }

        public List<SysColumn> columns;

    }

    [Table]
    class SysColumn
    {

        [Column]
        public string name { get; set; }

        [Column]
        public bool is_identity { get; set; }

        [Column]
        public bool is_nullable { get; set; }

        [Column]
        public string type { get; set; }

        [Column]
        public bool is_primary_key { get; set; }

        [Column]
        public string definition { get; set; }

        public override string ToString()
        {
            return name;
        }

    }

}