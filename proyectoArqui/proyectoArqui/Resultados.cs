using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace proyectoArqui
{
    public partial class Resultados : Form
    {
        string rutaHilos;
        int cantidadHilos;
        int valorQuantum;

        public Resultados(string rutaArchivo, int numHilos, int quantum)
        {
            InitializeComponent();
            rutaHilos = rutaArchivo;
            cantidadHilos = numHilos;
            valorQuantum = quantum;
        }

        private void Resultados_Load(object sender, EventArgs e)
        {
            IngresoDatos inicio = new IngresoDatos();

            if (Directory.GetFiles(rutaHilos, "*.txt").Length != 0 )
            {
                string[] archivos = Directory.GetFiles(rutaHilos, "*.txt");

                    /*Verifico que la cantidad de txt de la carpeta coincida con la cantidad de hilos especificada por el usuario
                     y que estos sean menos que 12 que es el máximo contexto que tienen los procesadores*/
                    if (archivos.Length == cantidadHilos && cantidadHilos <= 12)
                    {
                        Controladora controlador = new Controladora(rutaHilos, cantidadHilos, valorQuantum);
                        controlador.ejecutarSimulacion();
                    }
                    else if (cantidadHilos > 12)
                    {
                        MessageBox.Show("La cantidad de hilos introducida por el usuario es mayor a 12 (máximo de hilos a ejecutar)", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        inicio.Show();
                        this.Close();
                    }

                    else if (archivos.Length != cantidadHilos)
                    {
                        MessageBox.Show("La cantidad de hilos introducida por el usuario no coincide con la cantidad de archivos en la ruta especificada", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        inicio.Show();
                        this.Close();
                    }
            }

            else 
            {
                MessageBox.Show("La ruta especificada no contiene archivos txt que simulen hilos", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                inicio.Show();
                this.Close();
            }
            
        }

    }
}
