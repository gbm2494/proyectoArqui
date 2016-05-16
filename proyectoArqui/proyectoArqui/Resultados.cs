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
        /*valores para la simulación recibidos por el usuario*/
        string rutaHilos;
        int cantidadHilos;
        int valorQuantum;

        /*Objeto de tipo Controladora que se encarga de la simulación*/
        Controladora controlador;

        /*Constante del total de procesadores de la simulación*/
        public const int totalProcesadores = 3;

        /*Constructor de la clase que inicializa los atributos y la interfaz*/
        public Resultados(string rutaArchivo, int numHilos, int quantum)
        {
            InitializeComponent();
            rutaHilos = rutaArchivo;
            cantidadHilos = numHilos;
            valorQuantum = quantum;
        }

        /*Método Load de la interfaz*/
        private void Resultados_Load(object sender, EventArgs e)
        {
            /*Se crea un objeto de tipo interfaz inicial para ser llamada en caso de recibir valores erroneos por el usuario*/
            IngresoDatos inicio = new IngresoDatos();

            /*Si existen archivos válidos de hilos para la simulación*/
            if (Directory.GetFiles(rutaHilos, "*.txt").Length != 0 )
            {
                /*Se obtiene los nombres de los hilos en la carpeta*/
                string[] archivos = Directory.GetFiles(rutaHilos, "*.txt");

                    /*Verifico que la cantidad de txt de la carpeta coincida con la cantidad de hilos especificada por el usuario
                     y que estos sean menos que 12 que es el máximo contexto que tienen los procesadores*/
                    if (archivos.Length == cantidadHilos && cantidadHilos <= 12)
                    {
                        /*Si todo es válido se crea el objeto de tipo Controladora, se ejecuta la simulación y se muestran resultados*/
                        controlador = new Controladora(rutaHilos, cantidadHilos, valorQuantum);
                        controlador.llenarMemoria_Contexto();
                        llenarInterfaz();
                    }
                    /*cantidad de hilos es mayor a 12, no puede ser ejecutado en la simulación*/
                    else if (cantidadHilos > 12)
                    {
                        MessageBox.Show("La cantidad de hilos introducida por el usuario es mayor a 12 (máximo de hilos a ejecutar)", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        inicio.Show();
                        this.Close();
                    }
                    /*Si la cantidad de hilos no coincide con la cantidad de archivos proporcionados*/
                    else if (archivos.Length != cantidadHilos)
                    {
                        MessageBox.Show("La cantidad de hilos introducida por el usuario no coincide con la cantidad de archivos en la ruta especificada", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        inicio.Show();
                        this.Close();
                    }
            }
            /*Si no existen archivos válidos en la carpeta*/
            else 
            {
                MessageBox.Show("La ruta especificada no contiene archivos txt que simulen hilos", "Error de archivos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                inicio.Show();
                this.Close();
            }
            
        }

        /*Método para llenar los componentes gráficos de la interfaz*/
        public void llenarInterfaz() 
        {
            /*Carga valores genericos para los registros la primera vez*/
            string registros = "R1 R2 R3 R4 R5 R6 R7 R8 R9 R10 R11 R12 R13 R14 R15 R16 R17 R18 R19 R20 R21 R22 R23 R24 R25 R26 R27 R28 R29 R30 R31 R32";
            string[] regs = registros.Split(' ');

            /*Se cargan los registros iniciales en los 3 procesadores*/
            for (int i = 0; i < regs.Length; i++)
            {
                if (!regs[i].Equals(""))
                {
                    listRegistrosP1.Items.Add(regs[i]);
                    listRegistrosP2.Items.Add(regs[i]);
                    listRegistrosP3.Items.Add(regs[i]);
                }
            }

            /*Se obtiene el nombre de los hilos para cada procesador y son cargados en los combobox*/
            string[] nombres = new string[totalProcesadores];

            for (int i = 0; i < totalProcesadores; i++)
            {
                nombres[i] = controlador.getNombreHilos(i);
            }
           
            string[] nombresDivididosP1 = nombres[0].Split(' ');
            string[] nombresDivididosP2 = nombres[1].Split(' ');
            string[] nombresDivididosP3 = nombres[2].Split(' ');

            /*Se carga en los combobox la opción inicial del seleccione*/
            cmbHilosP1.Items.Add("-- Seleccione --");
            cmbHilosP2.Items.Add("-- Seleccione --");
            cmbHilosP3.Items.Add("-- Seleccione --");

            /*Se establece la opción por defecto de los combobox*/
            cmbHilosP1.SelectedIndex = 0;
            cmbHilosP2.SelectedIndex = 0;
            cmbHilosP3.SelectedIndex = 0;

            /*Se cargan los nombres de los hilos del procesador 1*/
            for (int i = 0; i < nombresDivididosP1.Length; i++)
            {
                if (!nombresDivididosP1[i].Equals(""))
                {
                    cmbHilosP1.Items.Add(nombresDivididosP1[i]);
                }
            }

            /*Se cargan los nombres de los hilos del procesador 2*/
            for (int i = 0; i < nombresDivididosP2.Length; i++)
            {
                if (!nombresDivididosP2[i].Equals(""))
                {
                    cmbHilosP2.Items.Add(nombresDivididosP2[i]);
                }
            }

            /*Se cargan los nombres de los hilos del procesador 3*/
            for (int i = 0; i < nombresDivididosP3.Length; i++)
            {
                if (!nombresDivididosP3[i].Equals(""))
                {
                    cmbHilosP3.Items.Add(nombresDivididosP3[i]);
                }
            }
        }

        /*Evento si la selección del combobox del procesador 1 cambia*/
        private void cmbHilosP1_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*Se limpia la lista de registros*/
            listRegistrosP1.Items.Clear();

            /*Se cargan los nombres de los registros*/
            string registros = "R0 R1 R2 R3 R4 R5 R6 R7 R8 R9 R10 R11 R12 R13 R14 R15 R16 R17 R18 R19 R20 R21 R22 R23 R24 R25 R26 R27 R28 R29 R30 R31";
            string[] regs = registros.Split(' ');

            /*Si el usuario selecciona "Seleccione"*/
            if (cmbHilosP1.SelectedIndex == 0)
            {
                /*Se inicializan los labels nuevamente*/
                lblCicloP1.Text = "#";
                lblInicioRelojP1.Text = "00:00:00";
                lblFinRelojP1.Text = "00:00:00";

                /*Carga valores genericos para los registros */
                for (int i = 0; i < regs.Length; i++)
                {
                    if (!regs[i].Equals(""))
                    {
                        listRegistrosP1.Items.Add(regs[i]);
                    }
                }
            }
            /*Si se selecciona un hilo válido del combobox*/
            else
            {
                /*Se carga el valor final de los registros en el contexto*/
                string valorRegistros = controlador.getContextoHilo(0, cmbHilosP1.SelectedIndex - 1);
                string[] valorRegs = valorRegistros.Split(' ');

                    /*Se cargan en pantalla el valor de los registros*/
                    for (int i = 0; i < regs.Length; i++)
                    {
                        if (!regs[i].Equals("") && !valorRegs[i].Equals(""))
                        {
                            listRegistrosP1.Items.Add(regs[i] + ": " + valorRegs[i]);
                        }
                    }

                    /*Se obtiene el valor de ciclos y el reloj inicial y final del hilo*/
                    lblCicloP1.Text = controlador.getCicloHilo(0, cmbHilosP1.SelectedIndex - 1);
                    lblInicioRelojP1.Text = controlador.getInicialHilo(0, cmbHilosP1.SelectedIndex - 1);
                    lblFinRelojP1.Text = controlador.getFinalHilo(0, cmbHilosP1.SelectedIndex - 1); ;
            }
        }

        /*Evento si la selección del combobox del procesador 2 cambia*/
        private void cmbHilosP2_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*Se limpia la lista de registros*/
            listRegistrosP2.Items.Clear();

            /*Se cargan los nombres de los registros*/
            string registros = "R0 R1 R2 R3 R4 R5 R6 R7 R8 R9 R10 R11 R12 R13 R14 R15 R16 R17 R18 R19 R20 R21 R22 R23 R24 R25 R26 R27 R28 R29 R30 R31";
            string[] regs = registros.Split(' ');

            /*Si el usuario selecciona "Seleccione"*/
            if (cmbHilosP2.SelectedIndex == 0)
            {
                /*Se inicializan los labels nuevamente*/
                lblCicloP2.Text = "#";
                lblInicioRelojP2.Text = "00:00:00";
                lblFinRelojP2.Text = "00:00:00";

                /*Carga valores genericos para los registros */
                for (int i = 0; i < regs.Length; i++)
                {
                    if (!regs[i].Equals(""))
                    {
                        listRegistrosP2.Items.Add(regs[i]);
                    }
                }
            }
            /*Si se selecciona un hilo válido del combobox*/
            else 
            {
                /*Se carga el valor final de los registros en el contexto*/
                string valorRegistros = controlador.getContextoHilo(1, cmbHilosP2.SelectedIndex - 1);
                string[] valorRegs = valorRegistros.Split(' ');

                /*Se cargan en pantalla el valor de los registros*/
                for (int i = 0; i < regs.Length; i++)
                {
                    if (!regs[i].Equals(""))
                    {
                        listRegistrosP2.Items.Add(regs[i] + ": " + valorRegs[i]);
                    }
                }

                /*Se obtiene el valor de ciclos y el reloj inicial y final del hilo*/
                lblCicloP2.Text = controlador.getCicloHilo(1, cmbHilosP2.SelectedIndex - 1);
                lblInicioRelojP2.Text = controlador.getInicialHilo(1, cmbHilosP2.SelectedIndex - 1); ;
                lblFinRelojP2.Text = controlador.getFinalHilo(1, cmbHilosP2.SelectedIndex - 1); ;
            }
        }

        /*Evento si la selección del combobox del procesador 3 cambia*/
        private void cmbHilosP3_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*Se limpia la lista de registros*/
            listRegistrosP3.Items.Clear();

            /*Se cargan los nombres de los registros*/
            string registros = "R0 R1 R2 R3 R4 R5 R6 R7 R8 R9 R10 R11 R12 R13 R14 R15 R16 R17 R18 R19 R20 R21 R22 R23 R24 R25 R26 R27 R28 R29 R30 R31";
            string[] regs = registros.Split(' ');

            /*Si el usuario selecciona "Seleccione"*/
            if (cmbHilosP3.SelectedIndex == 0)
            {
                /*Se inicializan los labels nuevamente*/
                lblCicloP3.Text = "#";
                lblInicioRelojP3.Text = "00:00:00";
                lblFinRelojP3.Text = "00:00:00";

                /*Carga valores genericos para los registros si se presiona seleccione*/
                for (int i = 0; i < regs.Length; i++)
                {
                    if (!regs[i].Equals(""))
                    {
                        listRegistrosP3.Items.Add(regs[i]);
                    }
                }
            }
            /*Si se selecciona un hilo válido del combobox*/
            else
            {
                /*Se carga el valor final de los registros en el contexto*/
                string valorRegistros = controlador.getContextoHilo(2, cmbHilosP3.SelectedIndex - 1);
                string[] valorRegs = valorRegistros.Split(' ');

                /*Se cargan en pantalla el valor de los registros*/
                for (int i = 0; i < regs.Length; i++)
                {
                    if (!regs[i].Equals(""))
                    {
                        listRegistrosP3.Items.Add(regs[i] + ": " + valorRegs[i]);
                    }
                }

                /*Se obtiene el valor de ciclos y el reloj inicial y final del hilo*/
                lblCicloP3.Text = controlador.getCicloHilo(2, cmbHilosP3.SelectedIndex - 1);
                lblInicioRelojP3.Text = controlador.getInicialHilo(2, cmbHilosP3.SelectedIndex - 1);
                lblFinRelojP3.Text = controlador.getFinalHilo(2, cmbHilosP3.SelectedIndex - 1);
            }
        }

    }
}
