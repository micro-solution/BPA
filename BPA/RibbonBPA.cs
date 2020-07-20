﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

using BPA.Forms;
using BPA.Model;
using BPA.Modules;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Tools.Ribbon;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Diagnostics;
using System.Text;
using Microsoft.Office.Core;
using SettingsBPA = BPA.Properties.Settings;

namespace BPA
{
    public partial class RibbonBPA
    {

        private void RibbonBPA_Load(object sender, RibbonUIEventArgs e)
        {

        }

        /// <summary>
        /// кнопка загрузки
        /// </summary>
        private void AddNewCalendar_Click(object sender, RibbonControlEventArgs e)
        {
            FileCalendar fileCalendar = null;

            try
            {
                fileCalendar = new FileCalendar();
                if (!fileCalendar.IsOpen) return;

                new Product().ReadColNumbers();
                new ProductCalendar().ReadColNumbers();

                FunctionsForExcel.SpeedOn();
                Globals.ThisWorkbook.Activate();

                fileCalendar.LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                if (fileCalendar?.IsOpen ?? false) fileCalendar.Close();
            }
        }

        /// <summary>
        /// кнопка обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateProducts_Click(object sender, RibbonControlEventArgs e)
        {
            new Product().ReadColNumbers();
            new ProductCalendar().ReadColNumbers();
            ProcessBar processBar = null;
            List<ProductCalendar> calendars = new ProductCalendar().GetProductCalendars();
            processBar = new ProcessBar("Обновление продуктовых календарей", calendars.Count);
            try
            {
                FunctionsForExcel.SpeedOn();
                
                Product product = new Product();
                processBar.Show();
                Globals.ThisWorkbook.Activate();
                foreach (ProductCalendar calendar in calendars)
                {
                    if (processBar.IsCancel) break;
                    processBar.TaskStart($"Обрабатывается календарь {calendar.Name}");
                    processBar.AddSubBar("Обновление данных", product.Table.ListRows.Count);
                    calendar.ActionStart += processBar.SubBar.TaskStart;
                    calendar.ActionDone += processBar.SubBar.TaskDone;
                    processBar.SubBar.CancelClick += calendar.Cancel;
                    try
                    {
                        calendar.UpdateProducts();
                    }
                    catch(FileNotFoundException)
                    {

                    }
                    processBar.SubBar.TaskDone();
                    processBar.TaskDone(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                if (processBar != null)
                {
                    processBar.SubBar?.Close();
                    processBar?.Close();
                }
            }
            
        }

        /// <summary>
        /// Кнопка информация
        /// </summary>
        private void About_Click(object sender, RibbonControlEventArgs e)
        {
            MessageBox.Show("Функционал в разработке", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Кнопка настроек
        /// </summary>
        private void Settings_Click(object sender, RibbonControlEventArgs e)
        {
            SettingsForm form = new SettingsForm();
            form.ShowDialog(new ExcelWindows(Globals.ThisWorkbook));
            //try
            //{
            //    FunctionsForExcel.SpeedOn();

            //    //FunctionsForExcel.HideShowSettingsSheets();
            //    WorksheetsSettings WS = new WorksheetsSettings();
            //    WS.ShowUnshowSheets();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
            //finally
            //{
            //    FunctionsForExcel.SpeedOff();
            //}
        }

        /// <summary>
        /// Обновление продукта
        /// </summary>
        private void UpdateProduct_Click(object sender, RibbonControlEventArgs e)
        {
            FileCalendar fileCalendar = null;
            try
            {
                FunctionsForExcel.SpeedOn();
                new Product().ReadColNumbers();
                new ProductCalendar().ReadColNumbers();

                Product product = new Product().GetPoductActive();
                ProductCalendar calendar = new ProductCalendar(product.Calendar);
                fileCalendar = new FileCalendar(calendar.Path);
                if (fileCalendar != null)
                {
                    product.SetFromCalendar(fileCalendar.Workbook);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (fileCalendar?.IsOpen ?? false) fileCalendar.Close();
                FunctionsForExcel.SpeedOff();
            }
        }

        private void UploadPrice_Click(object sender, RibbonControlEventArgs e)
        {
            new ProductForRRC().ReadColNumbers();
            new RRC().ReadColNumbers();
            ProcessBar processBar = null;
            List<ProductForRRC> products = new ProductForRRC().GetProducts();
            processBar = new ProcessBar("Обновление цен из справочника", products.Count);
            bool isCancel = false;
            void CancelLocal() => isCancel = true;

            try
            {
                FunctionsForExcel.SpeedOn();

                processBar.CancelClick += CancelLocal;
                processBar.Show();
                Globals.ThisWorkbook.Activate();

                DateTime date = products[0].DateOfPromotion;

                List<RRC> rrcs = new RRC().GetSortedRRCList();

                foreach (ProductForRRC product in products)
                {
                    if (isCancel)
                        break;

                    processBar.TaskStart($"Обрабатывается артикул {product.Article}");
                    List<RRC> RRCArt = rrcs.FindAll(x => x.Article == product.Article && x.Date <= date).ToList();

                    if (RRCArt.Count > 0)
                    {
                        product.UpdatePriceFromRRC(RRCArt[RRCArt .Count- 1]);
                    }

                    processBar.TaskDone(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                processBar?.Close();
            }
        }

        private void SavePrice_Click(object sender, RibbonControlEventArgs e)
        {
            ProcessBar processBar = null;
            bool isCancel = false;
            void CancelLocal() => isCancel = true;

            try
            {
                FunctionsForExcel.SpeedOn();

                new ProductForRRC().ReadColNumbers();
                new RRC().ReadColNumbers();
                List<ProductForRRC> products = new ProductForRRC().GetProducts();
                processBar = new ProcessBar("Обновление цен из справочника", products.Count);

                processBar.CancelClick += CancelLocal;
                processBar.Show();
                Globals.ThisWorkbook.Activate();

                DateTime date = products[0].DateOfPromotion;

                List<RRC> rrcs = new RRC().GetSortedRRCList();

                foreach (ProductForRRC product in products)
                {
                    if (isCancel)
                        break;

                    processBar.TaskStart($"Обрабатывается артикул {product.Article}");

                    if (date.Year > 1)
                    {
                        RRC rrc = rrcs.Find(x => x.Article == product.Article && x.Date == date);

                        if (rrc == null)
                        {
                            rrc = new RRC();
                            rrc.SetProduct(product);
                            rrc.Save();
                        } else
                        {
                            rrc.SetProduct(product);
                            rrc.Update();
                        }
                    }
                    processBar.TaskDone(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                processBar?.Close();
            }
        }

        private void ClientsUpdate_Click(object sender, RibbonControlEventArgs e)
        {
            FunctionsForExcel.SpeedOn();
            ProcessBar processBar = null;
            FileDescision fileDescision = null;

            try
            {
                new Client().ReadColNumbers();
                fileDescision = new FileDescision();
                if (!fileDescision.IsOpen) return;

                processBar = new ProcessBar("Обновление клиентов", fileDescision.CountActions);
                processBar.CancelClick += fileDescision.Cancel;
                fileDescision.ActionStart += processBar.TaskStart;
                fileDescision.ActionDone += processBar.TaskDone;
                processBar.Show(new ExcelWindows(Globals.ThisWorkbook));

                List<Client> clientsFromDecision = fileDescision.LoadClients();

                processBar.Close();

                //Загрузить данные из листа клиентов
                List<Client> clients = Client.GetAllClients();
                if (clients == null) return;

                //Получить разницу
                List<Client> newClients = clientsFromDecision.Except(clients, new Client.ComparerCustomer()).ToList();
                if(newClients.Count == 0)
                {
                    MessageBox.Show("В файле Decision не обнаружено новых клиентов", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //Выгрузить разницу как новых клиентов
                //newClients.ForEach(x => x.Save());
                bool isCancel = false;

                void Cancel() => isCancel = true;

                processBar = new ProcessBar("Обновление клиентов", fileDescision.CountActions);
                processBar.CancelClick += Cancel;
                processBar.Show(new ExcelWindows(Globals.ThisWorkbook));

                foreach (Client client in newClients)
                {
                    if (isCancel) return;
                    processBar.TaskStart($"Сохраняется клиент {client.Customer}");
                    client.Save();
                    processBar.TaskDone(1);
                }

                Client ClientForSort = newClients.First();
                ClientForSort.Sort("Id");
                Excel.Worksheet ws = Globals.ThisWorkbook.Sheets[ClientForSort.SheetName];
                ws.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if(fileDescision?.IsOpen ?? false) fileDescision.Close();
                processBar?.Close();
                FunctionsForExcel.SpeedOff();
            }
        }

        /// <summary>
        /// прайс-лист клиента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetClientPrice_Click(object sender, RibbonControlEventArgs e)
        {
            CreatePrice(false);
        }

        private void GetAllPrices_Click(object sender, RibbonControlEventArgs e)
        {
            CreatePrice(true);
        }

        private void CreatePrice(bool All = false)
        {
            ProcessBar processBar = null;
            FilePriceMT filePriceMT = null;

            bool isCancel = false;
            void Cancel() => isCancel = true;
            List<Client> priceClients = new List<Client>();

            try
            {
                new FinalPriceList().ReadColNumbers();
                new ExclusiveMag().ReadColNumbers();
                new Client().ReadColNumbers();
                new RRC().ReadColNumbers();
                new Product().ReadColNumbers();
                new Discount().ReadColNumbers();

                FunctionsForExcel.SpeedOn();

                if (All)
                {
                    //загрузить всех подопытных
                    Client client = new Client();
                    processBar = new ProcessBar($"Загрузка списка клиентов", client.Table.ListRows.Count);
                    processBar.CancelClick += Cancel;

                    foreach(Excel.ListRow row in client.Table.ListRows)
                    {
                        if (isCancel)
                        {
                            processBar.Close();
                            return;
                        }
                        processBar.TaskStart($"Загружаем {row.Index}");
                        priceClients.Add(new Client(row));
                        processBar.TaskDone(1);
                    }

                    processBar?.Close();
                }
                else
                {
                    //выбрать того на кого указал перст божий
                    //получить активного клиента, если нет, то на нет и суда нет
                    Client currentClient = Client.GetCurrentClient();
                    if (currentClient == null)
                    {
                        MessageBox.Show("Выберите клиента на листе \"Клиенты\"", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    priceClients.Add(currentClient);
                }

                //Запросить дату
                MSCalendar calendar = new MSCalendar();
                DateTime currentDate;
                if (calendar.ShowDialog(new ExcelWindows(Globals.ThisWorkbook)) == DialogResult.OK) currentDate = calendar.SelectedDate;
                else return;
                calendar.Close();

                //сюда вынести загрузку общих данных
                List<RRC> actualRRC = RRC.GetActualPriceList(currentDate);
                if (actualRRC == null) return;

                foreach (Client currentClient in priceClients)
                {
                    Discount currentDiscount = Discount.GetCurrentDiscount(currentClient, currentDate);
                    if (currentDiscount == null) return;
                    //if (currentDiscount == null) continue;

                    //подгрузить PriceMT если неужно, подключится к РРЦ                   
                    if (currentDiscount.NeedFilePriceMT() && (!filePriceMT?.IsOpen ?? true))
                    {
                        //Загурзить файл price list MT
                        filePriceMT = new FilePriceMT();
                        processBar = new ProcessBar($"Создание прайс-листа для {currentClient.Customer}", 1);
                        filePriceMT.ActionStart += processBar.TaskStart;
                        filePriceMT.ActionDone += processBar.TaskDone;
                        processBar.CancelClick += filePriceMT.Cancel;
                        filePriceMT.Load(currentClient.Mag, currentDate);
                        if (!filePriceMT.IsOpen) return;
                        if (!All) processBar.Close(); ///else not close???
                    }

                    //Загрузка списка артикулов, какие из них актуальные?
                    List<Product> products = Product.GetProductForClient(currentClient);
                    if (products == null) return;

                    //в цикле менять метки на значения из цен, с заменой;
                    List<FinalPriceList> priceList = new List<FinalPriceList>();

                    processBar = new ProcessBar($"Создание прайс-листа для {currentClient.Customer}", products.Count);
                    processBar.CancelClick += Cancel;
                    foreach (Product product in products)
                    {
                        if (isCancel) return;
                        //получить формулу
                        processBar.TaskStart($"Расчет цены для {product.Article}");
                        if (product.Category == "")
                        {
                            MessageBox.Show($"Для {product.Article} не указана категория", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        string formula = currentDiscount.GetFormulaByName(product.Category);

                        //Найти метку или метки. [Pricelist MT]  [DIY Pricelist] [РРЦ] и заменить
                        while (formula.Contains("[pricelist mt]"))
                            formula = formula.Replace("[pricelist mt]", filePriceMT.GetPrice(product.Article).ToString());

                        while (formula.Contains("[diy price list]"))
                            formula = formula.Replace("[diy price list]", actualRRC.Find(x => x.Article == product.Article).DIY.ToString());

                        while (formula.Contains("[ррц]"))
                            formula = formula.Replace("[ррц]", actualRRC.Find(x => x.Article == product.Article).RRCNDS.ToString());

                        if (Parsing.Calculation(formula) is double result)
                            priceList.Add(new FinalPriceList(product)
                            {
                                RRC = result
                            });
                        else
                        {
                            MessageBox.Show($"В одной из формул для {currentClient.Customer} содержится ошибка", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        processBar.TaskDone(1);
                    }
                    processBar.Close();

                    //Вывести
                    processBar = new ProcessBar($"Создание прайс-листа для {currentClient.Customer}", products.Count);
                    processBar.CancelClick += Cancel;
                    foreach (FinalPriceList item in priceList)
                    {
                        if (isCancel) return;
                        processBar.TaskStart($"Сохранение: {item.ArticleGardena}");
                        item.Save();
                        processBar.TaskDone(1);
                    }
                    processBar.TaskDone(1);
                }

                Excel.Worksheet ws = Globals.ThisWorkbook.Sheets[new FinalPriceList().SheetName];
                ws.Activate();
                MessageBox.Show("Создание прайс-листа завершено", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (filePriceMT?.IsOpen ?? false) filePriceMT.Close();
                processBar?.Close();
                FunctionsForExcel.SpeedOff();
            }
        }

        /// <summary>
        /// Кнопка создать планирование
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanningAdd_Click(object sender, RibbonControlEventArgs e)
        {
            try
            {
                FunctionsForExcel.SpeedOn();
                new PlanningNewYear().ReadColNumbers();

                PlanningNewYear planningNewYear = new PlanningNewYear(SettingsBPA.Default.SHEET_NAME_PLANNING_TEMPLATE);
                planningNewYear.GetSheetCopy();
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
            }
        }

        private void GetPlanningData_Click(object sender, RibbonControlEventArgs e)
        {
            ProcessBar processBar = null;
            FileDescision fileDescision = null;
            FileBuget fileBuget = null;


            Worksheet worksheet = Globals.ThisWorkbook.Application.ActiveSheet;
            if (!FunctionsForExcel.HasRange(worksheet, SettingsBPA.Default.PlannningNYIndicatorCellName) ||
                worksheet.Name == SettingsBPA.Default.SHEET_NAME_PLANNING_TEMPLATE)
            {
                MessageBox.Show("Перейдите на страницу планирования (или создайте её) и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                new Discount().ReadColNumbers();
                new ProductForPlanningNewYear().ReadColNumbers();
                new STK().ReadColNumbers();
                new Client().ReadColNumbers();
                new Product().ReadColNumbers();
                new RRC().ReadColNumbers();

                new PlanningNewYear(worksheet.Name).ReadColNumbers();
                new PlanningNewYearPrognosis(new PlanningNewYear(worksheet.Name)).ReadColNumbers();
                new PlanningNewYearPromo(new PlanningNewYear(worksheet.Name)).ReadColNumbers();

                //получаем заполненые данне
                PlanningNewYear planningNewYearTmp = new PlanningNewYear().GetTmp(worksheet.Name);
                if (planningNewYearTmp == null)
                {
                    MessageBox.Show("Создайте копию листа планирования нового года", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                planningNewYearTmp.ClearTable(worksheet.Name);
                planningNewYearTmp.MaximumBonus = new Discount().GetDiscountForPlanning(planningNewYearTmp);

                //получаем продукты на основании введенных данных
                List<ProductForPlanningNewYear> products = new ProductForPlanningNewYear().GetProducts(planningNewYearTmp);
                
                //получаем Desicion, Buget
                fileDescision = new FileDescision();
                fileBuget = new FileBuget();
                if (!fileBuget.IsOpen || !fileDescision.IsOpen) return;

                //загружаем Desicion, Buget
                fileDescision.LoadForPlanning(planningNewYearTmp);
                fileBuget.LoadForPlanning(planningNewYearTmp);
                if (fileDescision?.IsOpen ?? false) fileDescision.Close();
                if (fileBuget?.IsOpen ?? false) fileBuget.Close();

                //создание клиента для формирования PriceList-цены
                Client client = new Client(planningNewYearTmp);
                PriceListForPlaning priceListForPlaning = new PriceListForPlaning(client, planningNewYearTmp.CurrentDate);
                priceListForPlaning.Load();
                //

                processBar = new ProcessBar("Обновление клиентов", products.Count);
                bool isCancel = false;
                void CancelLocal() => isCancel = true;
                FunctionsForExcel.SpeedOn();
                processBar.CancelClick += CancelLocal;
                processBar.Show();

                // заполняем планнингньюер 
                foreach (ProductForPlanningNewYear product in products)
                {
                    if (isCancel)
                        break;
                    processBar.TaskStart($"Обрабатывается артикул {product.Article}");

                    PlanningNewYear planning = planningNewYearTmp.Clone();
                    planning.SetProduct(product);
                    planning.GetSTK();

                    PlanningNewYearPrognosis prognosis = new PlanningNewYearPrognosis(planning);
                    prognosis.SetValues(fileDescision.ArticleQuantities, fileBuget.ArticleQuantities);
                    prognosis.PriceList = priceListForPlaning.GetPrice(product.Article);

                    PlanningNewYearPromo promo = new PlanningNewYearPromo(planning);
                    promo.SetValues(fileDescision.ArticleQuantities, fileBuget.ArticleQuantities);

                    planning.Save(worksheet.Name);
                    planning.SetMaximumBonusValue();
                    prognosis.Save();
                    promo.Save();

                    processBar.TaskDone(1);
                }
                products.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                processBar?.Close();
                if (fileBuget?.IsOpen ?? false) fileBuget.Close();
                if (fileDescision?.IsOpen ?? false) fileDescision.Close();
            }
            
            //MessageBox.Show("Функционал в разработке", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary> 
        ///Планирование/обновить факт
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactUpdate_Click(object sender, RibbonControlEventArgs e)
        {
            ProcessBar processBar = null;
            FileDescision fileDescision = null;
            FileBuget fileBuget = null;

            Worksheet worksheet = Globals.ThisWorkbook.Application.ActiveSheet;
            if (!FunctionsForExcel.HasRange(worksheet, SettingsBPA.Default.PlannningNYIndicatorCellName) ||
                worksheet.Name == SettingsBPA.Default.SHEET_NAME_PLANNING_TEMPLATE)
            {
                MessageBox.Show("Перейдите на страницу планирования (или создайте её) и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                new Client().ReadColNumbers();
                new Product().ReadColNumbers();
                new RRC().ReadColNumbers();
                new Discount().ReadColNumbers();

                new PlanningNewYear(worksheet.Name).ReadColNumbers();
                new PlanningNewYearPrognosis(new PlanningNewYear(worksheet.Name)).ReadColNumbers();
                new PlanningNewYearPromo(new PlanningNewYear(worksheet.Name)).ReadColNumbers();

                
                PlanningNewYear planningNewYearTmp = new PlanningNewYear().GetTmp(worksheet.Name);
                if (!planningNewYearTmp.HasData())
                {
                    MessageBox.Show($"Заполните { worksheet.Name } и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<PlanningNewYearPrognosis> prognosises = new List<PlanningNewYearPrognosis>();
                List<PlanningNewYearPromo> promos = new List<PlanningNewYearPromo>();
                
                planningNewYearTmp.SetLists(prognosises, promos);

                //получаем Desicion, Buget
                if (prognosises == null || promos == null || (prognosises.Count < 0 || prognosises.Count < 0))
                {
                    MessageBox.Show($"Заполните { worksheet.Name } и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                fileDescision = new FileDescision();
                fileBuget = new FileBuget();
                if (!fileBuget.IsOpen || !fileDescision.IsOpen) return;
                
                fileDescision.LoadForPlanning(planningNewYearTmp);
                fileBuget.LoadForPlanning(planningNewYearTmp);
                if (fileBuget?.IsOpen ?? false) fileBuget.Close();
                if (fileDescision?.IsOpen ?? false) fileDescision.Close();

                //создание клиента для формирования PriceList-цены
                Client client = new Client(planningNewYearTmp);
                PriceListForPlaning priceListForPlaning = new PriceListForPlaning(client, planningNewYearTmp.CurrentDate);
                priceListForPlaning.Load();
                //

                int count = prognosises.Count > promos.Count ? prognosises.Count : promos.Count;
                processBar = new ProcessBar("Обновление клиентов", count);
                bool isCancel = false;
                void CancelLocal() => isCancel = true;
                FunctionsForExcel.SpeedOn();
                processBar.CancelClick += CancelLocal;
                processBar.Show();

                for (int p = 0; p < count; p++)
                {
                    if (isCancel)
                        break;

                    PlanningNewYearPrognosis prognosis = prognosises[p];
                    PlanningNewYearPromo promo = promos[p];

                    string article = prognosis.planningNewYear.Article;
                    processBar.TaskStart($"Обрабатывается артикул {article}");

                    //сопоставить каждый prognosis и promo ArticleQuantities из файлов
                    prognosis.SetValues(fileDescision.ArticleQuantities, fileBuget.ArticleQuantities);
                    prognosis.PriceList = priceListForPlaning.GetPrice(prognosis.planningNewYear.Article);
                    promo.SetValues(fileDescision.ArticleQuantities, fileBuget.ArticleQuantities);

                    prognosis.Save();
                    promo.Save();

                    processBar.TaskDone(1);
                }

                prognosises.Clear();
                promos.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                processBar?.Close();
                if (fileBuget?.IsOpen ?? false) fileBuget.Close();
                if (fileDescision?.IsOpen ?? false) fileDescision.Close();
            }
        }

        private void AddNewIRP_Click(object sender, RibbonControlEventArgs e)
        {
            //MessageBox.Show("Функционал в разработке", "BPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            BPASettings settings = new BPASettings();
            if(settings.GetProductCalendarPath(out string path, true))
            {
                MessageBox.Show(path);
            }
            else
            {
                MessageBox.Show("Kernel Panic");
            }

        }

        /// <summary>
        /// Планирование / сохранить планирование
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlanningSave_Click(object sender, RibbonControlEventArgs e)
        {
            ProcessBar processBar = null;

            Worksheet worksheet = Globals.ThisWorkbook.Application.ActiveSheet;

            new PlanningNewYear(worksheet.Name).ReadColNumbers();
            new PlanningNewYearSave(new PlanningNewYear(worksheet.Name)).ReadColNumbers();
            new Plan().ReadColNumbers();

            if (!FunctionsForExcel.HasRange(worksheet, SettingsBPA.Default.PlannningNYIndicatorCellName) ||
                worksheet.Name == SettingsBPA.Default.SHEET_NAME_PLANNING_TEMPLATE)
            {
                MessageBox.Show("Перейдите на страницу планирования (или создайте её) и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                PlanningNewYear planningNewYearTmp = new PlanningNewYear().GetTmp(worksheet.Name);

                if (!planningNewYearTmp.HasData())
                {
                    MessageBox.Show($"Заполните { worksheet.Name } и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List <PlanningNewYearSave> saves = new List<PlanningNewYearSave>();
                planningNewYearTmp.SetLists(saves);

                if (saves == null || saves.Count < 1)
                {
                    MessageBox.Show($"Заполните { worksheet.Name } и повторите попытку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                processBar = new ProcessBar("Обновление клиентов", saves.Count);
                bool isCancel = false;
                void CancelLocal() => isCancel = true;
                FunctionsForExcel.SpeedOn();
                processBar.CancelClick += CancelLocal;
                processBar.Show();

                foreach (PlanningNewYearSave planningNewYearSave in saves)
                {
                    if (isCancel)
                        break;

                    processBar.TaskStart($"Обрабатывается артикул { planningNewYearSave.planningNewYear.Article}");
                    Plan planning = new Plan().GetPlan(planningNewYearSave);
                    planning.Save();

                    processBar.TaskDone(1);
                    Globals.ThisWorkbook.Sheets[planning.SheetName].Activate();
                }

                saves.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FunctionsForExcel.SpeedOff();
                processBar?.Close();
            }
        }
    }
}
