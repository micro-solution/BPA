﻿using Excel = Microsoft.Office.Interop.Excel;
//using Application = Microsoft.Office.Interop.Excel.Application;
using System;
using System.Windows.Forms;


namespace BPA.Modules
{
    class FileBase
    {
        public readonly Excel.Application Application = Globals.ThisWorkbook.Application;
        protected string FileName = "";
        protected string FileSheetName = "";
        protected int FileHeaderRow = 1;

        protected virtual void OnActionStart(string str)
        {
            ActionStart?.Invoke(str);
        }
        protected virtual void OnActionDone(int i)
        {
            ActionDone?.Invoke(i);
        }
        /// <summary>
        /// Событие начала задачи
        /// </summary>
        public event Action<string> ActionStart;
        /// <summary>
        /// Событие завершения задачи
        /// </summary>
        public event Action<int> ActionDone;

        public int CountActions => LastRow - FileHeaderRow;
        public bool IsCancel = false;

        public bool IsOpen { get; set; } = false;

        public Excel.Workbook Workbook
        {
            get
            {
                if (_Workbook == null)
                {
                    try
                    {
                        _Workbook = Application.Workbooks.Open(FileName);
                    }
                    catch
                    {
                        _Workbook = null;
                    }
                }
                return _Workbook;
            }
            set
            {
                _Workbook = value;
            }
        }
        private Excel.Workbook _Workbook;

        public Excel.Worksheet worksheet
        {
            get
            {
                if (_worksheet == null)
                {
                    try
                    {
                        _worksheet = FileSheetName != "" ? Workbook?.Sheets[FileSheetName] : Workbook?.Sheets[1];
                    }
                    catch
                    {
                        throw new ApplicationException($"Лист \"{ FileSheetName }\" в книге { FileName } не найден!");
                    }
                }
                return _worksheet;
            }
            set
            {
                _worksheet = value;
            }
        }
        private Excel.Worksheet _worksheet;

        public int LastRow
        {
            get
            {
                if (_LastRow == 0) _LastRow = worksheet.UsedRange.Row + worksheet.UsedRange.Rows.Count - 1;
                return _LastRow;
            }
        }
        private int _LastRow = 0;

        public int LastColumn
        {
            get
            {
                if (_LastColumn == 0) _LastColumn = worksheet.Cells[FileHeaderRow, worksheet.UsedRange.Columns.Count].Column;
                return _LastColumn;
            }
        }
        private int _LastColumn = 0;

        public object[,] FileArray;
        public int ArrRrows;
        public int ArrColumns;
        public void SetFileData()
        {
            FileArray = worksheet.Range[worksheet.Cells[FileHeaderRow, 1], worksheet.Cells[LastRow, LastColumn]].Value;
            //ArrRrows = FileArray.GetUpperBound(0) + 1;
            ArrRrows = FileArray.GetUpperBound(0);
            ArrColumns = FileArray.Length / ArrRrows;
        }

        public FileBase() { }

        /// <summary>
        /// получение номена строки по имени заголовка
        /// </summary>
        /// <param name="fildName"></param>
        /// <returns></returns>
        public int FindColumn(string fildName)
        {
            for (int col = 1; col <= ArrColumns; col++)
            {
                object obj = FileArray[1, col];
                if ((obj is string) && Convert.ToString(obj) == fildName)
                    return col;
            }
            return 0;
        }

        /// <summary>
        /// Поиск значения String в столбце
        /// </summary>
        /// <param name="str"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public int FindRow(string str, int col)
        {
            for (int rowIndex = 2; rowIndex < ArrRrows; rowIndex++)
            {
                object obj = FileArray[rowIndex, col];
                if (obj is string && Convert.ToString(obj) == str) return rowIndex;
            }
            return 0;
        }

        /// <summary>
        /// Поиск значения Double в столбце
        /// </summary>
        /// <param name="dbl"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public int FindRow(double dbl, int col)
        {
            for (int rowIndex = 2; rowIndex < ArrRrows; rowIndex++)
            {
                object obj = FileArray[rowIndex, col];
                if (obj is double && Convert.ToDouble(obj) == dbl) return rowIndex;
            }
            return 0;
        }

        /// <summary>
        /// Поиск значения DataTime в столбце
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public int FindRow(DateTime dt, int col)
        {
            for (int rowIndex = 2; rowIndex < ArrRrows; rowIndex++)
            {
                object obj = FileArray[rowIndex, col];
                if (obj is DateTime && Convert.ToDateTime(obj) == dt) return rowIndex;
            }
            return 0;
        }
        /// <summary>
        /// Получение даты из ячейки
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public DateTime GetDateFromCell(int rw, int col)
        {
            object obj = GetValueFromColumn(rw, col);

            if (obj == null)
                return new DateTime();

            if (obj is double)
                return DateTime.FromOADate(Convert.ToDouble(obj));
            else if (obj is DateTime)
                return Convert.ToDateTime(obj);
            else
                return new DateTime();
        }

        /// <summary>
        /// получение значения из строки по номеру столбца
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public object GetValueFromColumn(int rw, int col) {
            return col != 0 ? FileArray[rw, col] : null;
        }

        public string GetValueFromColumnStr(int rw, int col)
        {
            object obj = FileArray[rw, col]; 
            return obj is string ? Convert.ToString(obj) : "";
        }
        public double GetValueFromColumnDbl(int rw, int col)
        {
            object obj = FileArray[rw, col];
            if (obj is double) return Convert.ToDouble(obj);

            string objStr = GetValueFromColumnStr(rw, col).Trim();
            return Double.TryParse(objStr, out double dbl) ? dbl : 0;
        }
        public DateTime GetValueFromColumnDT(int rw, int col)
        {
            object obj = FileArray[rw, col];
            return obj is DateTime ? Convert.ToDateTime(obj) : new DateTime();
        }

        /// <summary>
        /// Возвращает значение DataTime в формате Double
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public double GetDoubleDateFromCell(int rw, int col)
        {
            object obj = FileArray[rw, col];

            if (obj is DateTime)
            {
                DateTime tmpDateTime = Convert.ToDateTime(obj);
                if (tmpDateTime.ToOADate() > 0)
                    return tmpDateTime.ToOADate();
            }
            return 0;
        }
        public void Close()
        {
            IsOpen = false;
            Workbook.Close(false);
        }

        public void Cancel()
        {
            IsCancel = true;
        }

    }
}