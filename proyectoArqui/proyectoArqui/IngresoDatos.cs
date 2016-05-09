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

        //Botón que inicia la accion de ejecutar la simulación
        private void button2_Click(object sender, EventArgs e)
        {
            //Hilo principal del sistema

            Procesador procesador1 = new Procesador();
            Procesador procesador2 = new Procesador();
            Procesador procesador3 = new Procesador();

       //     Thread hiloProcesador1 = new Thread(procesador1.ejecucionInstrucciones);
            Thread hiloProcesador2;
            Thread hiloProcesador3;


     /*       hiloProcesador1.Start();
            hiloProcesador2.Start();
            hiloProcesador3.Start(); */


            //Se debe meter en cada memoria del procesador lo que contienen los archivos
            //También se debe ir llenando el contexto de cada procesador, en la seccion de
            //registros se ponen ceros, en la sección del PC se pone la ubicacion en memoria
            //donde se almacenó este número.





        }

    }
}
