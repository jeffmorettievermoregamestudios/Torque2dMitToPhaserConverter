using System;
using System.Threading;
using System.Windows.Forms;

namespace Torque2dMitToPhaserConverter
{
    public partial class FormTorque2dToPhaserConverter : Form
    {
        // NOTE TO DEVELOPERS - Can set this to false to disable the Please Wait dialog popup
        private static bool enablePleaseWaitDialog = true;
        private FormPleaseWait frmPleaseWait = new FormPleaseWait();

        public FormTorque2dToPhaserConverter()
        {
            InitializeComponent();
        }

        private void buttonTorque2dProjectBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialogTorque2dProjectModulesFolder.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxTorque2dProjectModulesFolder.Text = folderBrowserDialogTorque2dProjectModulesFolder.SelectedPath;
            }
        }

        private void buttonPhaserProjectOutputFolderBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialogPhaserProjectOutputFolder.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxPhaserProjectOutputFolder.Text = folderBrowserDialogPhaserProjectOutputFolder.SelectedPath;
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            GlobalVars.Torque2dProjectModulesFolder = textBoxTorque2dProjectModulesFolder.Text;
            GlobalVars.PhaserProjectOutputFolder = textBoxPhaserProjectOutputFolder.Text;
            GlobalVars.Torque2dProjectAppCoreVersion = textBoxAppCoreVersion.Text;
            GlobalVars.Torque2dProjectModuleVersion = textBoxModuleVersion.Text;
            GlobalVars.Torque2dCameraSizeWidth = System.Convert.ToSingle(textBoxCameraSizeWidth.Text);
            GlobalVars.Torque2dCameraSizeHeight = System.Convert.ToSingle(textBoxCameraSizeHeight.Text);

            GlobalVars.IsProcessingConversion = true;

            if (enablePleaseWaitDialog)
            {
                frmPleaseWait.ShowAndConvertProject();
            }
            else
            {
                new Thread(new ThreadStart(Torque2dToPhaserConverterFunctionLibrary.ConvertProject)).Start();
            }
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
