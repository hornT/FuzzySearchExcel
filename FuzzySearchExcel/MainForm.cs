using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace FuzzySearchExcel
{
    public partial class MainForm : Form
    {
        private const double DEFAULT_FUZZYNESS = 0.7;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly FuzzyProcessor _fuzzyProcessor = new FuzzyProcessor();
        private readonly double _fuzzyness;

        public MainForm()
        {
            InitializeComponent();

            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["fuzzyness"] ?? string.Empty;
            if (double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out _fuzzyness) == false)
                _fuzzyness = DEFAULT_FUZZYNESS;
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

            _fuzzyProcessor.ProcessAutoCorrection(cbColumn.SelectedIndex + 1, _fuzzyness);
        }
    }
}
