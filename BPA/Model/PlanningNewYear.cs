﻿using Microsoft.Office.Interop.Excel;
using System.Collections.Generic;
using BPA.Modules;
using System;
using BPA.Forms;

namespace BPA.Model
{
    /// <summary>
    /// Планирование нового года шаблон
    /// </summary>
    class PlanningNewYear : TableBase
    {
        //public override string TableName => "Планирование_новый_год";
        //public override string SheetName => "Планирование нового года шаблон";
        public override string TableName => GetTableName();
        public override string SheetName => TableWorksheetName;

        private string TableWorksheetName
        {
            get
            {
                if (_TableWorksheetName != "")
                {
                    return _TableWorksheetName;
                }
                else
                {
                    return templateSheetName;
                }
            }
            set
            {
                _TableWorksheetName = value;
            }
        }
        string _TableWorksheetName;

        private string GetTableName()
        {
            try
            {
                ThisWorkbook workbook = Globals.ThisWorkbook;
                ListObject table = workbook.Sheets[SheetName].ListObjects[1];
                return table.Name;
            }
            catch
            {
                ThisWorkbook workbook = Globals.ThisWorkbook;
                ListObject table = workbook.Sheets[templateSheetName].ListObjects[1];
                return table.Name;
            }
        }

        private string templateSheetName = "Планирование нового года шаблон";
        private string CustomerStatusLabel = "Customer status";
        private string ChannelTypeLabel = "Channel type";
        private string YearLabel = "Период";

        #region --- Словарь ---

        public override IDictionary<string, string> Filds
        {
            get
            {
                return _filds;
            }
        }
        private readonly Dictionary<string, string> _filds = new Dictionary<string, string>
        {
            { "Id","№" },
            { "Article","Артикул"},
            { "RRCNDS","РРЦ, руб.с НДС"},
            { "PercentageOfChange","Процент изменения"},
            { "STKEur","STK 2.5,  Eur"},
            { "STKRub","STK 2.5, руб."},
            { "IRP","IRP, Eur"},
            { "RRCNDS2","РРЦ, руб.с НДС2"},
            { "IRPIndex","Индекс IRP"},
            { "DIYPriceList","DIY price list, руб. без НДС"}
        };

        #endregion

        #region --- Свойства ---
        /// <summary>
        /// №
        /// </summary>
        public int Id
        {
            get; set;
        }

        /// <summary>
        /// Артикул
        /// </summary>
        public string Article
        {
            get; set;
        }

        /// <summary>
        /// РРЦ, руб. с НДС
        /// </summary>
        public double RRCNDS
        {
            get; set;
        }

        /// <summary>
        /// Процент изменения
        /// </summary>
        public double PercentageOfChange
        {
            get; set;
        }

        /// <summary>
        /// STK 2.5, руб.
        /// </summary>
        public double STKRub
        {
            get; set;
        }
        
        /// <summary>
        /// STK 2.5, Eur
        /// </summary>
        public string STKEur
        {
            get; set;
        }
        /// <summary>
        /// IRP, Eur
        /// </summary>
        public double IRP
        {
            get; set;
        }

        /// <summary>
        /// РРЦ, руб. с НДС
        /// </summary>
        public double RRCNDS2
        {
            get; set;
        }

        /// <summary>
        /// Индекс IRP
        /// </summary>
        public double IRPIndex
        {
            get; set;
        }

        /// <summary>
        /// DIY price list, руб. без НДС
        /// </summary>
        public double DIYPriceList
        {
            get; set;
        }
        /// <summary>
        /// Дата принятия
        /// </summary>

        #endregion
        
        public string ChanelType;
        public string CustomerStatus;
        public string Year;

        public PlanningNewYear() { }
        public PlanningNewYear(ListRow row) => SetProperty(row);
        public PlanningNewYear(ProductForPlanningNewYear product)
        {
            this.Article = product.Article;
            this.RRCNDS = product.RRCFinal; //?
            this.PercentageOfChange = product.RRCPercent;  //?
            //            this.STKEur = product.st
            //            this.STKRub = 
            this.IRP = product.IRP;
            this.RRCNDS2 = product.RRCFinal; //?
            this.IRPIndex = product.IRPIndex;
            this.DIYPriceList = product.DIY;
        }

        public void GetSheetCopy()
        {
            ThisWorkbook workbook = Globals.ThisWorkbook;
            FunctionsForExcel.ShowSheet(templateSheetName);

            string newSheetName = templateSheetName.Replace("шаблон", "").Trim();
            Worksheet newSheet = FunctionsForExcel.CreateSheetCopy(workbook.Sheets[templateSheetName], newSheetName);
            newSheet.Activate();

            FunctionsForExcel.HideSheet(templateSheetName);
        }

        public PlanningNewYear GetTmp(string worksheetName)
        {
            if (worksheetName == templateSheetName)
                return null;
            else
                TableWorksheetName = worksheetName;

            try
            {
                PlanningNewYear planningNewYear = new PlanningNewYear();
                ThisWorkbook workbook = Globals.ThisWorkbook;
                Range rng = workbook.Sheets[SheetName].UsedRange;

                planningNewYear.CustomerStatus = val(CustomerStatusLabel);
                planningNewYear.ChanelType = val(ChannelTypeLabel);
                planningNewYear.Year = val(YearLabel);

                string val(string label)
                {
                    try
                    {
                        Range cell =rng.Find(label, LookAt: XlLookAt.xlWhole);
                        return cell.Offset[0, 1].Text;
                    }
                    catch
                    {
                        return "";
                    }
                }

                return planningNewYear;
            } catch
            {
                return null;
            }
        }

        public void Save(string worksheetName)
        {
            TableWorksheetName = worksheetName;
            Save();
        }
    }
}
