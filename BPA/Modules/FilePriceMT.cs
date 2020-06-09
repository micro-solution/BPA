﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPA.Modules
{
    class FilePriceMT
    {
        private readonly string FileName;
        private readonly Microsoft.Office.Interop.Excel.Application Application = Globals.ThisWorkbook.Application;
        private readonly int CalendarHeaderRow = 1;

        /// <summary>
        /// Событие начала задачи
        /// </summary>
        public event ActionsStart ActionStart;
        public delegate void ActionsStart(string name);

        /// <summary>
        /// Событие завершения задачи
        /// </summary>
        public event ActionsDone ActionDone;
        public delegate void ActionsDone(int count);

        public int CountActions => LastRow - CalendarHeaderRow;
        private bool IsCancel = false;

        public Workbook Workbook
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
        private Workbook _Workbook;

        private Worksheet Worksheet => Workbook?.Sheets[1];

        public int LastRow
        {
            get
            {
                if (_LastRow == 0)
                    _LastRow = Worksheet.UsedRange.Row + Worksheet.UsedRange.Rows.Count - 1;
                return _LastRow;
            }
        }
        private int _LastRow = 0;

        #region --- Columns ---

        public int CustomerColumn => FindColumn("Customer");
        public int CustomerDeliveryColumn => FindColumn("CustomerDelivery");
        public int ShopNameColumn => FindColumn("ShopName");
        public int CodeColumn => FindColumn("Code");
        public int CityColumn => FindColumn("City");
        public int RegionColumn => FindColumn("Region");
        public int SalesmanColumn => FindColumn("Salesman");
        public int ClusterColumn => FindColumn("Cluster");
        public int DateColumn => FindColumn("Date");
        public int YearColumn => FindColumn("Year");
        public int GroupColumn => FindColumn("Group");
        public int CategoryColumn => FindColumn("Category");
        public int SuperCategoryColumn => FindColumn("SuperCategory");
        public int Group2Column => FindColumn("Group2");
        public int AltgroupColumn => FindColumn("Altgroup");
        public int TypeColumn => FindColumn("Type");
        public int ModelColumn => FindColumn("Model");
        public int InvoiceColumn => FindColumn("Invoice");
        public int BrandColumn => FindColumn("Brand");
        public int ProductGroupColumn => FindColumn("ProductGroup");
        public int DiscountColumn => FindColumn("Discount");
        public int CompanyColumn => FindColumn("Company");
        public int QuantityColumn => FindColumn("Quantity");
        public int TotalOrigColumn => FindColumn("TotalOrig");
        public int ProfitOrigColumn => FindColumn("ProfitOrig");
        public int WeightNkgColumn => FindColumn("WeightNkg");
        public int WeightGkgColumn => FindColumn("WeightGkg");
        public int VolumeCBMColumn => FindColumn("VolumeCBM");
        public int ChannelColumn => FindColumn("Channel");
        public int OrderLinesColumn => FindColumn("OrderLines");
        public int PricelistPriceTotalColumn => FindColumn("PricelistPriceTotal");
        public int BonusColumn => FindColumn("Bonus");
        public int DivisionColumn => FindColumn("Division");
        public int DoubleColumn => FindColumn("Double");
        public int CampaignColumn => FindColumn("Campaign");
        public int GardenaChannelColumn => FindColumn("GardenaChannel");
        public int ProfitPoolColumn => FindColumn("ProfitPool");
        public int ProfitPoolTypeColumn => FindColumn("ProfitPoolType");
        public int HusqvarnaGradeColumn => FindColumn("HusqvarnaGrade");
        public int GardenaGradeColumn => FindColumn("GardenaGrade");

        #endregion

        public FileFilePriceMT()
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Title = "Выберите расположение продуктового календаря",
                DefaultExt = "*.xls*",
                CheckFileExists = true,
                InitialDirectory = Globals.ThisWorkbook.Path,
                ValidateNames = true,
                Multiselect = false,
                Filter = "Excel|*.xls*"
            })
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    FileName = fileDialog.FileName;
                }
            }

        }

        public FilePriceMT(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Файл {filename} не найден");
            }
            FileName = filename;
        }

        public FilePriceMT(Workbook workbook)
        {
            Workbook = workbook;
        }

        public double Price(string client, string article, double date)
        {
            if (Workbook == null)
                return;

            double price = GetPrice(client, article, date);
//логика 
//где?
            
            Close();
            IsCancel = true;

            return price;
        }

        /// <summary>
        /// возвращает цену по поиску клиента, артикула и даты
        /// </summary>
        /// <param name="client"></param>
        /// <param name="article"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private double GetPrce(string client, string article, double date)
        {
            //где искать клиента ? CustomerColumn??
            int rw = FindRow(CustomerColumn, client);
            int firstfindedrw = rw;
            double price;

            do
                if (rw > 0)
                {
                    if (GetValueFromColumn(rw, CodeColumn) == article)
                    {
                        if (GetValueFromColumn(rw, DateColumn) < date) {
                            price = GetValueFromColumn(rw, PricelistPriceTotalColumn);
                        }
                    }
                    else
                    {
                        rw = FindRow(CustomerColumn, client, Worksheet.Cells[rw, CustomerColumn]);
                    }
                }
            while (rw != firstfindedrw);

            return price;
        }


        /// <summary>
        /// получение номена строки по имени заголовка
        /// </summary>
        /// <param name="fildName"></param>
        /// <returns></returns>
        private int FindColumn(string fildName)
        {
            return Worksheet.Cells.Find(fildName, LookAt: XlLookAt.xlWhole)?.Column ?? 0;
        }


        private int FindRow(int column, string articul)
        {
            return Worksheet.Columns[column].Find(articul, LookAt: XlLookAt.xlWhole)?.Row ?? 0;
        }

        private int FindRow(int column, string articul, Range afterCell)
        {
            return Worksheet.Columns[column].Find(articul, After:afterCell, LookAt: XlLookAt.xlWhole)?.Row ?? 0;
        }

        /// <summary>
        /// получение значения из строки по номеру столбца
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private string GetValueFromColumn(int rw, int col)
        {
            return col != 0 ? Worksheet.Cells[rw, col].value?.ToString() : "";
        }

        public void Close()
        {
            Workbook.Close(false);
        }

        public void Cancel()
        {
            IsCancel = true;
        }

    }
}
