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

        /*array para guardar los nombres de todos los txt que tienen hilos*/
        string[] archivos;

        //Creación de las 3 instancias de la clase procesador
        Procesador procesador1 ;
        Procesador procesador2 ;
        Procesador procesador3 ;

        /*valores recibidos de la clase interfaz, corresponde a la ruta de los hilos en el disco duro, la cantidad de hilos
         y el quantum*/
        string rutaArchivos;
        int hilos;
        int quantum;

        /*contador que almacena cuántos hilos tiene asigndo cada procesador*/
        int[] cantidadHilos;

        /* Constructor en el cual se lee la cantidad de archivos para repartirlos entre los 3 procesadores. Se crean además las instancias de los 
        procesadores al indicarle a cada uno cuántos hilos manejará y se les señala el valor del quantum para cada uno de ellos */
        public Controladora(string rutaHilos, int numHilos, int valorQuantum)
        {
            /*Se inicializan los atributos de la clase*/
            rutaArchivos = rutaHilos;
            hilos = numHilos;
            quantum = valorQuantum; 
            
            /*Si la ruta tiene archivos .txt entra al if*/
            if (Directory.GetFiles(@rutaArchivos, "*.txt") != null)
            {
                /*Se obtiene el nombre de los archivos en un array*/
                archivos = Directory.GetFiles(@rutaArchivos, "*.txt");

                    /*Reparto los hilos entre los 3 procesadores*/
                    int contador = 0;

                    //array contador de cantidad de hilos por procesador, se debe inicializar en cero
                    cantidadHilos = new int[totalProcesadores];

                    //Se inicializa el contador en cero
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

                    //Creación de las 3 instancias de la clase procesador
                    procesador1 = new Procesador(cantidadHilos[0]);
                    procesador2 = new Procesador(cantidadHilos[1]);
                    procesador3 = new Procesador(cantidadHilos[2]);

                    //Indicación del valor del quantum de acuerdo a lo ingresado por el usuario
                    procesador1.quantum = quantum;
                    procesador2.quantum = quantum;
                    procesador3.quantum = quantum;

            }
        }

        /*Método en el cual se llenan las memorias de los procesadores y sus contextos. Además se inicializa el valor del reloj en cada uno de 
        los primeros hilos a ejecutar de los procesadores  */
        public void llenarMemoria_Contexto()
        {
            /*posicion en memoria de los tres procesadores*/
            int[] posicionMemoria = new int[totalProcesadores];
            int[] hiloActual = new int[totalProcesadores];

            //Se inicializa el contador en cero de la posición en memoria de cada procesador así como el id del hilo actual 
            for (int i = 0; i < totalProcesadores; i++)
            {
                posicionMemoria[i] = 0;
                hiloActual[i] = 0;
            }

             //Para cada archivo del array de archivos se debe leer línea a línea el archivo para pasarlo a la memoria y el contexto del procesador
             for (int i = 0; i < archivos.Length; i++)
             {

             /*Carga del PC en el contexto de cada procesador*/
                 //El archivo es del primer procesador
                 if (i < cantidadHilos[0])
                 {
                     //Se llenan los datos del hilo en el procesador
                     procesador1.setNumHilo_Procesador(hiloActual[0], i+1, 1);
                     //Se llenan el PC del contexto del hilo
                     procesador1.contexto[hiloActual[0], 32] = posicionMemoria[0] + 128;
                     hiloActual[0]++;
                 }
                 //El archivo es del segundo procesador
                 else if (i < cantidadHilos[0] + cantidadHilos[1])
                 {
                     //Se llenan los datos del hilo en el procesador
                     procesador2.setNumHilo_Procesador(hiloActual[1], i + 1, 2);
                     //Se llenan el PC del contexto del hilo
                     procesador2.contexto[hiloActual[1], 32] = posicionMemoria[1] + 128;
                     hiloActual[1]++;
                 }
                 //El archivo es del tercer procesador
                 else
                 {
                     //Se llenan los datos del hilo en el procesador
                     procesador3.setNumHilo_Procesador(hiloActual[2], i + 1, 3);
                     //Se llenan el PC del contexto del hilo
                     procesador3.contexto[hiloActual[2], 32] = posicionMemoria[2] + 128;
                     hiloActual[2]++;
                 }

                 /*Carga de la memoria mediante cada línea del archivo*/
                 foreach (string line in File.ReadLines(@archivos[i], Encoding.UTF8))
                 {
                     /*Se divide la instrucción en sus 4 palabras*/
                     string[] divisionInstruccion = line.Split(' ');
                         
                     //El archivo es del primer procesador
                     if (i < cantidadHilos[0])
                     {
                         /*Se carga en memoria la instrucción del hilo*/
                         for (int pos = 0; pos < divisionInstruccion.Length; pos++)
                         {
                             procesador1.memoria[posicionMemoria[0]] = Convert.ToInt32(divisionInstruccion[pos]);
                             posicionMemoria[0]++;    
                         }
                             
                     }
                     //El archivo es del segundo procesador
                     else if (i < cantidadHilos[0] + cantidadHilos[1])
                     {
                         for (int pos = 0; pos < divisionInstruccion.Length; pos++)
                         {
                             /*Se carga en memoria la instrucción del hilo*/
                             procesador2.memoria[posicionMemoria[1]] = Convert.ToInt32(divisionInstruccion[pos]);
                             posicionMemoria[1]++;
                         }
                     }
                     //El archivo es del tercer procesador
                     else
                     {
                         for (int pos = 0; pos < divisionInstruccion.Length; pos++)
                         {
                             /*Se carga en memoria la instrucción del hilo*/
                             procesador3.memoria[posicionMemoria[2]] = Convert.ToInt32(divisionInstruccion[pos]);
                             posicionMemoria[2]++;
                         }
                     }
                 }
            
             }
 
            //Se inicializa el valor del reloj en cada uno de los primeros hilos a ejecutar de los procesadores
            //Se verifica que el procesador 1 posea uno o más hilos a ejecutar
            if(cantidadHilos[0] > 0)
            {
                //Se verifica que el procesador 2 posea uno o más hilos a ejecutar
                if(cantidadHilos[1] > 0)
                {
                    //Se verifica que el procesador 3 posea uno o más hilos a ejecutar
                    if(cantidadHilos[2] > 0)
                    {
                        procesador1.setValorInicialReloj();
                        procesador2.setValorInicialReloj();
                        procesador3.setValorInicialReloj();

                        //Se indica que ya el hilo va a inicar ejecucion
                        procesador1.ejecucionHilos[0, 0] = 1;
                        procesador2.ejecucionHilos[0, 0] = 1;
                        procesador3.ejecucionHilos[0, 0] = 1;
                    }


                    procesador1.setValorInicialReloj();
                    procesador2.setValorInicialReloj();

                    //Se indica que ya el hilo va a inicar ejecucion
                    procesador1.ejecucionHilos[0, 0] = 1;
                    procesador2.ejecucionHilos[0, 0] = 1;
                }

                procesador1.setValorInicialReloj();

                //Se indica que ya el hilo va a inicar ejecucion
                procesador1.ejecucionHilos[0, 0] = 1;

            }
             
             //Se inicia la ejecución de la simulación
             iniciarSimulacion();
        }

        /* Método que ejecuta la lectura y ejecución de las instrucciones de los hilillos ubicados en los hilos de tipo procesador */
        public void iniciarSimulacion()
        {
            //Creación de los 3 hilos, uno para cada procesador
            Thread hiloProcesador1 = new Thread(new ThreadStart(procesador1.ejecutarInstrucciones));
            Thread hiloProcesador2 = new Thread(new ThreadStart(procesador2.ejecutarInstrucciones));
            Thread hiloProcesador3 = new Thread(new ThreadStart(procesador3.ejecutarInstrucciones));


            /*Se ejecutan los hilos que simulan los procesadores */
            hiloProcesador1.Start();
            hiloProcesador2.Start();
            hiloProcesador3.Start(); 

            /* Ciclo que se ejecuta mientras hayan hilos de procesadores activos */
                   while (hiloProcesador1.IsAlive || hiloProcesador2.IsAlive || hiloProcesador3.IsAlive)
                     {
                       /* El hilo principal alcanza la barrera de fin de instrucción. Una vez que los otros 3 hilos la alcancen se aumentará
                          el reloj en cada procesador siempre y cuando éste se encuentre activo. */
                        proyectoArqui.Procesador.barreraFinInstr.SignalAndWait();

                           if (!procesador1.getEjecucion())
                           {
                               procesador1.aumentarReloj_Ciclos();
                           }

                           if (!procesador2.getEjecucion())
                           {
                               procesador2.aumentarReloj_Ciclos();
                           }

                           if (!procesador3.getEjecucion())
                           {
                               procesador3.aumentarReloj_Ciclos();
                           }

                           /* El hilo principal alcanza la barrera de fin cambio de reloj, donde se les indica a los otros hilos que pueden continuar
                           con la lectura de la próxima instrucción */
                           proyectoArqui.Procesador.barreraCambioReloj_Ciclo.SignalAndWait();

                }

                //El hilo principal termina la ejecución del método hasta que los otros 3 hilos finalicen ejecución
                hiloProcesador1.Join();
                hiloProcesador2.Join();
                hiloProcesador3.Join();
        }

        /*Método para obtener el nombre de todos los hilos que tengan el procesador con el id recibido por parámetro*/
        public string getNombreHilos(int idProcesador)
        {
            if (idProcesador == 0)
            {
                return procesador1.getNombreHilos();
            }
            else if (idProcesador == 1)
            {
                return procesador2.getNombreHilos();
            }
            else 
            {
                return procesador3.getNombreHilos();
            }
        }

        /*Método para obtener el valor de los registros del hilo con idHilo en el procesador con el id recibido por parámetro*/
        public string getContextoHilo(int idProcesador, int idHilo)
        {
            if (idProcesador == 0)
            {
                return procesador1.getContextoHilo(idHilo);
            }
            else if (idProcesador == 1)
            {
                return procesador2.getContextoHilo(idHilo);
            }
            else
            {
                return procesador3.getContextoHilo(idHilo);
            }
        }

        /*Método para obtener el valor de ciclos usados del hilo con idHilo en el procesador con el id recibido por parámetro*/
        public string getCicloHilo(int idProcesador, int idHilo)
        {
            if (idProcesador == 0)
            {
                return procesador1.getCicloHilo(idHilo);
            }
            else if (idProcesador == 1)
            {
                return procesador2.getCicloHilo(idHilo);
            }
            else
            {
                return procesador3.getCicloHilo(idHilo);
            }
        }

        /*Método para obtener el valor del reloj inicial del hilo con idHilo en el procesador con el id recibido por parámetro*/
        public string getInicialHilo(int idProcesador, int idHilo)
        {
            if (idProcesador == 0)
            {
                return procesador1.getInicialHilo(idHilo);
            }
            else if (idProcesador == 1)
            {
                return procesador2.getInicialHilo(idHilo);
            }
            else
            {
                return procesador3.getInicialHilo(idHilo);
            }
        }

        /*Método para obtener el valor del reloj final del hilo con idHilo en el procesador con el id recibido por parámetro*/
        public string getFinalHilo(int idProcesador, int idHilo)
        {
            if (idProcesador == 0)
            {
                return procesador1.getFinalHilo(idHilo);
            }
            else if (idProcesador == 1)
            {
                return procesador2.getFinalHilo(idHilo);
            }
            else
            {
                return procesador3.getFinalHilo(idHilo);
            }
        }
    }
}
