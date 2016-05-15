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

        /*Botón para cargar la ruta de hilos*/
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
            /*Si se proporcionaron los datos correctos y la ruta de archivos continua la simulación*/
            if (rutaHilos != null && !txtHilos.Text.ToString().Equals("") && !txtQuantum.Text.ToString().Equals(""))
            {
                if (Convert.ToInt32(txtQuantum.Text) == 0)
                {
                    MessageBox.Show("El valor del quantum debe ser mayor a 0", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                else
                {
                    Resultados resultados = new Resultados(rutaHilos, Convert.ToInt32(txtHilos.Text), Convert.ToInt32(txtQuantum.Text));
                    resultados.Show();
                    this.Hide();
                }
                
            }

            /*Si no se seleccionó la ruta de la carpeta con los hilos muestra un error*/
            else if (rutaHilos == null)
            {
                MessageBox.Show("Para iniciar la simulación debe elegir una ruta de archivos", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            /*Si no se digitaron todos los datos para la simulación muestra un error*/
            else 
            {
                MessageBox.Show("Debe ingresar todos los datos necesarios para simulación", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /*Método para validar que solo se introduzcan números en la cantidad de hilos*/
        private void txtHilos_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        /*Método para validar que solo se introduzcan números en el valor del quantum*/
        private void txtQuantum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

    }
}
