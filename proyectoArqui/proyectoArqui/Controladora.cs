using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
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

            //Creación de las 3 instancias de la clase procesador
            Procesador procesador1 = new Procesador();
            Procesador procesador2 = new Procesador();
            Procesador procesador3 = new Procesador();

            //Creación de los 3 hilos, uno para cada procesador
            Thread hiloProcesador1 = new Thread(new ThreadStart(procesador1.ejecutarInstrucciones));
            Thread hiloProcesador2 = new Thread(new ThreadStart(procesador2.ejecutarInstrucciones));
            Thread hiloProcesador3 = new Thread(new ThreadStart(procesador3.ejecutarInstrucciones));


            /* AQUI SE INDICA EL QUANTUM, SE LLENA LA MEMORIA, SE LLENA EL CONTEXTO Y SE PONE EL PC DE CADA PROCESADOR (PARA ESO ULTIMA
             * SE APUNTA AL PRIMER CAMPO DE CADA MEMORIA) */

            /*Se ejecutan los hilos que simulan los procesador */
       /*     hiloProcesador1.Start();
            hiloProcesador2.Start();
            hiloProcesador3.Start(); */

            /* Ciclo que se ejecuta mientras hayan hilos de procesadores activos */
     /*       while (hiloProcesador1.IsAlive || hiloProcesador2.IsAlive || hiloProcesador3.IsAlive)
            {
                /* El hilo principal alcanza la barrera de fin de instrucción. Una vez que los otros 3 hilos la alcancen se aumentará
                el reloj en cada procesador siempre y cuando éste se encuentre activo. */
     /*           proyectoArqui.Procesador.barreraFinInstr.SignalAndWait();

                if (!procesador1.getEjecucion())
                {
                    Debug.WriteLine("entre al proc 1 \n");
                    Console.Write("entre al proc 1 \n");
                    procesador1.aumentarReloj();
                }

                if (!procesador2.getEjecucion())
                {
                    Debug.WriteLine("entre al proc 2 \n");
                    Console.Write("entre al proc 1 \n");
                    procesador2.aumentarReloj();
                }

                if (!procesador3.getEjecucion())
                {
                    Debug.WriteLine("entre al proc 3 \n");
                    Console.Write("entre al proc 3 \n");
                    procesador3.aumentarReloj();
                }

                /* El hilo principal alcanza la barrera de fin cambio de reloj, donde se les indica a los otros hilos que pueden continuar
                 con la lectura de la próxima instrucción */
      /*          proyectoArqui.Procesador.barreraCambioReloj.SignalAndWait();

            }

        
       /*   hiloProcesador1.Join();
            hiloProcesador2.Join();
            hiloProcesador3.Join(); */

            /* El hilo principal muestra los resultados finales de cada procesador */



            //Se debe meter en cada memoria del procesador lo que contienen los archivos
            //También se debe ir llenando el contexto de cada procesador, en la seccion de
            //registros se ponen ceros, en la sección del PC se pone la ubicacion en memoria
            //donde se almacenó este número.
        }
    }
}
