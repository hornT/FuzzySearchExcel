using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using FuzzySearch;

namespace FuzzySearchExcel
{
    public partial class MainForm : Form
    {
        private const double DEFAULT_FUZZYNESS = 0.7;
        private const double DEFAULT_AUTO_FUZZYNESS = 0.9;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly FuzzyProcessor _fuzzyProcessor = new FuzzyProcessor();
        private readonly double _fuzzyness;
        private readonly double _autoFuzzyness;
        private int _replaceCount;
        private List<string[]> _possibleReplaces;

        public MainForm()
        {
            InitializeComponent();

            var appSettings = ConfigurationManager.AppSettings;

            string result = appSettings["fuzzyness"] ?? string.Empty;
            if (double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out _fuzzyness) == false)
                _fuzzyness = DEFAULT_FUZZYNESS;

            result = appSettings["autoFuzzyness"] ?? string.Empty;
            if (double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out _autoFuzzyness) == false)
                _autoFuzzyness = DEFAULT_AUTO_FUZZYNESS;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logger.Trace("Открытие файла");

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel(*.xlsx;*.xls;)|*.xlsx;*.xls;";
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    _logger.Debug("Отмена открытия файла");
                    return;
                }
                _logger.Debug($"Открыт файл {dialog.FileName}");

                string[] columns = _fuzzyProcessor.ReadExcelFile(dialog.FileName);
                cbColumn.Items.Clear();
                cbColumn.Items.AddRange(columns);
                cbColumn.SelectedIndex = 70; // TODO 1

                Reset();
            }
        }

        /// <summary>
        /// Нажатие кнопки выполнения автокоррекции
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcess_Click(object sender, EventArgs e)
        {
            if(cbColumn.SelectedIndex < 0)
            {
                MessageBox.Show("Не выбрана колонка", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Сбросить логи и т.д.
            Reset();

            PrepareResult prepareResult = _fuzzyProcessor.PrepareAutoCorrection(cbColumn.SelectedIndex + 1, _fuzzyness, _autoFuzzyness);
            if (prepareResult == null)
                return;

            AddReplaceLog(prepareResult.ReplacementLog);
            AddAutoCorrectionLog(prepareResult.AutoCorrectionResult);
            AddReplaceVariants(prepareResult.PossibleReplaces);
        }

        /// <summary>
        /// Сбросить логи и тд
        /// </summary>
        private void Reset()
        {
            _replaceCount = 0;
            lbReplaceCount.Text = "0";
            lbReplace.Items.Clear();
            btnNext.Enabled = btnRemove.Enabled = btnAdd.Enabled = false;
            rtbLog.Text = string.Empty;
        }

        /// <summary>
        /// Добавить лог автозамены
        /// </summary>
        /// <param name="replacedValues"></param>
        private void AddReplaceLog(IEnumerable<string> replacedValues)
        {
            if (replacedValues == null)
                return;

            rtbLog.AppendText("Произведена автозамена:\n");
            foreach (string replacedValue in replacedValues)
            {
                rtbLog.AppendText($"{replacedValue}\n");
            }
        }

        /// <summary>
        /// Добавить лог поиска автозамен
        /// </summary>
        /// <param name="replacedValues"></param>
        private void AddAutoCorrectionLog(Dictionary<string, string> replacedValues)
        {
            if (replacedValues == null)
                return;

            rtbLog.AppendText("Автопоиск:\n");
            foreach (var val in replacedValues)
            {
                rtbLog.AppendText($"{val.Key} заменены на {val.Value}\n");
            }
        }

        /// <summary>
        /// Показать пользователю варианты для замен
        /// </summary>
        /// <param name="possibleReplaces"></param>
        private void AddReplaceVariants(List<string[]> possibleReplaces)
        {
            _possibleReplaces = possibleReplaces;
            if (_possibleReplaces == null || _possibleReplaces.Count == 0)
                return;

            btnNext.Enabled = btnRemove.Enabled = btnAdd.Enabled = true;
            _replaceCount = _possibleReplaces.Count;
            lbReplaceCount.Text = _replaceCount.ToString();
            lbReplace.Items.AddRange(_possibleReplaces.LastOrDefault());
        }

        /// <summary>
        /// Клик по кнопке "Пропустить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNext_Click(object sender, EventArgs e)
        {
            _replaceCount--;
            lbReplace.Items.Clear();
            if (_replaceCount < 1)
            {
                btnNext.Enabled = btnRemove.Enabled = btnAdd.Enabled = false;
            }

            lbReplaceCount.Text = _replaceCount.ToString();
            lbReplace.Items.AddRange(_possibleReplaces[_replaceCount - 1]);
        }

        /// <summary>
        /// Клик по кнопке "Исключить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lbReplace.SelectedItem == null)
                return;

            lbReplace.Items.Remove(lbReplace.SelectedItem);
        }

        /// <summary>
        /// Клик по кнопке "Добавить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (lbReplace.SelectedItem == null)
                return;

            string keyWord = lbReplace.SelectedItem as string;
            var replaceWords = new HashSet<string>(lbReplace.Items.OfType<string>());
            replaceWords.Remove(keyWord);

            _fuzzyProcessor.Add(keyWord, replaceWords);

            btnNext_Click(sender, e);
        }

        /// <summary>
        /// Сохранть результат
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            _fuzzyProcessor.Save();
        }

        /// <summary>
        /// Событие перед закрытием формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _fuzzyProcessor.OnClose();
        }

        
    }
}
