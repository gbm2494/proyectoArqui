using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace proyectoArqui
{
    public partial class IngresoDatos : Form
    {
        string rutaHilos;
        FolderBrowserDialog folder = new FolderBrowserDialog();

        public IngresoDatos()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folder.Description = "Seleccione la carpeta donde se ubican los hilos para la simulación:";

            if(folder.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                rutaHilos = folder.SelectedPath;
            }
        }

        //Botón que inicia la accion de ejecutar la simulación
        private void button2_Click(object sender, EventArgs e)
        {
            if (rutaHilos != null && !txtHilos.Text.ToString().Equals("") && !txtQuantum.Text.ToString().Equals(""))
            {
                Resultados resultados = new Resultados(rutaHilos, Convert.ToInt32(txtHilos.Text), Convert.ToInt32(txtQuantum.Text));
                resultados.Show();
                this.Hide();
            }
            else if (rutaHilos == null)
            {
                MessageBox.Show("Para iniciar la simulación debe elegir una ruta de archivos", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else 
            {
                MessageBox.Show("Debe ingresar todos los datos necesarios para simulación", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private void txtHilos_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtQuantum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

    }
}
