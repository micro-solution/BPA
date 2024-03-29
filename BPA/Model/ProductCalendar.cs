﻿using BPA.Forms;
using BPA.Modules;

using Microsoft.Office.Interop.Excel;

using System.Collections.Generic;

namespace BPA.Model
{
    /// <summary>
    /// Справочник продуктовых календарей
    /// </summary>
    internal class ProductCalendar : TableBase
    {
        public override string TableName => "Продуктовые_календари";
        public override string SheetName => "Продуктовые календари";

        public static Dictionary<string, int> ColDict { get; set; } = new Dictionary<string, int>();

        public override IDictionary<string, string> Filds
        {
            get
            {
                return _filds;
            }
        }
        private readonly Dictionary<string, string> _filds = new Dictionary<string, string>
        {
            { "Id", "№" },
            { "Name", "Название" },
            { "Path", "Путь к файлу" },
        };

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id
        {
            get; set;
        }
        /// <summary>
        /// Название
        /// </summary>
        public string Name
        {
            get; set;
        }
        /// <summary>
        /// Путь к файлу
        /// </summary>
        public string Path
        {
            get; set;
        }

        private bool IsCancel = false;
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

        public ProductCalendar() { }

        public ProductCalendar(string name)
        {
            Name = name;
            var listRow = GetRow("Name", name);
            if (listRow != null) SetProperty(listRow);
        }

        /// <summary>
        /// Получение списка продуктовых календарей
        /// </summary>
        /// <returns></returns>
        public List<ProductCalendar> GetProductCalendars()
        {
            List<ProductCalendar> productCalendars = new List<ProductCalendar>();
            foreach (ListRow row in Table.ListRows)
            {
                ProductCalendar productCalendar = new ProductCalendar();
                productCalendar.SetProperty(row);
                productCalendars.Add(productCalendar);
            }
            return productCalendars;
        }

        public void Cancel()
        {
            IsCancel = true;
        }

        /// <summary>
        /// Обновление продуктов из календаря
        /// </summary>
        public void UpdateProducts(List<Product> products, ProcessBar pB)
        {
            FileCalendar fileCalendar = new FileCalendar(Path);
            if (fileCalendar == null)
                return;
            
            pB.AddSubBar("Обновление данных", products.Count); ///Вот этих ПБ созается очень много!
            this.ActionStart += pB.SubBar.TaskStart;
            this.ActionDone += pB.SubBar.TaskDone;
            pB.SubBar.CancelClick += this.Cancel;

            foreach (Product product in products)
            {
                if (IsCancel) return;
                ActionStart?.Invoke($"Обрабатывается № {product.Id}");
                if (product.Calendar == Name)
                {
                    product.SetFromCalendar(fileCalendar.Workbook);
                }
                ActionDone?.Invoke(1);
            }
            fileCalendar.Close();
        }
    }
}
