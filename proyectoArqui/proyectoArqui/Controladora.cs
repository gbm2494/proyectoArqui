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
        /*Constante del total de procesadores de la simulación*/
        public const int totalProcesadores = 3;

        /*Método para iniciar el proceso de simulación
         * Recibe:
           Modifica:
           Retorna:
         */
        public void ejecutarSimulacion(string rutaArchivos, int hilos, int quantum)
        {
            /*leer los archivos y llenar la memoria de cada procesador y el contexto*/

            /*Verifica si la ruta especificada por el usuario tienen archivos .txt que simulan los hilos*/
            if (Directory.GetFiles(@rutaArchivos, "*.txt") != null)
            {
                /*array para guardar los nombres de todos los txt que tienen hilos*/
                string[] archivos = Directory.GetFiles(@rutaArchivos, "*.txt");

                /*Verifico que la cantidad de txt de la carpeta coincida con la cantidad de hilos especificada por el usuario
                 y que estos sean menos que 12 que es el máximo contexto que tienen los procesadores*/
                if (archivos.Length == hilos && hilos <= 12)
                {
                    /*Reparto los hilos entre los 3 procesadores*/
                    int contador = 0;

                    //array contador de cantidad de hilos por procesador, se debe inicializar en cero
                    int[] cantidadHilos = new int[totalProcesadores];

                    for (int i = 0; i < totalProcesadores; i++)
                    {
                        cantidadHilos[i] = 0;
                    }

                    /*Se reparte la cantidad de hilos entre el total de procesadores*/
                    while (contador < hilos)
                    {
                        cantidadHilos[contador % 3] = cantidadHilos[contador % 3] + 1;
                        contador++;
                    }

                    for (int i = 0; i < totalProcesadores; i++)
                    {
                       Debug.WriteLine("El procesador " + i + "tiene " + cantidadHilos[i] + " hilos");
                    }

                    //Creación de las 3 instancias de la clase procesador
                    Procesador procesador1 = new Procesador(cantidadHilos[0]);
                    Procesador procesador2 = new Procesador(cantidadHilos[1]);
                    Procesador procesador3 = new Procesador(cantidadHilos[2]);

                    //Para cada archivo del array de archivos se debe leer línea a línea el archivo para pasarlo a la memoria y el contexto del procesador
                    for (int i = 0; i < archivos.Length; i++)
                    {
                        
                        foreach (string line in File.ReadLines(@archivos[i], Encoding.UTF8))
                        {
                            //El archivo es del primer procesador
                            if (i < cantidadHilos[0])
                            {
                                Debug.WriteLine("Archivo " + archivos[i] + " es del procesador 1");
                                

                            }
                            //El archivo es del segundo procesador
                            else if (i < cantidadHilos[0] + cantidadHilos[1])
                            {
                                Debug.WriteLine("Archivo " + archivos[i] + " es del procesador 2");
                            }
                            //El archivo es del tercer procesador
                            else
                            {
                                Debug.WriteLine("Archivo " + archivos[i] + " es del procesador 3");
                            }
                        }
                    }

                    

                    //Creación de los 3 hilos, uno para cada procesador
                    Thread hiloProcesador1 = new Thread(new ThreadStart(procesador1.ejecutarInstrucciones));
                    Thread hiloProcesador2 = new Thread(new ThreadStart(procesador2.ejecutarInstrucciones));
                    Thread hiloProcesador3 = new Thread(new ThreadStart(procesador3.ejecutarInstrucciones));


                    /* AQUI SE INDICA EL QUANTUM, SE LLENA LA MEMORIA, SE LLENA EL CONTEXTO Y SE PONE EL PC DE CADA PROCESADOR (PARA ESO ULTIMA
                     * SE APUNTA AL PRIMER CAMPO DE CADA MEMORIA) */

                    /*Se ejecutan los hilos que simulan los procesador */
                    /*    hiloProcesador1.Start();
                          hiloProcesador2.Start();
                          hiloProcesador3.Start(); */

                    /* Ciclo que se ejecuta mientras hayan hilos de procesadores activos */
                    /*       while (hiloProcesador1.IsAlive || hiloProcesador2.IsAlive || hiloProcesador3.IsAlive)
                             {
                               /* El hilo principal alcanza la barrera de fin de instrucción. Una vez que los otros 3 hilos la alcancen se aumentará
                                  el reloj en cada procesador siempre y cuando éste se encuentre activo. */
                    /*            proyectoArqui.Procesador.barreraFinInstr.SignalAndWait();

                                   if (!procesador1.getEjecucion())
                                   {
                                       Debug.WriteLine("entre al proc 1 \n");
                                       Console.Write("entre al proc 1 \n");
                                       procesador1.aumentarReloj_Ciclos();
                                   }

                                   if (!procesador2.getEjecucion())
                                   {
                                       Debug.WriteLine("entre al proc 2 \n");
                                       Console.Write("entre al proc 1 \n");
                                       procesador2.aumentarReloj_Ciclos();
                                   }

                                   if (!procesador3.getEjecucion())
                                   {
                                       Debug.WriteLine("entre al proc 3 \n");
                                       Console.Write("entre al proc 3 \n");
                                       procesador3.aumentarReloj_Ciclos();
                                   }

                                   /* El hilo principal alcanza la barrera de fin cambio de reloj, donde se les indica a los otros hilos que pueden continuar
                                   con la lectura de la próxima instrucción */
                    /*      proyectoArqui.Procesador.barreraCambioReloj_Ciclo.SignalAndWait();

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
            else 
            { 
            /*La ruta de archivos no tiene hilos*/
            }

        }

        public void llenarDatosProcesador(int idProcesador)
        {

        }
    }
}
