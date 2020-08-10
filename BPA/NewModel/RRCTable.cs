﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace BPA.NewModel
{
    class RRCTable : IEnumerable<RRCItem>
    {
        const string SHEET = "РРЦ";
        const string TABLE = "РРЦ";

        WS_DB db = new WS_DB();
        Excel.ListObject _table = null;

        public RRCTable()
        {
            Excel.Workbook wb = Globals.ThisWorkbook.InnerObject;
            Excel.Worksheet ws = wb.Sheets[SHEET];
            _table = ws.ListObjects[TABLE];
        }

        public IEnumerator<RRCItem> GetEnumerator()
        {
            foreach (TableRow item in db) yield return new RRCItem(item);
        }

        public int Load()
        {
            db.Load(_table);
            return db.RowCount();
        }

        public void Save() => db.Save();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
