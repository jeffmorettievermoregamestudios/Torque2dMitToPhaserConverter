using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Torque2dMitToPhaserConverter
{
    public partial class FormPleaseWait : Form
    {
        public FormPleaseWait()
        {
            InitializeComponent();
        }

        public void ShowAndConvertProject()
        {
            this.Show();
            new Thread(new ThreadStart(Torque2dToPhaserConverterFunctionLibrary.ConvertProject)).Start();
            new Thread(new ThreadStart(WaitForConvertProjectToComplete)).Start();
        }

        private void WaitForConvertProjectToComplete()
        {
            while (GlobalVars.IsProcessingConversion)
            {
                Thread.Sleep(250);
            }

            this.Invoke((MethodInvoker)delegate
            {
                this.Hide();
            });
        }
    }
}
