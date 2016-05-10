using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace proyectoArqui
{
    class Controladora
    {
        /*Método para iniciar el proceso de simulación */
        public void ejecutarSimulacion(string rutaArchivos, int hilos, int quantum)
        {
            /*Ver cuantos hilos son para cada procesador, 
             * leer los archivos y llenar la memoria de cada procesador y el contexto*/

            if (Directory.GetFiles(@rutaArchivos, "*.txt") != null)
            {
                string[] filePaths = Directory.GetFiles(@rutaArchivos, "*.txt");

                if (filePaths.Length == hilos && hilos <= 12)
                {
                    int hiloProcesador = hilos / 3;
                   
                }
            }

            //Se debe meter en cada memoria del procesador lo que contienen los archivos
            //También se debe ir llenando el contexto de cada procesador, en la seccion de
            //registros se ponen ceros, en la sección del PC se pone la ubicacion en memoria
            //donde se almacenó este número.
        }
    }
}
