using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace proyectoArqui
{

    class Procesador
    {
        //Memoria compartida del procesador
        private const int cantidadMC = 32;
        public static int[] memoriaCompartida = new int[cantidadMC];
    //    public static int[] memoriaCompartida1 = new int[cantidadMC];
    //   public static int[] memoriaCompartida2 = new int[cantidadMC];
    //    public static int[] memoriaCompartida3 = new int[cantidadMC];

        //caché de datos del procesador
        private const int filasCacheDatos = 6;
        private const int bloquesCache = 4;
        public int[,] cacheDatos = new int[filasCacheDatos, bloquesCache];

      //  public static int[,] cacheDatos1 = new int[filasCacheDatos,bloquesCache];
      //  public static int[,] cacheDatos2 = new int[filasCacheDatos, bloquesCache];
      //  public static int[,] cacheDatos3 = new int[filasCacheDatos, bloquesCache]; 

        //directorio de bloques del procesador
        private const int bloquesDirectorio = 8;
        private const int columnasDirectorio = 5;
        public static int[,] directorio = new int[bloquesDirectorio, columnasDirectorio];
    /*    public static int[,] directorio1 = new int[bloquesDirectorio, columnasDirectorio];
        public static int[,] directorio2 = new int[bloquesDirectorio, columnasDirectorio];
        public static int[,] directorio3 = new int[bloquesDirectorio, columnasDirectorio]; */

        //bandera de la instrucción LL
        bool banderaLL = false;
        int bloqueLL;
       
        int[] hilosRL;

        //variable para almacenar el quantum
        public int quantum = 0;

        //variable para almacenar cuantos hilos tiene activos el procesador
        public int hilosActivos;

        //program counter del procesador
        private int PC = 128;

        //cache del procesador, 4 palabras + el bloque, cada palabra es de 1 byte, por lo tanto el tamaño es de 5x16 
        public const int filasCache = 5;
        public const int columnasCache = 16;
	    int[,] cache = new int[filasCache,columnasCache];

        //contiene los 32 registros del procesador
        private const int cantidadRegistros = 32;
	    private int[] registros = new int[cantidadRegistros];

        //Contiene el PC y los registros de cada hilo, primero los 32 registros y por último el PC
        public readonly int filasContexto;
        public readonly int columnasContexto = 33;
        public int[,] contexto; 

        //Variable para manejar el reloj del procesador
        private int reloj = 1;

        //Diccionario que asocia el operando con su correspondiente operacion
	    private Dictionary<int,string> operaciones = new Dictionary<int, string>(); 

        //vector para almacenar el número de bloque, palabra e indice a partir de la lectura del PC
	    private int[] ubicacion = new int[3];

        //Memoria principal del procesador, comienza en 128
        public const int cantidadMemoria = 256;
        public int[] memoria = new int[cantidadMemoria];

        //Se almacena el número de fila del contexto correspondiente al hilo ejecutándose actualmente
        private int filaContextoActual = 0;

        /* Barrera para controlar cuando todos los hilos han ejecutado una instrucción, son 4 participantes porque el hilo principal
        también debe interactuar con éstos*/
        public static Barrier barreraFinInstr = new Barrier(participantCount: 4);

        /* Barrera para controlar que todos los hilos esperen mientras el hilo principal les aumenta el reloj y la cantidad de ciclos, son 4
        participantes porque el hilo principal también debe interactuar con éstos*/
        public static Barrier barreraCambioReloj_Ciclo = new Barrier(participantCount: 4);
     
        /*Variable que se utiliza para saber si un procesador ya terminó todas las ejecuciones de sus hilillos */
        private bool terminarEjecucion = false;


        /*Arreglo donde el número de filas indican la cantidad de hilos a ejecutar, la primer columna simboliza el número del hilo, la segunda 
         la cantidad de ciclos realizados, la tercera el valor del reloj al iniciar la ejecución, la cuarta el valor del reloj al finalizar la 
         ejecución y la quinta el número del procesador donde se ejecutará el hilo. */
        public readonly int filasDatosHilos;
        public const int columnasDatosHilos = 5;
        public int[,] datosHilos;


        /* Arreglo utilizado para conocer el estado del hilo ejecutandose: si es la primera vez y si ya terminó ejecución */
        readonly int filasEjecucionHilos;
        readonly int columnasEjecucionHilos = 2;
        public int[,] ejecucionHilos;


        readonly int compartido = 0; //Compartido
        readonly int modificado = 1; //Modificado
        readonly int uncached = 2; //Uncached
        readonly int invalido = 3; //Invalido

        public List<Procesador> procesadores = new List<Procesador>();



        /*Método para pruebas que imprime en el debugger la memoria del procesador*/
        public void imprimirMemoria() {
            for (int i = 0; i < cantidadMemoria; i++)
                Debug.WriteLine(memoria[i]);
        }

        /*Método para pruebas que imprime en el debugger el contexto del procesador*/
        public void imprimirContexto()
        {
             for(int i = 0; i < filasContexto; i++)
             {
                 Debug.WriteLine(contexto[i,32]);
             }
        }

        /*Método para obtener el nombre de los hilos ejecutados en el procesador*/
        public string getNombreHilos()
        {
            string retorno = "";

            for (int i = 0; i < filasDatosHilos; i++)
            {
                retorno = retorno + " " + datosHilos[i, 0];
            }

            return retorno;
        }

        /*Método para obtener el contexto del hilo especificado por el parámetro del método*/
        public string getContextoHilo(int idHilo)
        {
            string retorno = ""; 

            for (int i = 0; i < columnasContexto - 1; i++)
            {
                retorno = retorno + contexto[idHilo, i] + " ";
            }

            return retorno;
        }

        /*Método para obtener la cantidad de ciclos del hilo especificado por el parámetro del método*/
        public string getCicloHilo(int idHilo)
        {
            string retorno = "";
            
            retorno = "" +  datosHilos[idHilo, 1];
            
            return retorno;
        }

        /*Método para obtener el reloj inicial del hilo especificado por el parámetro del método*/
        public string getInicialHilo(int idHilo)
        {
            string retorno = "";

            retorno = "" + datosHilos[idHilo, 2];

            return retorno;
        }

        /*Método para obtener el reloj final del hilo especificado por el parámetro del método*/
        public string getFinalHilo(int idHilo)
        {
            string retorno = "";

            retorno = "" + datosHilos[idHilo, 3];

            return retorno;
        }

        /*Constructor de la clase procesador*/
        public Procesador(int numHilos)
        {
            /*Operaciones de los hilos agregadas al diccionario*/
            operaciones.Add(8, "DADDI");
            operaciones.Add(32, "DADD");
            operaciones.Add(34, "DSUB");
            operaciones.Add(12, "DMUL");
            operaciones.Add(14, "DDIV");
            operaciones.Add(4, "BEQZ");
            operaciones.Add(5, "BNEZ");
            operaciones.Add(3, "JAL");
            operaciones.Add(2, "JR");
            operaciones.Add(63, "FIN");
            operaciones.Add(50, "LL");
            operaciones.Add(51, "SC");
            operaciones.Add(35, "LW");
            operaciones.Add(43, "SW");

            //La cantidad de filas del contexto corresponde a la cantidad de hilos que manejará el procesador
            filasContexto = numHilos;
            contexto = new int[filasContexto, columnasContexto];
            hilosRL = new int[numHilos];

            /* La cantidad de filas del arreglo que almacena los datos que se desplegarán
            al finalizar el programa corresponde a la cantidad de hilos que manejará el procesador */
            filasDatosHilos = numHilos;
            datosHilos = new int[filasDatosHilos, columnasDatosHilos];

            /* La cantidad de filas del arreglo que controla el estado de ejecución de cada hilo corresponde a la cantidad de hilos
            que manejará el procesador */
            filasEjecucionHilos = numHilos;
            ejecucionHilos = new int[filasEjecucionHilos, columnasEjecucionHilos];

            //La cantidad de hilos activos del procesador corresponde a la cantidad de hilos que manejará el procesador
            hilosActivos = numHilos;

            //Método que inicializa cada uno de los arreglos con ceros
            inicializarEstructuras();
        }

       
        /*Método para inicializar todas las estructuras utilizadas en valores correctos*/
        public void inicializarEstructuras()
        {

            //Se inicializa con ceros la cache
            for (int contadorFilas = 0; contadorFilas < filasCache; ++contadorFilas)
            {
                
                    for (int contadorColumnas = 0; contadorColumnas < columnasCache; ++contadorColumnas)
                    {
                        if (contadorFilas != filasCache - 1)
                        {
                            cache[contadorFilas, contadorColumnas] = 0;
                        }

                        else /* Se inicializa en -1 la última fila que corresponde al número del bloque guardado en caché */
                        {
                            cache[contadorFilas, contadorColumnas] = -1;
                        }
                    }  
            }

            //Se inicializa con ceros la memoria
            for (int i = 0; i < cantidadMemoria; ++i )
            {
                memoria[i] = 0;
            }

            //Se inicializa con ceros los registros
            for (int i = 0; i < cantidadRegistros; i++)
            {
                registros[i] = 0;
            }

            //se inicializa con ceros el contexto
            for (int i = 0; i < filasContexto; i++)
            {
                for (int j = 0; j < columnasContexto; j++)
                {
                    contexto[i, j] = 0;
                }
            }

            /* Se inicializa con ceros el arreglo que almacenará datos importantes sobre cada hilo, tales como el número del hilo, la cantidad de 
             ciclos realizados, el valor del reloj al iniciar la ejecución, el valor del reloj al finalizar la ejecución y el número del procesador
             donde se ejecutó */
            for(int i = 0; i < filasDatosHilos; ++i)
            {
                for(int j = 0; j < columnasDatosHilos; ++j)
                {
                    datosHilos[i, j] = 0;
                }
            }

            /* Se inicializa con ceros el arreglo que mantiene el estado de ejecución de cada hilo */
            for (int i = 0; i < filasEjecucionHilos; ++i)
            {
                for (int j = 0; j < columnasEjecucionHilos; ++j)
                {
                    ejecucionHilos[i, j] = 0;
                }
            }

            //Se inicializa con 1s la memoria compartida del procesador
            for (int i = 0; i < cantidadMC; i++)
            {
                memoriaCompartida[i] = 1;
            }


            //Se inicializa con ceros la cache de datos, con -1 en la posición del bloque y con inválida en el estado del bloque
            for (int contadorFilas = 0; contadorFilas < filasCacheDatos; ++contadorFilas)
            {

                for (int contadorColumnas = 0; contadorColumnas < bloquesCache; ++contadorColumnas)
                {
                    if (contadorFilas == filasCache - 1)
                    {
                        cacheDatos[contadorFilas, contadorColumnas] = invalido;
                    }
                    else if (contadorFilas == filasCache - 2)
                    {
                        cacheDatos[contadorFilas, contadorColumnas] = -1;
                    }

                    else /* Se inicializa en -1 la fila que corresponde al número del bloque guardado en caché */
                    {
                        cacheDatos[contadorFilas, contadorColumnas] = 0;
                    }
                }
            }
        }

        /*Método para indicar en el arreglo el número del hilo así como el número del procesador donde correrá el hilo */
        public void setNumHilo_Procesador(int numFila, int numHilo, int numProcesador)
        {
            datosHilos[numFila, 0] = numHilo;
            datosHilos[numFila, 4] = numProcesador;

          /*  if (numHilo == 1)
            {
                //Se inicializa con 1s la memoria compartida del procesador 1
                for (int i = 0; i < cantidadMC; i++)
                {
                    memoriaCompartida1[i] = 1;
                }
              
            }
            else if (numHilo == 2)
            {
                //Se inicializa con 1s la memoria compartida del procesador 2
                for (int i = 0; i < cantidadMC; i++)
                {
                    memoriaCompartida2[i] = 1;
                }
            }
            else
            {
                //Se inicializa con 1s la memoria compartida del procesador 3
                for (int i = 0; i < cantidadMC; i++)
                {
                    memoriaCompartida3[i] = 1;
                }
            } */
         
        }

        /*Método para inicializar la caché de datos de acuerdo al número del procesador ejecutándose */
        //public void inicializarCacheDatos(int numHilo)
        //{
        //    if (numHilo == 1)
        //    {
        //        //Se inicializa con ceros la cache de datos, con -1 en la posición del bloque y con inválida en el estado del bloque
        //        for (int contadorFilas = 0; contadorFilas < filasCacheDatos; ++contadorFilas)
        //        {

        //            for (int contadorColumnas = 0; contadorColumnas < bloquesCache; ++contadorColumnas)
        //            {
        //                if (contadorFilas == filasCache - 1)
        //                {
        //                    cacheDatos1[contadorFilas, contadorColumnas] = invalido;
        //                }
        //                else if (contadorFilas == filasCache - 2)
        //                {
        //                    cacheDatos1[contadorFilas, contadorColumnas] = -1;
        //                }

        //                else /* Se inicializa en -1 la fila que corresponde al número del bloque guardado en caché */
        //                {
        //                    cacheDatos1[contadorFilas, contadorColumnas] = 0;
        //                }
        //            }
        //        }
        //    }

        //    else if (numHilo == 2)
        //    {
        //        //Se inicializa con ceros la cache de datos, con -1 en la posición del bloque y con inválida en el estado del bloque
        //        for (int contadorFilas = 0; contadorFilas < filasCacheDatos; ++contadorFilas)
        //        {

        //            for (int contadorColumnas = 0; contadorColumnas < bloquesCache; ++contadorColumnas)
        //            {
        //                if (contadorFilas == filasCache - 1)
        //                {
        //                    cacheDatos2[contadorFilas, contadorColumnas] = invalido;
        //                }
        //                else if (contadorFilas == filasCache - 2)
        //                {
        //                    cacheDatos2[contadorFilas, contadorColumnas] = -1;
        //                }

        //                else /* Se inicializa en -1 la fila que corresponde al número del bloque guardado en caché */
        //                {
        //                    cacheDatos2[contadorFilas, contadorColumnas] = 0;
        //                }
        //            }
        //        }
        //    }

        //    else 
        //    {
        //        //Se inicializa con ceros la cache de datos, con -1 en la posición del bloque y con inválida en el estado del bloque
        //        for (int contadorFilas = 0; contadorFilas < filasCacheDatos; ++contadorFilas)
        //        {

        //            for (int contadorColumnas = 0; contadorColumnas < bloquesCache; ++contadorColumnas)
        //            {
        //                if (contadorFilas == filasCache - 1)
        //                {
        //                    cacheDatos3[contadorFilas, contadorColumnas] = invalido;
        //                }
        //                else if (contadorFilas == filasCache - 2)
        //                {
        //                    cacheDatos3[contadorFilas, contadorColumnas] = -1;
        //                }

        //                else /* Se inicializa en -1 la fila que corresponde al número del bloque guardado en caché */
        //                {
        //                    cacheDatos3[contadorFilas, contadorColumnas] = 0;
        //                }
        //            }
        //        }
        //    }
              
        //}

        public void inicializarMemoriaCompartida(int numHilo)
        {
 
        }

        /*Método para indicar en el arreglo el valor inicial del reloj al iniciar la ejecucion del hilo */
        public void setValorInicialReloj()
        {
            datosHilos[filaContextoActual, 2] = reloj;
        }

        /*Método para leer una instrucción en la cache*/
        public void leerInstruccion()
        {
            /*Calcula el bloque en memoria*/
            int bloque = PC / 16;

            /*Calcula la palabra en memoria*/
            int palabra = PC % 16;
            palabra = palabra / 4;

            /*Calcula el indice en la caché*/
            int indice = bloque % 4;

            /*Vector que guarda los datos obtenidos a partir de la lectura del PC que se esté ejecutando*/
            ubicacion[0] = bloque;
            ubicacion[1] = palabra;
            ubicacion[2] = indice;

            //Se ejecuta la instrucción porque estaba en caché	
            if (cache[4, indice * 4] == bloque)
            {
                /* Se cambia el valor del PC a la dirección de la próxima instrucción */
                PC = PC + 4;

                /* Se llama al método que ejecuta una instrucción en específico */
                ejecutarInstruccion();

                /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                ejecutada la instrucción */
                barreraFinInstr.SignalAndWait();

                /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                que todos ya pasaron la anterior, esta barrera se utiliza para esperar que el hilo principal le aumente a cada hilo de tipo 
                Procesador su reloj y ciclos */
                barreraCambioReloj_Ciclo.SignalAndWait();

            }
            else
            {
                // Se llama el metodo de fallo de cache
                ejecutarFalloCache();

                //For de 16 ciclos para simular lo que se tarda en subir un bloque de memoria principal a caché
                for (int i = 0; i < 16; ++i)
                {
                    barreraFinInstr.SignalAndWait();
                    barreraCambioReloj_Ciclo.SignalAndWait();
                }

            }
        }

        /*Método para ejecutar únicamente una instrucción */
        public void ejecutarInstruccion()
        {
            //Variable que almacenará el tipo de operación de acuerdo al código de la misma
            string operando;

            /* Variable utilizada para conocer el número de fila donde se encuentra la palabra que se desea ejecutar, Ubicacion[1] 
            posee el número de palabra a ejecutar */
            int contadorFilas = ubicacion[1];

            /* Se obtiene el primer operando de la palabra o instrucción */
            int codigoOperacion = cache[contadorFilas, ubicacion[2] * 4];

            if(operaciones.TryGetValue(codigoOperacion, out operando))
            {
                    switch (operando)
                    {
                        case "DADDI":
                            /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 2]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];
                            break;
                        case "DADD":
                            /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DSUB":
                            /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] - registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DMUL":
                            /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] * registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DDIV":
                            /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] / registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "BEQZ":
                            /* Se verifica la condición del salto: que el contenido del registro en la posición determinada por el número ubicado en
                            cache[contadorFilas, ubicacion[2] * 4 + 1]  sea igual a 0 */
                            if(registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] == 0)
                            {
                                /* Se multiplica la cantidad de instrucciones que debe retornarse por 4 debido a que una instrucción equivale a
                                 1 palabra, es decir, a 4 bytes, por lo que la dirección de la próxima instrucción a ejecutar estará a 4 bytes 
                                 de distancia. */
                                PC = PC + (cache[contadorFilas, ubicacion[2] * 4 + 3]*4);

                            }
                            break;
                        case "BNEZ":
                            /* Se verifica la condición del salto: que el contenido del registro en la posición determinada por el número ubicado en
                            cache[contadorFilas, ubicacion[2] * 4 + 1] sea distinto de 0 */
                            if (registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] != 0)
                            {
                                /* Se multiplica la cantidad de instrucciones que debe retornarse por 4 debido a que una instrucción equivale a
                                 1 palabra, es decir, a 4 bytes, por lo que la dirección de la próxima instrucción a ejecutar estará a 4 bytes 
                                 de distancia. */
                                PC = PC + (cache[contadorFilas, ubicacion[2] * 4 + 3] * 4);

                            }
                            break;
                        case "JAL":
                            registros[31] = PC;
                            PC = PC + cache[contadorFilas, ubicacion[2] * 4 + 3];
                            break;
                        case "JR":
                            PC = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]];
                            break;

                        case "LL":
                            break;

                        case "SC":
                            break;

                        case "LW":
                            break;

                        case "SW":
                            break;

                        default: //Instrucción de fin

                            /* Se disminuye la cantidad de hilos activos en el procesador */
                            --hilosActivos;

                            /* Se guarda el valor del reloj porque ya se terminó de ejecutar el hilo. Se guarda el valor del reloj aumentado porque
                            en este punto el hilo principal aún no ha aumentado el valor del reloj. */
                            datosHilos[filaContextoActual, 3] = reloj+1;

                            /* Se indica que el hilo especificado por la variable filaContextoActual ya no está en ejecucion */
                            ejecucionHilos[filaContextoActual, 1] = 1;
                            break;

                    }
            }

            else
            {

            }
        }

        public bool bloquearCacheLW()
        {

            // Solicita bloquear la caché
            if (Monitor.TryEnter(cacheDatos))
            {
                try
                {
                    //Se verifica si en la caché de datos se ubica el bloque leido
                    if (cacheDatos[3, ubicacion[2]] == ubicacion[0])
                    {
                        //Se realiza la lectura. Hacerlo con metodo

                    }
                    else
                    {
                        //Se verifica si la posición a reemplazar posee un bloque modificado
                        if (cacheDatos[4, ubicacion[2]] == modificado)
                        {
                            //El bloque pertenece al procesador 1
                            if (cacheDatos[3, ubicacion[2]] <= 7)
                            {
                                if (datosHilos[0, 4] == 1) //Se está ejecutando el procesador 1
                                {
                                    if (Monitor.TryEnter(procesadores.ElementAt(0).cacheDatos))
                                    {
                                        try
                                        {

                                        }
                                        finally
                                        {

                                        }
                                    }
                                }


                           
                            }
                            //El bloque pertenece al procesador 2
                            else if (cacheDatos[3, ubicacion[2]] > 7 && cacheDatos[3, ubicacion[2]] <= 15)
                            {

                            }
                            //El bloque pertenece al procesador 3
                            else
                            {
                                
                            }
                        }
                        else
                        {

                        }
                    }
                }
                finally
                {
                    // Ensure that the lock is released.
                    Monitor.Exit(cacheDatos);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /*Método para arreglar un fallo de caché, cargando el bloque desde la memoria a caché */
        public void ejecutarFalloCache()
        {
            /* Calcula la dirección fisica en memoria: se le resta 8, debido a que el conteo de bloques inicia en este número, con el fin
            de obtener la posición en la que inicia el bloque */
            int numBloque = ubicacion[0] - 8;

            /* Se multiplica por 16 porque cada bloque está compuesto de 16 bytes */
            int direccionFisica = numBloque * 16;
       

           /*Carga en caché lo que está apuntando la dirección fisica */
            for (int i = 0; i < 5; ++i)
            {
                for (int c = 0; c < 4; c++)
                {
                    if(i == 4)
                    {
                        /* Se ubica en la última fila de la caché el # de bloque al que están asociadas las palabras que se están cargando 
                         desde la memoria principal. Ubicacion[0] contiene el # de bloque. */
                        cache[i, ubicacion[2] * 4 + c] = ubicacion[0];

                    }
                    else
                    {
                        /* Se carga la palabra de la memoria principal a la caché, en ubicacin[2] se tiene almacenado el índice (# de columna*4)
                        de la caché donde se deben cargar las palabras. Se multiplica por 4 porque cada palabra está compuesta de 4 bytes. */
                        cache[i, ubicacion[2] * 4 + c] = memoria[direccionFisica + c];
                    }


                }

                /*La dirección fisica aumenta de 4 en 4 bytes*/
                direccionFisica = direccionFisica + 4;
            }
        }

        /* Método para incrementar el valor del reloj y la cantidad de ciclos */
        public void aumentarReloj_Ciclos()
        {
            //Se aumenta el reloj
            ++reloj;

            //La columna 1 del arreglo datosHilos[] simboliza la cantidad de ciclos, por lo que se aumenta dicha cantidad. 
            ++datosHilos[filaContextoActual, 1];
        }

        /* Método para obtener el valor de la variable terminarEjecución que indica si el procesador se encuentra aún con hilos pendientes
         o no */
        public bool getEjecucion()
        {
            return terminarEjecucion;
        }

        /*Método para ejecutar instrucciones por parte del procesador */
        public void ejecutarInstrucciones() 
        { 
            
            //Variable que sirve como contador para controlar que no se haya excedido el valor del quantum
            int contadorInstrucciones = 0;
            
            //Variable que se utiliza para recorrer el vector de registros 
            int contadorContexto = 0;

            while (hilosActivos > 0)
            {

                //Se ejecuta el ciclo mientras el contador sea menor que el quantum y mientras la ejecucion del hilo actual no haya terminado
                while (contadorInstrucciones < quantum  && ejecucionHilos[filaContextoActual, 1] == 0)
                {
                    contadorInstrucciones++;
                    leerInstruccion();               
                }

                    //Se copia en el contexto del hilo que se estaba ejecutando el valor de los registros porque se acabó el quantum
                    for (contadorContexto = 0; contadorContexto < columnasContexto - 1; ++contadorContexto)
                    {
                        contexto[filaContextoActual, contadorContexto] = registros[contadorContexto];
                    }

                    
                    //Se copia en la última columna del contexto el PC a ejecutar posteriormente o -1 si ya el hilo se terminó de ejecutar
                    if (ejecucionHilos[filaContextoActual, 1] == 1)
                    {
                        contexto[filaContextoActual, contadorContexto] = -1;
                    }
                    else
                    {
                        contexto[filaContextoActual, contadorContexto] = PC;
                    }


                    //Se inicializa en 0 nuevamente el contador de instrucciones
                    contadorInstrucciones = 0;

                  
                    //Se aumenta el valor del hilo ejecutandose actualmente
                    ++filaContextoActual;

                    /* Se verifica si la fila actual del contexto es la última, pues en caso de serlo el valor de la búsqueda del hilo a ejecutar
                    debe iniciar en 0. Se utiliza un ciclo que se lleva a cabo mientras el hilo actual ya haya terminado su ejecución y mientras
                    este valor no exceda la cantidad de filas que posee el contexto. */
                    if (filaContextoActual == filasContexto)
                    {
                        filaContextoActual = 0;
                        while (filaContextoActual < filasContexto && contexto[filaContextoActual, columnasContexto - 1] == -1)
                        {
                            ++filaContextoActual;
                        }

                        if (filaContextoActual < filasContexto)
                        {
                            PC = contexto[filaContextoActual, columnasContexto - 1];
                        }
                    }
                    else
                    {
                        int contador = 0;
                        while (contador < filasContexto && contexto[filaContextoActual, columnasContexto - 1] == -1)
                        {
                            if (filaContextoActual == filasContexto - 1)
                            {
                                filaContextoActual = 0;
                            }
                            else
                            {
                                ++filaContextoActual;
                            }

                            ++contador;

                        }


                        PC = contexto[filaContextoActual, columnasContexto - 1];
                    }

                    /* Se verifica que el valor del hilo a ejecutar sea uno válido, es decir, que sea menor que la cantidad de filas del contexto */
                    if (filaContextoActual < filasContexto)
                    {
                        //Se copia en los registros el contexto del hilo a ejecutar proximamente
                        for (contadorContexto = 0; contadorContexto < columnasContexto - 1; ++contadorContexto)
                        {
                            registros[contadorContexto] = contexto[filaContextoActual, contadorContexto];
                        }

                        /*Se verifica si es la primera vez que se ejecuta el hilo, pues en caso de serlo se debe guardar el valor actual del reloj */
                        if (ejecucionHilos[filaContextoActual, 0] == 0)
                        {
                            ejecucionHilos[filaContextoActual, 0] = 1;
                            datosHilos[filaContextoActual, 2] = reloj;
                        }
                    }

            }
            
            //Se indica que el procesador ya no posee hilos activos
            terminarEjecucion = true;

            //Se disminuye en 1 la cantidad de partipantes en la barrera de sincronización de instrucciones pues ya el procesador terminó su labor
            barreraFinInstr.RemoveParticipant();

            //Se disminuye en 1 la cantidad de partipantes en la barrera de sincronización de cambio de reloj pues ya el procesador terminó su labor
            barreraCambioReloj_Ciclo.RemoveParticipant();
        }
    }
}
