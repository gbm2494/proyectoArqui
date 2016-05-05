using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace proyectoArqui
{
    public partial class IngresoDatos : Form
    {
        string rutaHilos;

        public IngresoDatos()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new FolderBrowserDialog();

            folder.Description = "Seleccione la carpeta donde se ubican los hilos para la simulación:";

            if(folder.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                rutaHilos = folder.SelectedPath;
            }
        }

    }
}
