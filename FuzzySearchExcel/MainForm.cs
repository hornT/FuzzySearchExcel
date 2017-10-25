using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FuzzySearchExcel
{
    public partial class MainForm : Form
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly FuzzyProcessor fuzzyProcessor = new FuzzyProcessor();

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Trace("Открытие файла");

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel(*.xlsx;*.xls;)|*.xlsx;*.xls;";
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    logger.Debug("Отмена открытия файла");
                    return;
                }
                logger.Debug($"Открыт файл {dialog.FileName}");

                string[] columns = fuzzyProcessor.ReadExcelFile(dialog.FileName);
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

            fuzzyProcessor.ProcessAutoCorrection(cbColumn.SelectedIndex + 1);
        }
    }
}
