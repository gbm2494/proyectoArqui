﻿using System;
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
        /*Lista de accciones para el LW*/
        private const int solicitudCacheDiagrama2_LW = 1;


        //Memoria compartida del procesador
        public const int cantidadMC = 128;
        public int[] memoriaCompartida = new int[cantidadMC];
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
        public int[,] directorio = new int[bloquesDirectorio, columnasDirectorio];
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
        int[,] cache = new int[filasCache, columnasCache];

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
        private Dictionary<int, string> operaciones = new Dictionary<int, string>();

        //vector para almacenar el número de bloque, palabra e indice a partir de la lectura del PC
        private int[] ubicacion = new int[3];

        //vector para almacenar el número de bloque, palabra e índice a partir de la dirección donde se desea realizar la lectura o escritura
        private int[] ubicacionMem = new int[3];

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

        // Variables que almacenan el inicio de la memoria compartida de cada procesador
        readonly int inicioMemProcesador1 = 0;
        readonly int inicioMemProcesador2 = 128;
        readonly int inicioMemProcesador3 = 256;


        /*Método para pruebas que imprime en el debugger la memoria del procesador*/
        public void imprimirMemoria()
        {
            for (int i = 0; i < cantidadMemoria; i++)
                Debug.WriteLine(memoria[i]);
        }

        /*Método para pruebas que imprime en el debugger el contexto del procesador*/
        public void imprimirContexto()
        {
            for (int i = 0; i < filasContexto; i++)
            {
                Debug.WriteLine(contexto[i, 32]);
            }
        }

        public string getMemoriaCompartida()
        {
            string retorno = "";

            for (int i = 0; i < cantidadMC; i++)
            {
                retorno = retorno + memoriaCompartida[i] + " ";
            }
            return retorno;
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

            retorno = "" + datosHilos[idHilo, 1];

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
            for (int i = 0; i < cantidadMemoria; ++i)
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
            for (int i = 0; i < filasDatosHilos; ++i)
            {
                for (int j = 0; j < columnasDatosHilos; ++j)
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
            for (int contadorColumnas = 0; contadorColumnas < bloquesCache; ++contadorColumnas)
            {

                for (int contadorFilas = 0; contadorFilas < filasCacheDatos; ++contadorFilas)
                {
                    if (contadorFilas == filasCacheDatos - 1) /*Se inicializa en inválida el estado de cada bloque en caché da datos */
                    {
                        cacheDatos[contadorFilas, contadorColumnas] = invalido;
                    }
                    else if (contadorFilas == filasCacheDatos - 2)  /* Se inicializa en -1 la fila que corresponde al número del bloque guardado en caché */
                    {
                        cacheDatos[contadorFilas, contadorColumnas] = -1;
                    }

                    else
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

            //Variable para guardar el número del bloque en la memoria compartida
            int numBloqueMemComp = 0;

            //Variable para guardar la posición en la caché de datos
            int posicionCacheDatos = 0;

            //Variable para guardar el número de la dirección donde inicia el bloque en la memoria compartida
            int numDireccionMemComp = 0;

            /* Variable utilizada para conocer el número de fila donde se encuentra la palabra que se desea ejecutar, Ubicacion[1] 
            posee el número de palabra a ejecutar */
            int contadorFilas = ubicacion[1];

            int contenidoReg1 = 0;
            int contenidoReg2 = 0;


            //Variable que se utiliza para saber si es LW o LL la instrucción
            bool LW = false;

            /* Se obtiene el primer operando de la palabra o instrucción */
            int codigoOperacion = cache[contadorFilas, ubicacion[2] * 4];

            if (operaciones.TryGetValue(codigoOperacion, out operando))
            {
                int direccionMemoria = 0;
                switch (operando)
                {
                    case "DADDI":
                        /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */
                        registros[cache[contadorFilas, ubicacion[2] * 4 + 2]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];
                        break;
                    case "DADD":
                        /* Ubicacion[2] contiene el índice de la caché donde se encuentra el bloque almacenado  */

                        contenidoReg1 = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]];
                        contenidoReg2 = registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];

                        registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = contenidoReg1 + contenidoReg2;

                        if(datosHilos[filaContextoActual, 4] == 1)
                        {

                            int num1 = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]];
                            int num2 = registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];

                            int num3 = num1 + num2;

                            Debug.WriteLine("esto contiene el registro num " + cache[contadorFilas, ubicacion[2] * 4 + 1] + " " + registros[cache[contadorFilas, ubicacion[2] * 4 + 1]]);
                            Debug.WriteLine("esto contiene el registro num " + cache[contadorFilas, ubicacion[2] * 4 + 2] + " " + registros[cache[contadorFilas, ubicacion[2] * 4 + 2]]);
                            Debug.WriteLine("esto contiene el registro num " + cache[contadorFilas, ubicacion[2] * 4 + 3] + " " + registros[cache[contadorFilas, ubicacion[2] * 4 + 3]]);
                        }
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
                        if (registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] == 0)
                        {
                            /* Se multiplica la cantidad de instrucciones que debe retornarse por 4 debido a que una instrucción equivale a
                             1 palabra, es decir, a 4 bytes, por lo que la dirección de la próxima instrucción a ejecutar estará a 4 bytes 
                             de distancia. */
                            PC = PC + (cache[contadorFilas, ubicacion[2] * 4 + 3] * 4);

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

                        LW = false;
                        direccionMemoria = 0;  
                     
                        //Se calcula la direccion de memoria                      
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];
                        
                        //Se calcula en número de bloque en memoria compartida
                        numBloqueMemComp = direccionMemoria / 16;

                        //Se calcula la posición en donde debería estar el bloque en la caché de datos
                        posicionCacheDatos = numBloqueMemComp % 4;

                        //Se calcula la dirección dónde se ubica el bloque en la memoria compartida
                        numDireccionMemComp = numBloqueMemComp * 16;

                        while (bloquearCacheLW(direccionMemoria, cache[contadorFilas, ubicacion[2] * 4 + 2], numBloqueMemComp, posicionCacheDatos, numDireccionMemComp, LW) == false)
                        {
                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            ejecutada la instrucción */
                            barreraFinInstr.SignalAndWait();

                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            que todos ya pasaron la anterior, esta barrera se utiliza para esperar que el hilo principal le aumente a cada hilo de tipo 
                            Procesador su reloj y ciclos */
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        break;
                    case "SC":
                        direccionMemoria = 0;

                        //Se calcula la direccion de memoria
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];

                        //Se calcula la direccion de memoria                      
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];

                        //Se calcula en número de bloque en memoria compartida
                        numBloqueMemComp = direccionMemoria / 16;

                        //Se calcula la posición en donde debería estar el bloque en la caché de datos
                        posicionCacheDatos = numBloqueMemComp % 4;

                        //Se calcula la dirección dónde se ubica el bloque en la memoria compartida
                        numDireccionMemComp = numBloqueMemComp * 16;

                        while (bloquearCacheSC(direccionMemoria, cache[contadorFilas, ubicacion[2] * 4 + 2], numBloqueMemComp, posicionCacheDatos, numDireccionMemComp) == false)
                        {
                            hilosRL[filaContextoActual] = 0;
                            banderaLL = false;

                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            ejecutada la instrucción */
                            barreraFinInstr.SignalAndWait();

                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            que todos ya pasaron la anterior, esta barrera se utiliza para esperar que el hilo principal le aumente a cada hilo de tipo 
                            Procesador su reloj y ciclos */
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }


                        break;

                    case "LW":

                        LW = true;

                        direccionMemoria = 0;  
                     
                        //Se calcula la direccion de memoria                      
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];

                        hilosRL[filaContextoActual] = direccionMemoria;
                        
                        //Se calcula en número de bloque en memoria compartida
                        numBloqueMemComp = direccionMemoria / 16;

                        //Se calcula la posición en donde debería estar el bloque en la caché de datos
                        posicionCacheDatos = numBloqueMemComp % 4;

                        //Se calcula la dirección dónde se ubica el bloque en la memoria compartida
                        numDireccionMemComp = numBloqueMemComp * 16;

                        while (bloquearCacheLW(direccionMemoria, cache[contadorFilas, ubicacion[2] * 4 + 2], numBloqueMemComp, posicionCacheDatos, numDireccionMemComp, LW) == false)
                        {
                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            ejecutada la instrucción */
                            barreraFinInstr.SignalAndWait();

                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            que todos ya pasaron la anterior, esta barrera se utiliza para esperar que el hilo principal le aumente a cada hilo de tipo 
                            Procesador su reloj y ciclos */
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        break;

                    case "SW":
                         direccionMemoria = 0;

                        //Se calcula la direccion de memoria
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];

                        //Se calcula la direccion de memoria                      
                        direccionMemoria = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];

                        //Se calcula en número de bloque en memoria compartida
                        numBloqueMemComp = direccionMemoria / 16;

                        //Se calcula la posición en donde debería estar el bloque en la caché de datos
                        posicionCacheDatos = numBloqueMemComp % 4;

                        //Se calcula la dirección dónde se ubica el bloque en la memoria compartida
                        numDireccionMemComp = numBloqueMemComp * 16;

                        while (bloquearCacheSW(direccionMemoria, cache[contadorFilas, ubicacion[2] * 4 + 2], numBloqueMemComp, posicionCacheDatos, numDireccionMemComp) == false)
                        {
                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            ejecutada la instrucción */
                            barreraFinInstr.SignalAndWait();

                            /* Barrera de sincronización que controla que todos los hilos de tipo Procesador y el hilo principal la alcancen una vez 
                            que todos ya pasaron la anterior, esta barrera se utiliza para esperar que el hilo principal le aumente a cada hilo de tipo 
                            Procesador su reloj y ciclos */
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        break;

                    default: //Instrucción de fin

                        /* Se disminuye la cantidad de hilos activos en el procesador */
                        --hilosActivos;

                        /* Se guarda el valor del reloj porque ya se terminó de ejecutar el hilo. Se guarda el valor del reloj aumentado porque
                        en este punto el hilo principal aún no ha aumentado el valor del reloj. */
                        datosHilos[filaContextoActual, 3] = reloj + 1;

                        /* Se indica que el hilo especificado por la variable filaContextoActual ya no está en ejecucion */
                        ejecucionHilos[filaContextoActual, 1] = 1;
                        break;

                }
            }

            else
            {

            }
        }

        public bool bloquearCacheLW(int direccionMemoria, int numRegistro, int numBloque, int posicionCache, int numDireccionMemComp, bool LW )
        {
            int palabra = 0;
            bool bloqueo = false;
            int numDirectorioCasa = 0;
            int numDirectorioBloqueVictima = 0;
            int posicionMemBloqueVictima = 0;
            int posicionMemBloque = 0;
            int numBloqueVictima = 0;
            // Solicita bloquear la caché
            if (Monitor.TryEnter(cacheDatos))
            {
                try
                {
                    bloqueo = true;
                    numDirectorioCasa = (numBloque / 8) + 1;
                    posicionMemBloque = numBloque * 16;

                    if(datosHilos[filaContextoActual, 4] == 1)
                    {
                        Debug.WriteLine("soy el procesador 1 " + "posicion cache en LW es : " + posicionCache + "direccion a leer es: " + direccionMemoria);

                    }
                    else if (datosHilos[filaContextoActual, 4] == 2)
                    {
                        Debug.WriteLine("soy el procesador 2 " + "posicion cache en LW es : " + posicionCache + "direccion a leer es: " + direccionMemoria);

                    }
                    else
                    {
                        Debug.WriteLine("soy el procesador 3 " + "posicion cache en LW es : " + posicionCache + "direccion a leer es: " + direccionMemoria);
                    }
                 
                 
                    if(LW == false)
                    {
                        banderaLL = true; //Se activa la bandera
                        bloqueLL = numBloque;
                    }

                    //Se verifica si en la caché de datos se ubica el bloque leido
                    if (cacheDatos[4, posicionCache] == numBloque)
                    {
                        //Se verifica si está modificado o compartido
                        if (cacheDatos[5, posicionCache] == modificado || cacheDatos[5, posicionCache] == compartido)
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;

                            if(datosHilos[filaContextoActual, 4] == 1)
                            {
                                Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else if (datosHilos[filaContextoActual, 4] == 2)
                            {
                                Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else
                            {
                                Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }

                 

                            //Se realiza la lectura
                            registros[numRegistro] = cacheDatos[palabra, posicionCache];

                            bloqueo = true;
                        }
                        else //Se encuentra el bloque en la caché de datos pero inválido
                        {
                            if (numBloque <= 7) //Pertenece al procesador 1
                            {
                                if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador1, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;


                                    if (datosHilos[filaContextoActual, 4] == 1)
                                    {
                                        Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else if (datosHilos[filaContextoActual, 4] == 2)
                                    {
                                        Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    //Se realiza la lectura
                                    registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else if (numBloque > 7 && numBloque <= 15) //Pertenece al procesador 2
                            {
                                if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador2, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;


                                    if (datosHilos[filaContextoActual, 4] == 1)
                                    {
                                        Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else if (datosHilos[filaContextoActual, 4] == 2)
                                    {
                                        Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }

                                    //Se realiza la lectura
                                    registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                    bloqueo = true;

                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Pertenece al procesador 3
                            {
                                if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador3, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;


                                    if (datosHilos[filaContextoActual, 4] == 1)
                                    {
                                        Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else if (datosHilos[filaContextoActual, 4] == 2)
                                    {
                                        Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                    }

                                    //Se realiza la lectura
                                    registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                    bloqueo = true;

                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }

                        }
                    }

                    //Se verifica si el bloque víctima está modificado o compartido
                    else if (cacheDatos[5, posicionCache] == modificado || cacheDatos[5, posicionCache] == compartido)
                    {
         
                        numBloqueVictima = cacheDatos[4, posicionCache];
                        numDirectorioBloqueVictima = (numBloqueVictima / 8) + 1;
                        posicionMemBloqueVictima = numBloqueVictima * 16;
                        
                        if(numBloque <= 7)
                        {
                            posicionMemBloque = posicionMemBloque - inicioMemProcesador1;
                        }
                        else if(numBloque <= 15)
                        {
                            posicionMemBloque = posicionMemBloque - inicioMemProcesador2;
                        }
                        else
                        {
                            posicionMemBloque = posicionMemBloque - inicioMemProcesador3;
                        }


                        //El bloque víctima pertenece al procesador 1
                        if (numBloqueVictima <= 7)
                        {
                            //número de procesador, número de directorio, posición en memoria del número de bloque
                            if (solicitarDirectorioDiagrama3_LW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, numBloqueVictima, numBloque, posicionMemBloqueVictima - inicioMemProcesador1, posicionMemBloque, posicionCache))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;


                                if (datosHilos[filaContextoActual, 4] == 1)
                                {
                                    Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else if (datosHilos[filaContextoActual, 4] == 2)
                                {
                                    Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else
                                {
                                    Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                //Se realiza la lectura
                                registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                bloqueo = true;

                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                        //El bloque pertenece al procesador 2
                        else if (numBloqueVictima > 7 && numBloqueVictima <= 15)
                        {
                            //número de procesador, número de directorio, posición en memoria del número de bloque
                            if (solicitarDirectorioDiagrama3_LW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, numBloqueVictima, numBloque, posicionMemBloqueVictima - inicioMemProcesador2, posicionMemBloque, posicionCache))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;


                                if (datosHilos[filaContextoActual, 4] == 1)
                                {
                                    Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else if (datosHilos[filaContextoActual, 4] == 2)
                                {
                                    Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else
                                {
                                    Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }

                                //Se realiza la lectura
                                registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                        //El bloque pertenece al procesador 3
                        else
                        {
                            //número de procesador, número de directorio, posición en memoria del número de bloque
                            if (solicitarDirectorioDiagrama3_LW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, numBloqueVictima, numBloque, posicionMemBloqueVictima - inicioMemProcesador3, posicionMemBloque, posicionCache))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;


                                if (datosHilos[filaContextoActual, 4] == 1)
                                {
                                    Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else if (datosHilos[filaContextoActual, 4] == 2)
                                {
                                    Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }
                                else
                                {
                                    Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                                }

                                //Se realiza la lectura
                                registros[numRegistro] = cacheDatos[palabra, posicionCache];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                    }
                    //En caso de no estar modificado ni compartido, se solicita el directorio correspondiente del bloque que se va a leer

                    else if (numBloque <= 7) //Pertenece al procesador 1
                    {
                        if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador1, posicionCache))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;


                            if (datosHilos[filaContextoActual, 4] == 1)
                            {
                                Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else if (datosHilos[filaContextoActual, 4] == 2)
                            {
                                Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else
                            {
                                Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }

                            //Se realiza la lectura
                            registros[numRegistro] = cacheDatos[palabra, posicionCache];

                            bloqueo = true;
                        }
                        else
                        {
                            bloqueo = false;
                        }
                    }
                    else if (numBloque > 7 && numBloque <= 15) //Pertenece al procesador 2
                    {
                        if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador2, posicionCache))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;


                            if (datosHilos[filaContextoActual, 4] == 1)
                            {
                                Debug.WriteLine("soy el procesador 1 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else if (datosHilos[filaContextoActual, 4] == 2)
                            {
                                Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else
                            {
                                Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }

                            //Se realiza la lectura
                            registros[numRegistro] = cacheDatos[palabra, posicionCache];

                            bloqueo = true;

                        }
                        else
                        {
                            bloqueo = false;
                        }
                    }
                    else //Pertenece al procesador 3
                    {
                        if (solicitarDirectorioDiagrama1_LW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, numDireccionMemComp - inicioMemProcesador3, posicionCache))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;


                            if (datosHilos[filaContextoActual, 4] == 1)
                            {
                                Debug.WriteLine("soy el procesador 1" + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);

                            }
                            else if (datosHilos[filaContextoActual, 4] == 2)
                            {
                                Debug.WriteLine("soy el procesador 2 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);
                            }
                            else
                            {
                                Debug.WriteLine("soy el procesador 3 " + "el numero de palabra es : " + palabra + "el numero de registro es: " + numRegistro);

                            }

                            //Se realiza la lectura
                            registros[numRegistro] = cacheDatos[palabra, posicionCache];

                            bloqueo = true;

                        }
                        else
                        {
                            bloqueo = false;
                        }
                    }

                }
                finally
                {
                    //Se libera la caché.
                    Monitor.Exit(cacheDatos);
                }
            }
            else
            {
                bloqueo = false;
            }

            return bloqueo;
        }

  
        public bool bloquearCacheSW(int direccionMemoria, int numRegistro, int numBloque, int posicionCache, int posicionMemCompartida)
        {
            bool hit = false;
            bool bloqueModificado = false;
            int palabra = 0;
            bool bloqueo = false;
            int numDirectorioCasa = 0;
            int numDirectorioBloqueVictima = 0;
            int numBloqueVictima = 0;
            int posicionMemBloqueVictima = 0;
          
            
            // Solicita bloquear la caché
            if (Monitor.TryEnter(cacheDatos))
            {
                try
                {
                    bloqueo = true;

                    numDirectorioCasa = (numBloque / 8) + 1;


                    if (datosHilos[filaContextoActual, 4] == 1)
                    {
                        Debug.WriteLine("soy el procesador 1 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);

                    }
                    else if (datosHilos[filaContextoActual, 4] == 2)
                    {
                        Debug.WriteLine("soy el procesador 2 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);

                    }
                    else
                    {
                        Debug.WriteLine("soy el procesador 3 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);
                    }


                    if(numBloque <= 7)
                    {
                        posicionMemCompartida = posicionMemCompartida - inicioMemProcesador1;
                    }
                    else if(numBloque <= 15)
                    {
                        posicionMemCompartida = posicionMemCompartida - inicioMemProcesador2;
                    }
                    else
                    {
                        posicionMemCompartida = posicionMemCompartida - inicioMemProcesador3;
                    }

                    //Se verifica si en la caché de datos se ubica el bloque que se escribirá
                    if (cacheDatos[4, posicionCache] == numBloque)
                    {
                        hit = true;
                        //Se revisa si el bloque está modificado
                        if (cacheDatos[5, posicionCache] == modificado)
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;

                            //Se realiza la escritura
                            cacheDatos[palabra, posicionCache] = registros[numRegistro];

                            bloqueo = true;
                        }
                        else if (cacheDatos[5, posicionCache] == compartido) //Se revisa si el bloque entonces está compartido
                        {
                            //Se solicita el directorio correspondiente del bloque de escritura que ya se encuentra en la caché 
                            if (numBloque <= 7) //El bloque pertenece al procesador 1
                            {
    
                                if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la lectura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }

                            }
                            else if (numBloque <= 15) //El bloque pertenece al procesador 2
                            {
                                if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //El bloque pertenece al procesador 3
                            {
                                if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }

                        }
                        else //El bloque se encuentra invalidado y por ende se realiza un fallo 
                        {
                            hit = false;
                            if (numBloque <= 7) //El bloque pertenece al procesador 1
                            {
                                Debug.WriteLine("bloque inválido en fallo de cache load, pertenece al procesador 1");
                                hit = true;
                                if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else if (numBloque <= 15) //El bloque pertenece al procesador 2
                            {
                                hit = false;
                                if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //El bloque pertenece al procesador 3
                            {
                                hit = false;
                                if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                    }
                    //Se verifica si el bloque víctima está modificado o compartido
                    else if (cacheDatos[5, posicionCache] == modificado || cacheDatos[5, posicionCache] == compartido)
                    {
                        numBloqueVictima = cacheDatos[4, posicionCache];
                        numDirectorioBloqueVictima = (numBloqueVictima / 8) + 1;
                        posicionMemBloqueVictima = numBloqueVictima * 16;

                        if(numBloqueVictima <= 7)
                        {
                            posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador1;
                        }
                        else if(numBloqueVictima <= 15 )
                        {
                            posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador2;
                        }
                        else
                        {
                            posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador3;
                        }


                        
                        if (cacheDatos[5, posicionCache] == modificado)
                        {
                            bloqueModificado = true;
                        }
                        else
                        {
                            bloqueModificado = false;
                        }

                        //El bloque pertenece al procesador 1
                        if (cacheDatos[4, posicionCache] <= 7)
                        {

                            //número de procesador, número de directorio, número de bloque
                            if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la escritura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;

                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                        //El bloque pertenece al procesador 2
                        else if (cacheDatos[4, posicionCache] > 7 && cacheDatos[4, posicionCache] <= 15)
                        {
                            //número de procesador, número de directorio, número de bloque
                            if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la lectura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                        //El bloque pertenece al procesador 3
                        else
                        {
                            //número de procesador, número de directorio, número de bloque
                            if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la lectura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }
                    }
                    //En caso de no estar modificado ni compartido, se solicita el directorio correspondiente del bloque que se va a escribir
                    else if (numBloque <= 7) //Pertenece al procesador 1
                    {
                        hit = false;

                        numBloqueVictima = cacheDatos[4, posicionCache];
                        numDirectorioBloqueVictima = (numBloqueVictima / 8) + 1;

                        if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;

                            //Se realiza la lectura
                            cacheDatos[palabra, posicionCache] = registros[numRegistro];

                            bloqueo = true;
                        }
                        else
                        {
                            bloqueo = false;
                        }

                    }
                    else if (numBloque > 7 && numBloque <= 15) //Pertenece al procesador 2
                    {
                        hit = false;
                        if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;

                            //Se realiza la lectura
                            cacheDatos[palabra, posicionCache] = registros[numRegistro];

                            bloqueo = true;
                        }
                        else
                        {
                            bloqueo = false;
                        }

                    }
                    else //Pertenece al procesador 3
                    {
                        hit = false;
                        if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                        {
                            /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                            palabra = direccionMemoria % 16;
                            palabra = palabra / 4;

                            //Se realiza la lectura
                            cacheDatos[palabra, posicionCache] = registros[numRegistro];

                            bloqueo = true;

                        }
                        else
                        {
                            bloqueo = false;
                        }
                    }

                }
                finally
                {
                    //Se libera la caché.
                    Monitor.Exit(cacheDatos);
                }
               
            }
            else
            {
                bloqueo = false;
            }

            return bloqueo;
        }


        public bool bloquearCacheSC(int direccionMemoria, int numRegistro, int numBloque, int posicionCache, int posicionMemCompartida)
        {
            bool hit = false;
            bool bloqueModificado = false;
            int palabra = 0;
            bool bloqueo = false;
            int numDirectorioCasa = 0;
            int numDirectorioBloqueVictima = 0;
            int numBloqueVictima = 0;
            int posicionMemBloqueVictima = 0;


            // Solicita bloquear la caché

            if(hilosRL[filaContextoActual] == direccionMemoria)
            {
                if (Monitor.TryEnter(cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        numDirectorioCasa = (numBloque / 8) + 1;


                        if (datosHilos[filaContextoActual, 4] == 1)
                        {
                            Debug.WriteLine("soy el procesador 1 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);

                        }
                        else if (datosHilos[filaContextoActual, 4] == 2)
                        {
                            Debug.WriteLine("soy el procesador 2 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);

                        }
                        else
                        {
                            Debug.WriteLine("soy el procesador 3 " + "posicion cache en SW es : " + posicionCache + "direccion a escribir es: " + direccionMemoria);
                        }


                        if (numBloque <= 7)
                        {
                            posicionMemCompartida = posicionMemCompartida - inicioMemProcesador1;
                        }
                        else if (numBloque <= 15)
                        {
                            posicionMemCompartida = posicionMemCompartida - inicioMemProcesador2;
                        }
                        else
                        {
                            posicionMemCompartida = posicionMemCompartida - inicioMemProcesador3;
                        }

                        //Se verifica si en la caché de datos se ubica el bloque que se escribirá
                        if (cacheDatos[4, posicionCache] == numBloque)
                        {
                            hit = true;
                            //Se revisa si el bloque está modificado
                            if (cacheDatos[5, posicionCache] == modificado)
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la escritura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;
                            }
                            else if (cacheDatos[5, posicionCache] == compartido) //Se revisa si el bloque entonces está compartido
                            {
                                //Se solicita el directorio correspondiente del bloque de escritura que ya se encuentra en la caché 
                                if (numBloque <= 7) //El bloque pertenece al procesador 1
                                {

                                    if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la lectura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }

                                }
                                else if (numBloque <= 15) //El bloque pertenece al procesador 2
                                {
                                    if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la escritura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }
                                else //El bloque pertenece al procesador 3
                                {
                                    if (solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionCache))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la escritura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }

                            }
                            else //El bloque se encuentra invalidado y por ende se realiza un fallo 
                            {
                                hit = false;
                                if (numBloque <= 7) //El bloque pertenece al procesador 1
                                {
                                    Debug.WriteLine("bloque inválido en fallo de cache load, pertenece al procesador 1");
                                    hit = true;
                                    if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la escritura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }
                                else if (numBloque <= 15) //El bloque pertenece al procesador 2
                                {
                                    hit = false;
                                    if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la escritura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }
                                else //El bloque pertenece al procesador 3
                                {
                                    hit = false;
                                    if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                                    {
                                        /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                        palabra = direccionMemoria % 16;
                                        palabra = palabra / 4;

                                        //Se realiza la escritura
                                        cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }
                            }
                        }
                        //Se verifica si el bloque víctima está modificado o compartido
                        else if (cacheDatos[5, posicionCache] == modificado || cacheDatos[5, posicionCache] == compartido)
                        {
                            numBloqueVictima = cacheDatos[4, posicionCache];
                            numDirectorioBloqueVictima = (numBloqueVictima / 8) + 1;
                            posicionMemBloqueVictima = numBloqueVictima * 16;

                            if (numBloqueVictima <= 7)
                            {
                                posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador1;
                            }
                            else if (numBloqueVictima <= 15)
                            {
                                posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador2;
                            }
                            else
                            {
                                posicionMemBloqueVictima = posicionMemBloqueVictima - inicioMemProcesador3;
                            }



                            if (cacheDatos[5, posicionCache] == modificado)
                            {
                                bloqueModificado = true;
                            }
                            else
                            {
                                bloqueModificado = false;
                            }

                            //El bloque pertenece al procesador 1
                            if (cacheDatos[4, posicionCache] <= 7)
                            {

                                //número de procesador, número de directorio, número de bloque
                                if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la escritura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;

                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            //El bloque pertenece al procesador 2
                            else if (cacheDatos[4, posicionCache] > 7 && cacheDatos[4, posicionCache] <= 15)
                            {
                                //número de procesador, número de directorio, número de bloque
                                if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la lectura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            //El bloque pertenece al procesador 3
                            else
                            {
                                //número de procesador, número de directorio, número de bloque
                                if (solicitarDirectorioBloqueVictima_SW(datosHilos[filaContextoActual, 4], numDirectorioBloqueVictima, numDirectorioCasa, posicionMemBloqueVictima, posicionMemCompartida, numBloqueVictima, numBloque, posicionCache, bloqueModificado))
                                {
                                    /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                    palabra = direccionMemoria % 16;
                                    palabra = palabra / 4;

                                    //Se realiza la lectura
                                    cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        //En caso de no estar modificado ni compartido, se solicita el directorio correspondiente del bloque que se va a escribir
                        else if (numBloque <= 7) //Pertenece al procesador 1
                        {
                            hit = false;

                            numBloqueVictima = cacheDatos[4, posicionCache];
                            numDirectorioBloqueVictima = (numBloqueVictima / 8) + 1;

                            if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la lectura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }

                        }
                        else if (numBloque > 7 && numBloque <= 15) //Pertenece al procesador 2
                        {
                            hit = false;
                            if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la lectura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;
                            }
                            else
                            {
                                bloqueo = false;
                            }

                        }
                        else //Pertenece al procesador 3
                        {
                            hit = false;
                            if (solicitarDirectorioFallo_SW(datosHilos[filaContextoActual, 4], numDirectorioCasa, numBloque, posicionMemCompartida, posicionCache, hit))
                            {
                                /*Calcula la palabra a leer de acuerdo a la dirección de memoria*/
                                palabra = direccionMemoria % 16;
                                palabra = palabra / 4;

                                //Se realiza la lectura
                                cacheDatos[palabra, posicionCache] = registros[numRegistro];

                                bloqueo = true;

                            }
                            else
                            {
                                bloqueo = false;
                            }
                        }

                    }
                    finally
                    {
                        //Se libera la caché.
                        Monitor.Exit(cacheDatos);
                    }

                }
                else
                {
                    bloqueo = false;
                }
            }
            else
            {
                hilosRL[filaContextoActual] = 0; //Se coloca un 0 para indicar que no se pudo llevar a cabo
                banderaLL = false;
                bloqueo = false;
            }
           
            return bloqueo;
        }


        /*Método que copia desde la memoria el contenido del bloque a la caché de datos */
        public void copiarBloqueDesdeMemoria(int[] memCompartida, int posicionMemoria, int posicionCache, int numBloque, int tipoOperacion, bool memLocal)
        {

            //Copio en la caché de datos el bloque que se escribirá 
            int contador = posicionMemoria;
            Debug.WriteLine("valor de memoria es: " + contador);
            for (int i = 0; i < 4; ++i)
            {
          
                cacheDatos[i, posicionCache] = memCompartida[contador];
                ++contador;
            }

            //Se establece el número del bloque en la caché de datos 
            cacheDatos[4, posicionCache] = numBloque;

            //1 significa store
            if (tipoOperacion == 1)
            {
                //Se establece el estado del bloque en la caché de datos 
                cacheDatos[5, posicionCache] = modificado;
            }
            //0 significa load
            else if (tipoOperacion == 0)
            {
                //Se establece el estado del bloque en la caché de datos 
                cacheDatos[5, posicionCache] = compartido;
            }

            if(memLocal)
            {
                //For de 16 ciclos para simular lo que se tarda en copiar desde mememoria a caché
                for (int i = 0; i < 16; ++i)
                {
                    barreraFinInstr.SignalAndWait();
                    barreraCambioReloj_Ciclo.SignalAndWait();
                }
            }
            else
            {
                //For de 32 ciclos para simular lo que se tarda en copiar desde mememoria a caché
                for (int i = 0; i < 32; ++i)
                {
                    barreraFinInstr.SignalAndWait();
                    barreraCambioReloj_Ciclo.SignalAndWait();
                }
            }
        
            
        }

        //Método para solicitar el directorio en caso de que se produzca un fallo
        public bool solicitarDirectorioFallo_SW(int numProcesadorLocal, int numDirectorio, int numBloque, int posicionMemoriaCompartida, int posicionCache, bool hit)
        {
            bool bloqueo = false;
            int posicionBloqueDirectorio = 0;
            int contadorCaches = 0;
            int contadorCachesSolicitadas = 0;
            int[] datosBloqueModificado = new int[4];
            int posicionProcesador = 0;
            bool memLocal = false;

            if(numBloque <= 7)
            {
                posicionBloqueDirectorio = numBloque;
            }
            else if(numBloque <= 15)
            {
                posicionBloqueDirectorio = numBloque - 8;
            }
            else
            {
                posicionBloqueDirectorio = numBloque - 16;
            }

            Debug.WriteLine("soy el procesador " + numProcesadorLocal);
            Debug.WriteLine("ESTOY EN SOLICITAR DIRECTORIO FALLOOOOOOOO");
   
            if (numProcesadorLocal == numDirectorio) 
            {
                    if (Monitor.TryEnter(directorio))
                    {
                        try
                        {
                            bloqueo = true;


                            //For de 2 ciclo para simular lo que se tarda en acceder a directorio local
                            for (int i = 0; i < 2; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            if (directorio[posicionBloqueDirectorio, 1] == compartido) //El bloque que se escribirá se encuentra compartido 
                            {
                                contadorCaches = 0;
                                contadorCachesSolicitadas = 0;
                                for (int i = 2; i < columnasDirectorio; ++i)
                                {
                                    if (directorio[posicionBloqueDirectorio, i] == 1)
                                    {
                                        ++contadorCaches;

                                        //Se solicita la caché correspondiente para invalidar el bloque
                                        if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                                        {
                                            //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                            directorio[posicionBloqueDirectorio, i] = 0;
                                            ++contadorCachesSolicitadas;
                                        }
                                        else
                                        {
                                            //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                            bloqueo = false;
                                        }
                                        
                                    }
                                }

                                //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                                if (contadorCaches == contadorCachesSolicitadas)
                                {
                                    memLocal = true;

                                    //Se copia desde la memoria el contenido del bloque a la caché de datos
                                    copiarBloqueDesdeMemoria(memoriaCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 1, memLocal);

                                    //Se actualiza el estado del bloque en el directorio
                                    directorio[posicionBloqueDirectorio, 1] = modificado;

                                    //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                    directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    bloqueo = true;

                                    //Realizo la escritura
                                }
                                else
                                {
                                    bloqueo = false;
                                }

                            }
                            else if (directorio[posicionBloqueDirectorio, 1] == modificado) //El bloque que se escribirá se encuentra modificado
                            {
                                datosBloqueModificado = bloqueModificado(directorio, posicionBloqueDirectorio);

                                if(datosBloqueModificado[0] == 1) //Se verifica que realmente el bloque está modificado por una caché de datos
                                {
                                    if (solicitarCacheExterna_BloqueModificado_Diagrama3_SW(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], memoriaCompartida, numBloque, posicionMemoriaCompartida, posicionCache, hit, memLocal))
                                    {
                                        //Se invalida la entrada en el directorio del procesador que tenía el bloque
                                        directorio[posicionBloqueDirectorio, datosBloqueModificado[1]+1] = 0;

                                        //Se mantiene el estado del bloque 

                                        //Se actualiza la entrada en el directorio del procesador que ahora tiene el bloque modificado
                                        directorio[posicionBloqueDirectorio, numProcesadorLocal+1] = 1;

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }                     
                            }
                            else //El bloque no se encuentra ni compartido ni modificado
                            {
                                memLocal = true;

                                //Se copia desde la memoria el contenido del bloque a la caché de datos
                                copiarBloqueDesdeMemoria(memoriaCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 1, memLocal);

                                //Se actualiza el estado del bloque en el directorio
                                directorio[posicionBloqueDirectorio, 1] = modificado;

                                //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                bloqueo = true;

                                //Realizo la escritura
                            }
                        }
                        finally
                        {
                            Monitor.Exit(directorio);
                        }
                    }
                    else
                    {
                        bloqueo = false;
                    }
                }                
                else 
                {
                        if (numProcesadorLocal == 1) //Se está ejecutando el procesador 1
                        {
                            posicionProcesador = numDirectorio-2;
                        }
                        else if(numProcesadorLocal == 2) //Se está ejecutando el procesador 2
                        {
                            if(numDirectorio == 1)
                            {
                                 posicionProcesador = 0;
                            }
                            else
                            {
                                posicionProcesador = 1;
                            }
                   
                        }
                        else
                        {
                            posicionProcesador = numDirectorio-1;
                        }

                    if (Monitor.TryEnter(procesadores.ElementAt(posicionProcesador).directorio))
                    {
                        try
                        {
                            bloqueo = true;

                            //For de 4 ciclos para simular lo que se tarda en acceder a directorio remoto
                            for (int i = 0; i < 4; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            if (procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] == compartido) //El bloque que se escribirá se encuentra compartido 
                            {
                                contadorCaches = 0;
                                contadorCachesSolicitadas = 0;
                                for (int i = 2; i < columnasDirectorio; ++i)
                                {
                                    if (procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, i] == 1)
                                    {
                                        ++contadorCaches;

                                        //Se solicita la caché correspondiente para invalidar el bloque
                                        if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                                        {
                                            //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                            procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, i] = 0;
                                            ++contadorCachesSolicitadas;
                                        }
                                        else
                                        {
                                            //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                        }

                                    }
                                }

                                //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                                if (contadorCaches == contadorCachesSolicitadas)
                                {
                                    memLocal = false;
                                    //Se copia desde la memoria el contenido del bloque a la caché de datos
                                    copiarBloqueDesdeMemoria(procesadores.ElementAt(posicionProcesador).memoriaCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 1, memLocal);

                                    //Se actualiza el estado del bloque en el directorio
                                    procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] = modificado;

                                    //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                    procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    bloqueo = true;

                                    //Realizo la escritura
                                }
                                else
                                {
                                    bloqueo = false;
                                }

                            }
                            else if (procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] == modificado) //El bloque que se escribirá está modificado
                            {
                                datosBloqueModificado = bloqueModificado(procesadores.ElementAt(posicionProcesador).directorio, posicionBloqueDirectorio);

                                if (datosBloqueModificado[0] == 1) //Se verifica que realmente el bloque está modificado por una caché de datos
                                {
                                    if (solicitarCacheExterna_BloqueModificado_Diagrama3_SW(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], procesadores.ElementAt(posicionProcesador).memoriaCompartida, numBloque, posicionMemoriaCompartida, posicionCache, hit, memLocal))
                                    {
                                        //Se invalida la entrada en el directorio del procesador que tenía el bloque
                                        directorio[posicionBloqueDirectorio, datosBloqueModificado[1] + 1] = 0;

                                        //Se mantiene el estado del bloque 

                                        //Se actualiza la entrada en el directorio del procesador que ahora tiene el bloque modificado
                                        directorio[posicionBloqueDirectorio, numProcesadorLocal+1] = 1;

                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                                }            
                            }
                            else //El bloque no se encuentra ni compartido ni modificado
                            {
                                memLocal = false;

                                //Se copia desde la memoria el contenido del bloque a la caché de datos
                                copiarBloqueDesdeMemoria(procesadores.ElementAt(posicionProcesador).memoriaCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 1, memLocal);

                                //Se actualiza el estado del bloque en el directorio
                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] = modificado;

                                //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                bloqueo = true;

                                //Realizo la escritura
                            }
                        }
                        finally
                        {
                            Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                        }
                    }
                    else
                    {
                        bloqueo = false;
                    }
                }
          
            return bloqueo;
        }

        /*Método para realizar la verificacion del estado del bloque en el directorio a subir a la caché de datos */

        public bool verificarEstadoBloque_FalloCacheSW(int numProcesadorLocal, int[,] dir, int[] memCompartida, int numBloque, int posicionBloqueDirectorio, int posicionMemBloque, int posicionCache, bool dirSolicitado, bool memLocal, bool dirLocal)
        {
            bool bloqueo = false;
            int[] datosBloqueModificado = new int[4];
            bool hit = false;
            int contadorCaches = 0;
            int contadorCachesSolicitadas = 0;

            if(dirSolicitado)
            {
                bloqueo = true;

                //Se verifica en el directorio si alguna caché lo tiene modificado
                datosBloqueModificado = bloqueModificado(dir, posicionBloqueDirectorio);

                if (datosBloqueModificado[0] == 1) //Se verifica si el bloque se encuentra modificado
                {
                    hit = false;
                    if (solicitarCacheExterna_BloqueModificado_Diagrama3_SW(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], memCompartida, numBloque, posicionMemBloque, posicionCache, hit, memLocal))
                    {
                        //Se actualiza la entrada en el directorio correspondiente al procesador que tenía el bloque modificado
                        dir[posicionBloqueDirectorio, datosBloqueModificado[1] + 1] = 0;

                        //Se actualiza el estado del bloque en el directorio
                        dir[posicionBloqueDirectorio, 1] = modificado;

                        //Se actualiza la entrada en el directorio correspondiente al procesador que ahora posee el bloque
                        dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                        //Se realiza la lectura

                        bloqueo = true;
                    }
                    else
                    {
                        bloqueo = false;
                    }
                }
                else if (dir[posicionBloqueDirectorio, 1] == compartido) //Se verifica si alguna caché lo tiene compartido
                {
                    contadorCaches = 0;
                    contadorCachesSolicitadas = 0;
                    Debug.WriteLine("la posicion del bloque es: " + posicionBloqueDirectorio + " el num del bloque a escribir es: " + numBloque);
                    for (int i = 2; i < columnasDirectorio; ++i)
                    {
                        if (dir[posicionBloqueDirectorio, i] == 1)
                        {
                            ++contadorCaches;

                            //Se solicita la caché correspondiente para invalidar el bloque
                            if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                            {
                                //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                dir[posicionBloqueDirectorio, i] = 0;
                                ++contadorCachesSolicitadas;
                            }
                            else
                            {
                                //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                bloqueo = false;
                            }
                        }
                    }

                    //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                    if (contadorCaches == contadorCachesSolicitadas)
                    {

                       copiarBloqueDesdeMemoria(memCompartida, posicionMemBloque, posicionCache, numBloque, 1, memLocal);

                        //Se actualiza la entrada en el directorio correspondiente al procesador que posee el bloque
                        dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                        //Se actualiza el estado del bloque en el directorio a modificado
                        dir[posicionBloqueDirectorio, 1] = modificado;

                    }

                }
                else //Ninguna caché tenía el bloque modificado o compartido, se realiza la lectura del bloque desde la memoria
                {
                    copiarBloqueDesdeMemoria(memCompartida, posicionMemBloque, posicionCache, numBloque, 1, memLocal);

                    //Se actualiza la entrada en el directorio correspondiente al procesador que posee el bloque
                    dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                    //Se actualiza el estado del bloque en el directorio a modificado
                    dir[posicionBloqueDirectorio, 1] = modificado;
                }
            }
            else
            {
                
                if (Monitor.TryEnter(dir))
                {
                        try
                        {
                            bloqueo = true;

                            if(dirLocal)
                            {
                                //For de 2 ciclos para simular lo que se tarda en acceder a directorio local
                                for (int i = 0; i < 2; ++i)
                                {
                                    barreraFinInstr.SignalAndWait();
                                    barreraCambioReloj_Ciclo.SignalAndWait();
                                }
                            }
                            else
                            {
                                //For de 4 ciclos para simular lo que se tarda en acceder a directorio local
                                for (int i = 0; i < 4; ++i)
                                {
                                    barreraFinInstr.SignalAndWait();
                                    barreraCambioReloj_Ciclo.SignalAndWait();
                                }
                            }

                            //Se verifica en el directorio si alguna caché lo tiene modificado
                            datosBloqueModificado = bloqueModificado(dir, posicionBloqueDirectorio);

                            if (datosBloqueModificado[0] == 1) //Se verifica si el bloque se encuentra modificado
                            {
                                hit = false;
                                if (solicitarCacheExterna_BloqueModificado_Diagrama3_SW(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], memCompartida, numBloque, posicionMemBloque, posicionCache, hit, memLocal))
                                {
                                    //Se actualiza la entrada en el directorio correspondiente al procesador que tenía el bloque modificado
                                    dir[posicionBloqueDirectorio, datosBloqueModificado[1] + 1] = 0;

                                    //Se actualiza el estado del bloque en el directorio
                                    dir[posicionBloqueDirectorio, 1] = modificado;

                                    //Se actualiza la entrada en el directorio correspondiente al procesador que ahora posee el bloque
                                    dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = modificado;

                                    //Se realiza la lectura
                                    bloqueo = true;

                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else if (dir[posicionBloqueDirectorio, 1] == compartido) //Se verifica si alguna caché lo tiene compartido
                            {
                                contadorCaches = 0;
                                contadorCachesSolicitadas = 0;

                                for (int m = 0; m < columnasDirectorio; ++ m )
                                {
                                    Debug.WriteLine("CONTENIDO EN EL DIRECTORIO ES " + dir[posicionBloqueDirectorio, m]);
                                }

                                    for (int i = 2; i < columnasDirectorio; ++i)
                                    {
                                        if (dir[posicionBloqueDirectorio, i] == 1)
                                        {
                                            ++contadorCaches;
                                            Debug.WriteLine("la posicion del bloque es: " + posicionBloqueDirectorio + " el num del bloque a escribir es: " + numBloque);

                                            //Se solicita la caché correspondiente para invalidar el bloque
                                            if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                                            {
                                                //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                                dir[posicionBloqueDirectorio, i] = 0;
                                                ++contadorCachesSolicitadas;
                                            }
                                            else
                                            {
                                                //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                                bloqueo = false;
                                            }
                                        }
                                    }

                                //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                                if (contadorCaches == contadorCachesSolicitadas)
                                {

                                    copiarBloqueDesdeMemoria(memCompartida, posicionMemBloque, posicionCache, numBloque, 1, memLocal);

                                    //Se actualiza la entrada en el directorio correspondiente al procesador que posee el bloque
                                    dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    //Se actualiza el estado del bloque en el directorio a modificado
                                    dir[posicionBloqueDirectorio, 1] = modificado;

                                }

                            }
                            else //Ninguna caché tenía el bloque modificado o compartido, se realiza la lectura del bloque desde la memoria
                            {
                                copiarBloqueDesdeMemoria(memCompartida, posicionMemBloque, posicionCache, numBloque, 1, memLocal);

                                //Se actualiza la entrada en el directorio correspondiente al procesador que posee el bloque
                                dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                //Se actualiza el estado del bloque en el directorio a modificado
                                dir[posicionBloqueDirectorio, 1] = modificado;
                            }
                        }
                        finally
                        {
                            Monitor.Exit(dir);
                        }
                    }
                    else
                    {
                        bloqueo = false;
                    }
            }


            return bloqueo;
         
        }


        //Método para solicitar el directorio de un bloque víctima
        public bool solicitarDirectorioBloqueVictima_SW(int numProcesadorLocal, int numDirectorioBloqueVictima, int numDirectorioBloqueCasa, int posicionMemCompartidaBloqueVictima, int posicionMemBloqueCasa, int numBloqueVictima, int numBloque, int posicionCache, bool bloque_Modificado)
        {
            bool bloqueo = false;
            int posicionBloqueDirectorioCasa = 0;
            int posicionBloqueDirectorioVictima = 0;
            int[] datosBloqueModificado = new int[4];
            int contador = 0;
            int posicionProcesador = 0;
            int posicionProcesadorDir = 0;
            bool directorioSolicitado = false;
            bool liberarDirectorio = true;
            bool memLocal = false;
            bool dirLocal = false;

            if(numBloqueVictima <= 7)
            {
                posicionBloqueDirectorioVictima = numBloqueVictima;
            }
            else if(numBloqueVictima <= 15)
            {
                posicionBloqueDirectorioVictima = numBloqueVictima - 8;
            }
            else
            {
                posicionBloqueDirectorioVictima = numBloqueVictima - 16;
            }

            if (numBloque <= 7)
            {
                posicionBloqueDirectorioCasa = numBloque;
            }
            else if (numBloque <= 15)
            {
                posicionBloqueDirectorioCasa = numBloque - 8;
            }
            else
            {
                posicionBloqueDirectorioCasa = numBloque - 16;
            }



            if (numProcesadorLocal == numDirectorioBloqueVictima) 
            {

                if (Monitor.TryEnter(directorio))
                {
                    try
                    {
                        bloqueo = true;

                        //For de 2 ciclos para simular lo que se tarda en acceder en acceder a directorio local
                        for (int i = 0; i < 2; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        if (bloque_Modificado) //El bloque víctima se encuentra modificado
                        {
                            //Se obtiene los datos del bloque víctima modificado
                            datosBloqueModificado = bloqueModificado(directorio, posicionBloqueDirectorioVictima);
                            if (datosBloqueModificado[0] == 1) //Se verifica que realmente el bloque víctima se encuentra modificado de acuerdo al directorio
                            {

                                //For de 16 ciclos para simular lo que se tarda en acceder en escribir desde caché a memoria
                                for (int i = 0; i < 16; ++i)
                                {
                                    barreraFinInstr.SignalAndWait();
                                    barreraCambioReloj_Ciclo.SignalAndWait();
                                }

                                //Se copia el contenido del bloque ubicado en la caché de datos a la memoria
                                contador = posicionMemCompartidaBloqueVictima;

                                for (int i = 0; i < 4; ++i)
                                {
                                    memoriaCompartida[contador] = cacheDatos[i, posicionCache];
                                    ++contador;
                                }

                                //Se invalida el estado del bloque en la caché de datos
                                cacheDatos[5, posicionCache] = invalido;

                                //Se actualiza el estado del procesador que tenía el bloque víctima en el directorio
                                directorio[posicionBloqueDirectorioVictima, numDirectorioBloqueVictima+1] = 0;

                                //Se actualiza el estado del bloque en el directorio
                                directorio[posicionBloqueDirectorioVictima, 1] = uncached;
                            }
                        }
                        else //Bloque víctima se encuentra compartido
                        {
                            //Actualizo la entrada sobre el procesador que tenía el bloque compartido en el directorio
                            directorio[posicionBloqueDirectorioVictima, numDirectorioBloqueVictima + 1] = 0;

                            //En caso de que ninguna otra caché lo tenga compartido o modificado, entonces se pone en uncached
                            if (bloqueLibre(directorio, posicionBloqueDirectorioVictima))
                            {
                                directorio[posicionBloqueDirectorioVictima, 1] = uncached;
                            }

                            //Se invalida el estado del bloque víctima en la caché de datos
                            cacheDatos[5, posicionCache] = invalido;
                        }

                        //Se verifica si el bloque que se va a escribir se encuentra en el mismo directorio
                        if (numProcesadorLocal == 1)
                        {
                            if (numBloque <= 7) //El bloque que se escribirá pertenece al procesador 1
                            {
                                directorioSolicitado = true;
                                liberarDirectorio = true;

                                for (int i = 0; i < columnasDirectorio; ++i )
                                {
                                    Debug.WriteLine("DIRECTORIO ES: " + directorio[posicionBloqueDirectorioCasa, i]);
                                }

                                memLocal = true;
                                dirLocal = true;
                                    if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, directorio, memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                    {
                                        bloqueo = true;
                                    }
                                    else
                                    {
                                        bloqueo = false;
                                    }
                            }
                            else //Bloque a escribir pertenece al segundo o al tercer procesador
                            {
                                if (numBloque <= 15) //El bloque a escribir pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    if (numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 1;
                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if(numProcesadorLocal == 2)
                        {
                            if (numBloque > 7 && numBloque <= 15) //El bloque que se escribirá pertenece al procesador 2
                            {
                                if (numDirectorioBloqueVictima == 2)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }

                                memLocal = true;
                                dirLocal = true;

                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, directorio, memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al tercer procesador
                            {
                                if (numBloque <= 7) //El bloque a escribir pertenece al primer procesador
                                {
                                    if (numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    if (numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 1;
                                   
                                }
                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if(numProcesadorLocal == 3)
                        {
                            if (numBloque > 15) //El bloque que se escribirá pertenece al procesador 3
                            {
                                if (numDirectorioBloqueVictima == 3)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }

                                memLocal = true;
                                dirLocal = true;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, directorio, memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al segundo procesador
                            {
                                if (numBloque <= 7) //El bloque a escribir pertenece al primer procesador
                                {
                                    if (numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 1;
                                }

                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                    }

                    finally
                    {
                        if(liberarDirectorio)
                        {
                            Monitor.Exit(directorio);
                        }
                       
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            else 
            {
                if (numProcesadorLocal == 1) //Se está ejecutando el procesador 1
                {
                    posicionProcesador = numDirectorioBloqueCasa - 2;
                }
                else if (numProcesadorLocal == 2) //Se está ejecutando el procesador 2
                {
                    if (numDirectorioBloqueCasa == 1)
                    {
                        posicionProcesador = 0;
                    }
                    else 
                    {
                        posicionProcesador = 1;
                    }

                }
                else //Se está ejecutando el procesador 3
                {
                    posicionProcesador = numDirectorioBloqueCasa - 1;
                }

                if (Monitor.TryEnter(procesadores.ElementAt(posicionProcesador).directorio))
                {
                    try
                    {
                        bloqueo = true;

                        //For de 4 ciclos para simular lo que se tarda en acceder a directorio remoto
                        for (int i = 0; i < 4; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        if (bloque_Modificado) //El bloque víctima se encuentra modificado
                        {
                            //Se obtiene los datos del bloque víctima modificado
                            datosBloqueModificado = bloqueModificado(procesadores.ElementAt(posicionProcesador).directorio, posicionBloqueDirectorioVictima);
                            if (datosBloqueModificado[0] == 1) //Se verifica que realmente el bloque víctima se encuentra modificado de acuerdo al directorio
                            {
                                //For de 32 ciclos para simular lo que se tarda en acceder en escribir desde caché a memoria remota
                                for (int i = 0; i < 32; ++i)
                                {
                                    barreraFinInstr.SignalAndWait();
                                    barreraCambioReloj_Ciclo.SignalAndWait();
                                }


                                //Se copia el contenido del bloque ubicado en la caché de datos a la memoria
                                contador = posicionMemCompartidaBloqueVictima;
                                for (int i = 0; i < 4; ++i)
                                {
                                    procesadores.ElementAt(posicionProcesador).memoriaCompartida[contador] = cacheDatos[i, posicionCache];
                                    ++contador;
                                }

                                //Se invalida el estado del bloque en la caché de datos
                                cacheDatos[5, posicionCache] = invalido;

                                //Se actualiza el estado del procesador que tenía el bloque víctima en el directorio
                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorioVictima, numProcesadorLocal + 1] = 0;

                                //Se actualiza el estado del bloque en el directorio
                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorioVictima, 1] = uncached;
                            }
                        }
                        else //Bloque víctima se encuentra compartido
                        {
                            //Actualizo la entrada sobre el procesador que tenía el bloque compartido en el directorio
                            procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorioVictima, numProcesadorLocal+1] = 0;

                            //En caso de que ninguna otra caché lo tenga compartido o modificado, entonces se pone en uncached
                            if (bloqueLibre(procesadores.ElementAt(posicionProcesador).directorio, posicionBloqueDirectorioVictima))
                            {
                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorioVictima, 1] = uncached;
                            }

                            //Se invalida el estado del bloque víctima en la caché de datos
                            cacheDatos[5, posicionCache] = invalido;
                        }


                        //Se verifica si el bloque que se va a escribir se encuentra en el mismo directorio
                        if (numProcesadorLocal == 1)
                        {
                         
                            if (numBloque <= 7) //El bloque que se escribirá pertenece al procesador 1
                            {
                                if (numDirectorioBloqueVictima == 1)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    directorioSolicitado = false;

                                    //Se libera el directorio
                                    Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                    liberarDirectorio = false;

                                }




                                for (int i = 0; i < columnasDirectorio; ++i)
                                {
                                    Debug.WriteLine("DIRECTORIO ES: " + directorio[posicionBloqueDirectorioCasa, i]);
                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                                                    
                            else //Bloque a escribir pertenece al segundo o al tercer procesador
                            {
                                if (numBloque <= 15) //El bloque a escribir pertenece al segundo procesador
                                {
                                    if(numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        directorioSolicitado = false;
                                        //Se libera el directorio
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    if(numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        directorioSolicitado = false;

                                        //Se libera el directorio
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        liberarDirectorio = false;
                                    }
                                    posicionProcesadorDir = 1;

                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if (numProcesadorLocal == 2)
                        {
                            if (numBloque > 7 && numBloque <= 15) //El bloque que se escribirá pertenece al procesador 2
                            {
                                if(numDirectorioBloqueVictima == 2)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }




                                for (int i = 0; i < columnasDirectorio; ++i)
                                {
                                    Debug.WriteLine("DIRECTORIO ES: " + directorio[posicionBloqueDirectorioCasa, i]);
                                }

                                memLocal = true;
                                dirLocal = true;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, directorio, memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al tercer procesador
                            {
                                if (numBloque <= 7) //El bloque a escribir pertenece al primer procesador
                                {
                                    if(numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {

                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;

                                    }

                                    posicionProcesadorDir = 0;
                             
                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    if(numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 1;

                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if (numProcesadorLocal == 3)
                        {
                            if (numBloque > 15) //El bloque que se escribirá pertenece al procesador 3
                            {
                                if (numDirectorioBloqueVictima == 3)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }

                                memLocal = true;
                                dirLocal = true;

                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, directorio, memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al segundo procesador
                            {
                                if (numBloque <= 7) //El bloque a escribir pertenece al primer procesador
                                {
                                    if (numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }
                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }
                                    posicionProcesadorDir = 1;
                                }

                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_FalloCacheSW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, numBloque, posicionBloqueDirectorioCasa, posicionMemBloqueCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                    }

                    finally
                    {
                        if (liberarDirectorio)
                        {
                            Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                        }
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            return bloqueo;
        }

        //Método para solicitar el directorio en caso de que se produzca un hit y el bloque se encuentre compartido
        public bool solicitarDirectorioHit_BloqueCompartidoDiagrama4_SW(int numProcesadorLocal, int numDirectorio, int numBloque, int posicionCache)
        {
            Debug.WriteLine("ESTOY EN SOLICITAR DIRECTORIO HIT");

            bool bloqueo = false;
            int posicionBloqueDirectorio = 0;
            int contadorCaches = 0;
            int contadorCachesSolicitadas = 0;
            int[] datosBloqueModificado = new int[4];
            int posicionProcesador = 0;


            if(numBloque <= 7)
            {
                posicionBloqueDirectorio = numBloque;
            }
            else if( numBloque <= 15)
            {
                posicionBloqueDirectorio = numBloque - 8;
            }
            else
            {
                posicionBloqueDirectorio = numBloque - 16;
            }

            if (numProcesadorLocal == numDirectorio) 
            {
                    if (Monitor.TryEnter(directorio))
                    {
                        try
                        {
                            bloqueo = true;

                            //For de 2 ciclos para simular lo que se tarda en acceder a directorio local
                            for (int i = 0; i < 2; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            if (directorio[posicionBloqueDirectorio, 1] == compartido) //El bloque que se escribirá se encuentra compartido 
                            {
                                contadorCaches = 0;
                                contadorCachesSolicitadas = 0;
                                for (int i = 2; i < columnasDirectorio; ++i)
                                {
                                    if (directorio[posicionBloqueDirectorio, i] == 1)
                                    {
                                        ++contadorCaches;

                                        if (numProcesadorLocal == i-1) //El bloque que se encuentra compartido pertenece al directorio local
                                        {
                                            cacheDatos[5, posicionCache] = modificado;
                                            directorio[posicionBloqueDirectorio, i] = 0;
                                            ++contadorCachesSolicitadas;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("ENTRE AL ELSE DE DIRECTORIO HIT");

                                            //Se solicita la caché correspondiente para invalidar el bloque
                                            if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                                            {
                                                //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                                directorio[posicionBloqueDirectorio, i] = 0;
                                                ++contadorCachesSolicitadas;
                                            }
                                            else
                                            {
                                                //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                                bloqueo = false;
                                            }
                                        }
                                    }
                                }

                                //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                                if (contadorCaches == contadorCachesSolicitadas)
                                {
                                    //Se actualiza el estado del bloque en el directorio
                                    directorio[posicionBloqueDirectorio, 1] = modificado;

                                    //Se actualiza el estado del bloque en la caché local
                                    cacheDatos[5, posicionCache] = modificado;

                                    //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                    directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }

                            }
                        }
                        finally
                        {
                            Monitor.Exit(directorio);
                        }
                    }
                    else
                    {
                        bloqueo = false;
                    }
            }
            else
            {
                if (numProcesadorLocal == 1) //Se está ejecutando el procesador 1
                {
                    posicionProcesador = numDirectorio - 2;
                }
                else if (numProcesadorLocal == 2) //Se está ejecutando el procesador 2
                {
                    if (numDirectorio == 1)
                    {
                        posicionProcesador = 0;
                    }
                    else
                    {
                        posicionProcesador = 1;
                    }

                }
                else //Se está ejecutando el procesador 3
                {
                    posicionProcesador = numDirectorio - 1;
                }

                    if (Monitor.TryEnter(procesadores.ElementAt(posicionProcesador).directorio))
                    {
                        try
                        {
                            bloqueo = true;

                            //For de 4 ciclos para simular lo que se tarda en acceder a directorio remoto
                            for (int i = 0; i < 4; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            if (procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] == compartido) //El bloque que se escribirá se encuentra compartido 
                            {
                                contadorCaches = 0;
                                contadorCachesSolicitadas = 0;
                                for (int i = 2; i < columnasDirectorio; ++i)
                                {
                                    if (procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, i] == 1)
                                    {
                                        ++contadorCaches;

                                        if (numProcesadorLocal == i - 1) //El bloque que se encuentra compartido pertenece al directorio local
                                        {
                                            cacheDatos[5, posicionCache] = invalido;
                                            procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, i] = 0;
                                            ++contadorCachesSolicitadas;
                                        }
                                        else
                                        {
                                            //Se solicita la caché correspondiente para invalidar el bloque
                                            if (solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(datosHilos[filaContextoActual, 4], i - 1, posicionCache))
                                            {
                                                //Se invalida la entrada del procesador que tenía el bloque en el directorio 
                                                procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, i] = 0;
                                                ++contadorCachesSolicitadas;
                                            }
                                            else
                                            {
                                                //SE TIENE QUE LIBERAR TODO E INICIAR DE NUEVO
                                                bloqueo = false;
                                            }
                                        }
                                    }
                                }

                                //Se verifica que la cantidad de cachés que debían invalidarse realmente lo pudieron hacer
                                if (contadorCaches == contadorCachesSolicitadas)
                                {

                                    //Se actualiza el estado del bloque en el directorio
                                    procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] = modificado;

                                    //Se actualiza la entrada del procesador que posee el bloque en el directorio
                                    procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    //Se actualiza el estado del bloque en la caché local
                                    cacheDatos[5, posicionCache] = modificado;

                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }

                            }
                        }
                        finally
                        {
                            Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                        }
                    }
                    else
                    {
                        bloqueo = false;
                    }

            }
            
            return bloqueo;
        }


        //Método para obtener la caché con el fin de copiar a memoria el contenido del bloque víctima que se encuentra modificado
        public bool solicitarCacheExterna_BloqueModificado_Diagrama3_SW(int numProcesadorLocal, int numCacheCasa, int[] memCompartida, int numBloque, int posicionMemoriaCompartida, int posicionCache, bool hit, bool memLocal)
        {
            bool bloqueo = false;
            int posicionBloque = posicionMemoriaCompartida;
            int contador = 0;

            if (numProcesadorLocal == 1)
            {
                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 2).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        if(memLocal)
                        {
                            //For de 16 ciclos para simular lo que se tarda en acceder en escribir desde caché a memoria
                            for (int i = 0; i < 16; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                        }
                        else
                        {
                            //For de 32 ciclos para simular lo que se tarda en acceder en escribir desde caché a memoria remota
                            for (int i = 0; i < 32; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }

                    
                        //Se copia en memoria el contenido del bloque 
                        contador = posicionMemoriaCompartida;
                        for (int i = 0; i < 4; ++i)
                        {
                            memCompartida[contador] = procesadores.ElementAt(numCacheCasa - 2).cacheDatos[i, posicionCache];
                            ++contador;
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCacheCasa - 2).cacheDatos[5, posicionCache] = invalido;

                        if(banderaLL)
                        {
                            if(bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }


                        if (!hit)
                        {
                            //For de 20 ciclo para simular lo que se tarda en acceder en escribir desde caché a caché remota
                            for (int i = 0; i < 20; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            //Se debe copiar en la caché local el contenido que tenía el bloque ya copiado a memoria
                            contador = posicionMemoriaCompartida;
                            for (int i = 0; i < 4; ++i)
                            {
                                cacheDatos[i, posicionCache] = procesadores.ElementAt(numCacheCasa - 2).cacheDatos[i, posicionCache];
                                ++contador;
                            }

                            //Se establece el número del bloque en la caché de datos
                            cacheDatos[4, posicionCache] = numBloque;

                            //Se establece el estado del bloque en la caché de datos
                            cacheDatos[5, posicionCache] = modificado;

                        }

                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 2).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }

            else if (numProcesadorLocal == 2)
            {
                int numCache = 0;
                if (numCacheCasa == 3)
                {
                    numCache++;
                }
                if (Monitor.TryEnter(procesadores.ElementAt(numCache).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        //For de 32 ciclo para simular lo que se tarda en acceder en escribir desde caché a memoria
                        for (int i = 0; i < 32; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        //Copio en memoria el contenido del bloque víctima 
                        int contadorMem = posicionMemoriaCompartida;
                        for (int i = 0; i < 4; ++i)
                        {
                           memCompartida[contador] = procesadores.ElementAt(numCache).cacheDatos[i, posicionCache];
                            ++contadorMem;
                        }

                        if (banderaLL)
                        {
                            if (bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCache).cacheDatos[5, posicionCache] = invalido;

                        if (!hit)
                        {
                            //Se debe copiar en la caché local el contenido que tenía el bloque ya copiado a memoria

                            //For de 20 ciclo para simular lo que se tarda en acceder en escribir desde caché a caché remota
                            for (int i = 0; i < 20; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            contador = posicionMemoriaCompartida;
                            for (int i = 0; i < 4; ++i)
                            {
                                cacheDatos[i, posicionCache] = procesadores.ElementAt(numCache).cacheDatos[i, posicionCache];
                                ++contador;
                            }

                            //Se establece el número del bloque en la caché de datos
                            cacheDatos[4, posicionBloque] = numBloque ;

                            //Se establece el estado del bloque en la caché de datos
                            cacheDatos[5, posicionCache] = modificado;

                        }

                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCache).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            else if (numProcesadorLocal == 3)
            {

                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 1).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        //For de 32 ciclo para simular lo que se tarda en acceder en escribir desde caché a memoria
                        for (int i = 0; i < 32; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        contador = posicionMemoriaCompartida;
                        //Copio en memoria el contenido del bloque víctima 
                        for (int i = 0; i < 4; ++i)
                        {
                            memCompartida[contador] = procesadores.ElementAt(numCacheCasa - 1).cacheDatos[i, posicionCache];
                            ++contador;
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCacheCasa - 1).cacheDatos[5, posicionCache] = invalido;

                        if (banderaLL)
                        {
                            if (bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }

                        if (!hit)
                        {

                            //For de 20 ciclo para simular lo que se tarda en acceder en escribir desde caché a caché remota
                            for (int i = 0; i < 20; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }

                            //Se debe copiar en la caché local el contenido que tenía el bloque ya copiado a memoria
                            contador = posicionMemoriaCompartida;
                            for (int i = 0; i < 4; ++i)
                            {
                                cacheDatos[i, posicionCache] = procesadores.ElementAt(numCacheCasa - 1).cacheDatos[i, posicionCache];
                                ++contador;
                            }

                            //Se establece el número del bloque en la caché de datos
                            cacheDatos[4, posicionCache] = numBloque;

                            //Se establece el estado del bloque en la caché de datos
                            cacheDatos[5, posicionCache] = modificado;

                        }

                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 1).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }

            }

            return bloqueo;
        }

        //Método para obtener la caché con el fin de invalidar un bloque en particular
        public bool solicitarCacheExterna_BloqueCompartido_Diagrama4_SW(int numProcesadorLocal, int numCacheCasa, int posicionCache)
        {
            bool bloqueo = false;
            Debug.WriteLine("ESTOY EN SOLICITAR DIRECTORIO BLOQUE COMPARTIDO DIAGRAMA 4");


            Debug.WriteLine("El NUM DEL PROCESADOR ES: " + numProcesadorLocal + " LA NUMERO DE CACHE ES: " + numCacheCasa);

            if (numProcesadorLocal == 1)
            {
                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 2).cacheDatos))
                {
                    try
                    {

                        bloqueo = true;

                        //For de 1 ciclo para simular lo que se tarda en invalidar
                        for (int i = 0; i < 1; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCacheCasa - 2).cacheDatos[5, posicionCache] = invalido;

                        if (banderaLL)
                        {
                            if (bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 2).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }

            else if (numProcesadorLocal == 2)
            {
                int numCache = 0;
                if (numCacheCasa == 3)
                {
                    numCache++;
                }
                if (Monitor.TryEnter(procesadores.ElementAt(numCache).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;


                        //For de 1 ciclo para simular lo que se tarda en invalidar
                        for (int i = 0; i < 1; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCache).cacheDatos[5, posicionCache] = invalido;

                        if (banderaLL)
                        {
                            if (bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCache).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            else if (numProcesadorLocal == 3)
            {

                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 1).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;


                        //For de 1 ciclo para simular lo que se tarda en invalidar
                        for (int i = 0; i < 1; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }

                        //Actualiza la entrada de la caché a inválida
                        procesadores.ElementAt(numCacheCasa - 1).cacheDatos[5, posicionCache] = invalido;

                        if (banderaLL)
                        {
                            if (bloqueLL == procesadores.ElementAt(numCacheCasa - 2).cacheDatos[4, posicionCache])
                            {
                                hilosRL[filaContextoActual] = -1;
                            }
                        }

                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 1).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }

            }

            return bloqueo;
        }



        //Método para solicitar la caché de datos de otro procesador
        public bool solicitarCacheExterna(int numProcesadorLocal, int numCacheCasa, int posicionMemoriaCompartida, int numBloque, int posicionCache, int[] memCompartida, bool memLocal)
        {
            bool bloqueo = false;
            int contador = 0;


            if (numProcesadorLocal == 1)
            {
                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 2).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        if(memLocal)
                        {
                            //For de 16 ciclos para simular lo que se tarda en escribir en mem local
                            for (int i = 0; i < 16; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }
                        else
                        {
                            //For de 32 ciclos para simular lo que se tarda en escribir en mem remota
                            for (int i = 0; i < 32; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }


                        //Se copia en la memoria respectiva el contenido del bloque ubicado en la caché de datos del procesador 2 y en la caché del procesador 1
                        contador = posicionMemoriaCompartida;
                        for (int i = 0; i < 4; ++i)
                        {
                            memCompartida[contador] = procesadores.ElementAt(numCacheCasa - 2).cacheDatos[i, posicionCache];
                            cacheDatos[i, posicionCache] = procesadores.ElementAt(numCacheCasa - 2).cacheDatos[i, posicionCache];
                            ++contador;
                        }

                        //Actualiza el numero de bloque y el estado de ese bloque en la caché propia
           //             Debug.WriteLine("ESTOY METIENDO ESTE BLOQUE CACHE EXTERNA: " + numBloque);
                        cacheDatos[4, posicionCache] = numBloque;
                        cacheDatos[5, posicionCache] = compartido;
                      
                        //actualiza la entrada de la caché con compartido
                        procesadores.ElementAt(numCacheCasa - 2).cacheDatos[5, posicionCache] = compartido;

                        //realiza la lectura.
                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 2).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }

            else if (numProcesadorLocal == 2)
            {
                int numCache = 0;
                if (numCacheCasa == 3)
                {
                    numCache++;
                }
                if (Monitor.TryEnter(procesadores.ElementAt(numCache).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;
                        contador = posicionMemoriaCompartida;
                        Debug.WriteLine("esto vale el contador " + contador);

                        if (memLocal)
                        {
                            //For de 16 ciclos para simular lo que se tarda en escribir en mem local
                            for (int i = 0; i < 16; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }
                        else
                        {
                            //For de 32 ciclos para simular lo que se tarda en escribir en mem remota
                            for (int i = 0; i < 32; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }

                        //Se copia en la memoria respectiva el contenido del bloque ubicado en la caché de datos del procesador 2 y en la caché del procesador 1
                        for (int i = 0; i < 4; ++i)
                        {
                            memCompartida[contador] = procesadores.ElementAt(numCache).cacheDatos[i, posicionCache];
                            cacheDatos[i, posicionCache] = procesadores.ElementAt(numCache).cacheDatos[i, posicionCache];
                            ++contador;
                        }

                        //Actualiza el numero de bloque y el estado de ese bloque en la caché propia
                        cacheDatos[4, posicionCache] = numBloque;
                        cacheDatos[5, posicionCache] = compartido;

                        //actualiza la entrada de la caché con compartido
                        procesadores.ElementAt(numCache).cacheDatos[5, posicionCache] = compartido;

                        //realiza la lectura.
                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCache).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            else if (numProcesadorLocal == 3)
            {

                if (Monitor.TryEnter(procesadores.ElementAt(numCacheCasa - 1).cacheDatos))
                {
                    try
                    {
                        bloqueo = true;

                        if (memLocal)
                        {
                            //For de 16 ciclos para simular lo que se tarda en escribir en mem local
                            for (int i = 0; i < 16; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }
                        else
                        {
                            //For de 32 ciclos para simular lo que se tarda en escribir en mem remota
                            for (int i = 0; i < 32; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }

                        contador = posicionMemoriaCompartida;


                        //Se copia en la memoria respectiva el contenido del bloque ubicado en la caché de datos del procesador 2 y en la caché del procesador 1
                        for (int i = 0; i < 4; ++i)
                        {
                            memCompartida[contador] = procesadores.ElementAt(numCacheCasa - 1).cacheDatos[i, posicionCache];
                            cacheDatos[i, posicionCache] = procesadores.ElementAt(numCacheCasa - 1).cacheDatos[i, posicionCache];
                            ++contador;
                        }

                        //Actualiza el numero de bloque y el estado de ese bloque en la caché propia
                        cacheDatos[4, posicionCache] = numBloque;
                        cacheDatos[5, posicionCache] = compartido;


                        //actualiza la entrada de la caché con compartido
                        procesadores.ElementAt(numCacheCasa - 1).cacheDatos[5, posicionCache] = compartido;

                        //realiza la lectura.
                    }
                    finally
                    {
                        Monitor.Exit(procesadores.ElementAt(numCacheCasa - 1).cacheDatos);
                    }
                }
                else
                {
                    bloqueo = false;
                }

            }

            return bloqueo;
        }

        /* Método para verificar el estado de un bloque en un load debido a un fallo */
        public bool verificarEstadoBloqueFalloCache_LW(int numProcesadorLocal, int[,] dir, int[] memCompartida, int posicionMemoriaCompartida, int posicionBloqueDirectorio, int posicionCacheDatos, int numBloque, bool dirSolicitado, bool dirLocal, bool memLocal)
        {
            bool bloqueo = false;
            int[] datosBloqueModificado = new int[4];

            if(dirSolicitado)
            {
                //Verifico si alguna caché lo tiene modificado
                datosBloqueModificado = bloqueModificado(dir, posicionBloqueDirectorio);
                if (datosBloqueModificado[0] == 1)
                {
      //              Debug.WriteLine("este es el numero del bloque en verificar estado bloque fallo cache " + datosBloqueModificado[3]);
                    //Se solicita la caché externa
                    if (solicitarCacheExterna(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], posicionMemoriaCompartida, datosBloqueModificado[3], posicionCacheDatos, memCompartida, memLocal))
                    {
                        //actualiza la entrada del directorio con compartido
                        dir[posicionBloqueDirectorio, 1] = compartido;

                        //Se actualiza la entrada del directorio del procesador que desea realizar la lectura
                        dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                        bloqueo = true;
                    }
                    else
                    {
                        //SE LIBERAN LAS COSAS
                        bloqueo = false;
                    }
                }
                else
                {
              
                    copiarBloqueDesdeMemoria(memCompartida, posicionMemoriaCompartida, posicionCacheDatos, numBloque, 0, memLocal);

                    //Actualiza la entrada del estado de la caché en el directorio a "compartido" y 
                    //se indica que lo posee el procesador 1
                    dir[posicionBloqueDirectorio, 1] = compartido;

                    //Se actualiza la entrada del directorio del procesador que desea realizar la lectura
                    dir[posicionBloqueDirectorio, numProcesadorLocal+1] = 1;

                    bloqueo = true;

                    //Realiza la lectura
                }
          }
          else
          {
                if (Monitor.TryEnter(dir))
                {
                       try
                       {

                           if(dirLocal)
                           {

                               //For de 2 ciclos para simular lo que se tarda en acceder a directorio local
                               for (int i = 0; i < 2; ++i)
                               {
                                   barreraFinInstr.SignalAndWait();
                                   barreraCambioReloj_Ciclo.SignalAndWait();
                               }
                           }
                           else
                           {
                               //For de 4 ciclos para simular lo que se tarda en acceder a directorio remoto
                               for (int i = 0; i < 4; ++i)
                               {
                                   barreraFinInstr.SignalAndWait();
                                   barreraCambioReloj_Ciclo.SignalAndWait();
                               }
                           }



                           bloqueo = true;
                            //Verifico si alguna caché lo tiene modificado
                            datosBloqueModificado = bloqueModificado(dir, posicionBloqueDirectorio);
                            if (datosBloqueModificado[0] == 1)
                            {
                                //Se solicita la caché externa
             //                   Debug.WriteLine("VERIFICAR ESTADO BLOQUE FALLO CACHE");
             //                   Debug.WriteLine("este es el numero del bloque en verificar estado bloque fallo cache " + datosBloqueModificado[3]);
                                if (solicitarCacheExterna(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], posicionMemoriaCompartida, datosBloqueModificado[3], posicionCacheDatos, memCompartida, memLocal))
                                {
                                    //actualiza la entrada del directorio con compartido
                                    dir[posicionBloqueDirectorio, 1] = compartido;

                                    //Se actualiza la entrada del directorio del procesador que desea realizar la lectura
                                    dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                                    bloqueo = true;
                                }
                                else
                                {
                                    //SE LIBERAN LAS COSAS
                                    bloqueo = false;
                                }
                            }
                            else
                            {
                                copiarBloqueDesdeMemoria(memCompartida, posicionMemoriaCompartida, posicionCacheDatos, numBloque, 0, memLocal);

                                //Actualiza la entrada del estado de la caché en el directorio a "compartido" y el procesador
                                //que lo posee
                                dir[posicionBloqueDirectorio, 1] = compartido;

                                //Se actualiza la entrada del directorio del procesador que desea realizar la lectura
                                dir[posicionBloqueDirectorio, numProcesadorLocal+1] = 1;

                                bloqueo = true;

                                //Realiza la lectura
                            }
                       }
                       finally
                       {
                           Monitor.Exit(dir);
                       }
                }
                else
                {
                    bloqueo = false;
                }
          }

            return bloqueo;
           
        }


        public bool verificarEstadoBloque_Diagrama3_LW(int numProcesadorLocal, int [,] dir, int[] memCompartida, int posicionMemoriaCompartida, int numBloque, int posicionBloqueDirectorio, int posicionCache, bool directorioSolicitado, bool memLocal, bool dirLocal)
        {
            bool bloqueo = false;
            int[] datosBloqueModificado = new int[4];


            if(directorioSolicitado)
            {
                //Verifico si alguna caché lo tiene modificado
                datosBloqueModificado = bloqueModificado(directorio, posicionBloqueDirectorio);
                if (datosBloqueModificado[0] == 1)
                {

                    //Se solicita la caché externa
                    if (solicitarCacheExterna(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], posicionMemoriaCompartida, datosBloqueModificado[3], posicionCache, memCompartida, memLocal))
                    {
                        //actualiza la entrada del directorio con compartido
                        dir[posicionBloqueDirectorio, 1] = compartido;

                        //Se actualiza la entrada del procesador que ahora ya realizará la lectura
                        dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;
                    }
                    else
                    {
                        //SE LIBERAN LAS COSAS
                        bloqueo = false;
                    }
                }
                else
                {
                    //0 porque es un load
                    copiarBloqueDesdeMemoria(memCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 0, memLocal);

                    //Actualiza la entrada del estado de la caché en el directorio a "compartido" y 
                    //se indica cual procesador lo posee
                    dir[posicionBloqueDirectorio, 1] = compartido;

                    //Se actualiza la entrada del procesador que ahora ya realizará la lectura
                    dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                    bloqueo = true;
                }
            }
            else
            {
                if (Monitor.TryEnter(dir))
                {
                    try
                    {
                        bloqueo = true;


                        if(dirLocal)
                        {
                            //For de 2 ciclos para simular el acceso a directorio local                        
                            for (int i = 0; i < 2; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }
                        else
                        {
                            //For de 4 ciclos para simular el acceso a directorio local                        
                            for (int i = 0; i < 4; ++i)
                            {
                                barreraFinInstr.SignalAndWait();
                                barreraCambioReloj_Ciclo.SignalAndWait();
                            }
                        }
                     

                        //Verifico si alguna caché lo tiene modificado
                        datosBloqueModificado = bloqueModificado(directorio, posicionBloqueDirectorio);
                        if (datosBloqueModificado[0] == 1)
                        {
                            //Se solicita la caché externa
                            if (solicitarCacheExterna(datosHilos[filaContextoActual, 4], datosBloqueModificado[1], posicionMemoriaCompartida, datosBloqueModificado[3], posicionCache, memCompartida, memLocal))
                            {
                                //actualiza la entrada del directorio con compartido
                                dir[posicionBloqueDirectorio, 1] = compartido;

                                //Se actualiza la entrada del procesador que ahora ya realizará la lectura
                                dir[posicionBloqueDirectorio, numProcesadorLocal + 1] = 1;

                       
                            }
                            else
                            {
                                //SE LIBERAN LAS COSAS
                                bloqueo = false;
                            }
                        }
                        else
                        {
                            //0 porque es un load
                            copiarBloqueDesdeMemoria(memCompartida, posicionMemoriaCompartida, posicionCache, numBloque, 0, memLocal);

                            //Actualiza la entrada del estado de la caché en el directorio a "compartido" y 
                            //se indica cual procesador lo posee
                            dir[posicionBloqueDirectorio, 1] = compartido;
                            dir[posicionBloqueDirectorio, numProcesadorLocal+1] = 1;

                            bloqueo = true;
                        }
                    }
                    finally 
                    {
                        Monitor.Exit(dir);
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }

            return bloqueo;
            
        }

        //Método para solicitar un directorio externo
        public bool solicitarDirectorioDiagrama3_LW(int numProcesadorLocal, int numDirectorioBloqueVictima, int numDirectorioCasa, int numBloqueVictima, int numBloque, int posMemBloqueVictima, int posMemBloque, int posicionCache )
        {
            bool bloqueo = false;
            int posicionBloqueDirectorio = 0;
            bool liberarDirectorio = true;
            int posicionProcesador = 0;
            int posicionProcesadorDir = 0;
            bool directorioSolicitado = false;           
            int[] datosBloqueModificado = new int[4];
            int contador = 0;
            int posicionBloqueDirectorioCasa = 0;
            bool memLocal = false;
            bool dirLocal = false;

            if(numBloqueVictima <= 7)
            {
                posicionBloqueDirectorio = numBloqueVictima;
            }
            else if(numBloqueVictima > 7 && numBloqueVictima <= 15)
            {
                posicionBloqueDirectorio = numBloqueVictima - 8;
            }
            else
            {
                posicionBloqueDirectorio = numBloqueVictima - 16;

            }

            if(numBloque <= 7)
            {
                posicionBloqueDirectorioCasa = numBloque;
            }
            else if(numBloque > 7 && numBloque <= 15)
            {
                 posicionBloqueDirectorioCasa = numBloque - 8;
            }
            else
            {
                 posicionBloqueDirectorioCasa = numBloque - 16;
            }

      //      Debug.WriteLine("num del bloque victima es: " + numBloqueVictima);
      //      Debug.WriteLine("posicion del bloque en el directorio es: " + posicionBloqueDirectorio);


            if (numProcesadorLocal == numDirectorioBloqueVictima)
            {

                if (Monitor.TryEnter(directorio))
                {
                    try
                    {
                        bloqueo = true;

                        //For de 4 ciclos para simular el acceso a directorio local
                        for (int i = 0; i < 2; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }


                            if (cacheDatos[5, posicionCache] == modificado) //El bloque víctima se encuentra modificado
                            {
                                contador = posMemBloqueVictima;
                                //Se copia en la memoria respectiva el contenido del bloque ubicado en la caché de datos
                                for (int i = 0; i < 4; ++i)
                                {
                                    memoriaCompartida[contador] = cacheDatos[i, posicionCache];
                                    ++contador;
                                }

                            }

                        //Se coloca en "uncached" la posición del bloque que será reemplazado y se actualiza la entrada del procesador que antes 
                        //tenía el bloque
                        directorio[posicionBloqueDirectorio, numDirectorioBloqueVictima + 1] = 0;
                        if (bloqueLibre(directorio, posicionBloqueDirectorio))
                        {
                            directorio[posicionBloqueDirectorio, 1] = uncached;
                        }

                        //Se invalida la entrada en la caché de datos
                        cacheDatos[5, posicionCache] = invalido;

                        //Se verifica si el bloque que se va a leer se encuentra en el mismo directorio
                        if(numProcesadorLocal == 1)
                        {
                            if (numBloque <= 7) //El bloque que se leerá pertenece al procesador 1
                            {
                              
                                if (numDirectorioBloqueVictima == 1)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    directorioSolicitado = false;
                                   
                                    //Libera su propio directorio 
                                    Monitor.Exit(directorio);

                                    liberarDirectorio = false;
                                    
                                }

                                memLocal = true;
                                dirLocal = true;
                            
                                if(verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, directorio, memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }                      
                            }

                            else //Bloque a leer pertenece al segundo o al tercer procesador
                            {
                                if (numBloque <= 15) //El bloque a leer pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                       
                                    }
                                    else
                                    {
                                        directorioSolicitado = false;

                                        //Libera su propio directorio 
                                        Monitor.Exit(directorio);
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 0;

                                }
                                else //El bloque a leer pertenece al tercer procesador
                                {
                                    if (numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        directorioSolicitado = false;

                                        //Libera su propio directorio 
                                        Monitor.Exit(directorio);
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 1;
                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesador).directorio, procesadores.ElementAt(posicionProcesador).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if(numProcesadorLocal == 2)
                        {
                            if (numBloque > 7 && numBloque <= 15) //El bloque que se escribirá pertenece al procesador 2
                            {
                                if (numDirectorioBloqueVictima == 2)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesador).directorio, procesadores.ElementAt(posicionProcesador).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al tercer procesador
                            {

                                if (numBloque <= 7) //El bloque a escribir pertenece al primer procesador
                                {
                                    if (numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 0;

                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    if (numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 1;
                                }

                                directorioSolicitado = false;
                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesador).directorio, procesadores.ElementAt(posicionProcesador).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if(numProcesadorLocal == 3)
                        {
                            if (numBloque > 15) //El bloque que se escribirá pertenece al procesador 3
                            {
                                if (numDirectorioBloqueVictima == 3)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                    directorioSolicitado = false;
                                    liberarDirectorio = false;
                                }

                                memLocal = true;
                                dirLocal = true;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, directorio, memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al segundo procesador
                            {

                                if (numBloque <= 7) //El bloque a escribir pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 1)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 0;

                                }
                                else //El bloque a escribir pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        directorioSolicitado = false;
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesador = 1;
                                    numBloque = numBloque - 8;
                                }

                                memLocal = false;
                                dirLocal = false;
                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesador).directorio, procesadores.ElementAt(posicionProcesador).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }

                       
                    }
                    finally
                    {
                        if (liberarDirectorio)
                        {
                            Monitor.Exit(directorio);
                        }
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }
            else
            {
          //      Debug.WriteLine("el numero del directorio bloque victima es: " + numDirectorioBloqueVictima);
                if (numProcesadorLocal == 1) //Se está ejecutando el procesador 1
                {
                    posicionProcesador = numDirectorioCasa - 2;
                }
                else if (numProcesadorLocal == 2) //Se está ejecutando el procesador 2
                {
                    if (numDirectorioCasa == 1)
                    {
                        posicionProcesador = 0;
                    }
                    else
                    {
                        posicionProcesador = 1;
                    }

                }
                else //Se está ejecutando el procesador 3
                {
                    posicionProcesador = numDirectorioCasa - 1;
                }

                if (Monitor.TryEnter(procesadores.ElementAt(posicionProcesador).directorio))
                {
                    try
                    {
                        bloqueo = true;
                        liberarDirectorio = false;

                        //For de 4 ciclos para simular el acceso a directorio remoto
                        for (int i = 0; i < 4; ++i)
                        {
                            barreraFinInstr.SignalAndWait();
                            barreraCambioReloj_Ciclo.SignalAndWait();
                        }


                        if (cacheDatos[5, posicionCache] == modificado) //El bloque víctima se encuentra modificado
                        {
                            contador = posMemBloqueVictima;
                            //Se copia en la memoria respectiva el contenido del bloque ubicado en la caché de datos
                            for (int i = 0; i < 4; ++i)
                            {
                                procesadores.ElementAt(posicionProcesador).memoriaCompartida[contador] = cacheDatos[i, posicionCache];
                                ++contador;
                            }

                        }

                        //Se coloca en "uncached" la posición del bloque que será reemplazado y se actualiza la entrada del procesador
                        procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, numDirectorioBloqueVictima+1] = 0;

                        if (bloqueLibre(procesadores.ElementAt(posicionProcesador).directorio, posicionBloqueDirectorio))
                        {
                            procesadores.ElementAt(posicionProcesador).directorio[posicionBloqueDirectorio, 1] = uncached;
                        }


                        //Se invalida la entrada en la caché de datos
                        cacheDatos[5, posicionCache] = invalido;

                        //Se verifica si el bloque que se va a leer se encuentra en el mismo directorio
                        if (numProcesadorLocal == 1)
                        {
                            if (numBloque <= 7) //El bloque que se leerá pertenece al procesador 1
                            {

                                if (numDirectorioBloqueVictima == 1)
                                {
                                    directorioSolicitado = true;
                                    liberarDirectorio = true;
                                }
                                else
                                {
                                    directorioSolicitado = false;

                                    //Libera su propio directorio 
                                    Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                    liberarDirectorio = false;

                                }

                                memLocal = true;
                                dirLocal = true;
                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, directorio, memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }

                            else //Bloque a leer pertenece al segundo o al tercer procesador
                            {
                                if (numBloque <= 15) //El bloque a leer pertenece al segundo procesador
                                {
                                    if (numDirectorioBloqueVictima == 2)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;

                                    }
                                    else
                                    {
                                        directorioSolicitado = false;

                                        //Libera su propio directorio 
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 0;
                                }
                                else //El bloque a leer pertenece al tercer procesador
                                {
                                    if (numDirectorioBloqueVictima == 3)
                                    {
                                        directorioSolicitado = true;
                                        liberarDirectorio = true;
                                    }
                                    else
                                    {
                                        directorioSolicitado = false;

                                        //Libera su propio directorio 
                                        Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                        liberarDirectorio = false;
                                    }

                                    posicionProcesadorDir = 1;

                                }

                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if (numProcesadorLocal == 2)
                        {
                            if (numBloque > 7 && numBloque <= 15) //El bloque que se escribirá pertenece al procesador 2
                            {
                                numBloque = numBloque - 8;
                                directorioSolicitado = true;
                                liberarDirectorio = true;
                                memLocal = true;
                                dirLocal = true;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, directorio, memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al tercer procesador
                            {

                                //Libera su propio directorio 
                                Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);

                                liberarDirectorio = false;

                                if (numBloque <= 7) //El bloque a escribir pertenece al segundo procesador
                                {
                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al tercer procesador
                                {
                                    posicionProcesadorDir = 1;
                                }

                                directorioSolicitado = false;
                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                        else if (numProcesadorLocal == 3)
                        {
                            if (numBloque > 15) //El bloque que se escribirá pertenece al procesador 3
                            {
                                numBloque = numBloque - 16;
                                directorioSolicitado = true;
                                liberarDirectorio = true;
                                memLocal = true;
                                dirLocal = true;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, directorio, memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                            else //Bloque a escribir pertenece al primero o al segundo procesador
                            {

                                //Libera su propio directorio 
                                Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                                liberarDirectorio = false;

                                if (numBloque <= 7) //El bloque a escribir pertenece al segundo procesador
                                {
                                    posicionProcesadorDir = 0;

                                }
                                else //El bloque a escribir pertenece al segundo procesador
                                {
                                    posicionProcesadorDir = 1;
                                    numBloque = numBloque - 8;
                                }

                                directorioSolicitado = false;
                                memLocal = false;
                                dirLocal = false;

                                if (verificarEstadoBloque_Diagrama3_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesadorDir).directorio, procesadores.ElementAt(posicionProcesadorDir).memoriaCompartida, posMemBloque, numBloque, posicionBloqueDirectorioCasa, posicionCache, directorioSolicitado, memLocal, dirLocal))
                                {
                                    bloqueo = true;
                                }
                                else
                                {
                                    bloqueo = false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if(liberarDirectorio)
                        {
                            Monitor.Exit(procesadores.ElementAt(posicionProcesador).directorio);
                        }
                        
                    }
                }
                else
                {
                    bloqueo = false;
                }
            }

            return bloqueo;
        }

        public bool solicitarDirectorioDiagrama1_LW(int numProcesadorLocal, int numDirectorio, int numBloque, int posicionMemoriaCompartida, int posicionCacheDatos)
        {
            bool bloqueo = false;
            int posicionBloqueDirectorio = 0;
            int[] datosBloqueModificado = new int[4];
            bool directorioSolicitado = false;
            int posicionProcesador = 0;
            bool directorioLocal = false;
            bool memLocal = false;
           

            if(numDirectorio == 1)
            {
                posicionBloqueDirectorio = numBloque;
            }
            else if(numDirectorio == 2)
            {
                posicionBloqueDirectorio = numBloque - 8;
            }
            else
            {
                posicionBloqueDirectorio = numBloque - 16;
            }


            if (numProcesadorLocal == numDirectorio)
            {
                directorioSolicitado = false;
                directorioLocal = true;
                memLocal = true;

                if (verificarEstadoBloqueFalloCache_LW(numProcesadorLocal, directorio, memoriaCompartida, posicionMemoriaCompartida, posicionBloqueDirectorio, posicionCacheDatos, numBloque, directorioSolicitado, directorioLocal, memLocal))
                {
                    bloqueo = true;
                }
                else 
                {
                    bloqueo = false;
                }
            }
            else
            {
                if (numProcesadorLocal == 1) //Se está ejecutando el procesador 1
                {
                    posicionProcesador = numDirectorio - 2;
                }
                else if (numProcesadorLocal == 2) //Se está ejecutando el procesador 2
                {
                    if (numDirectorio == 1)
                    {
                        posicionProcesador = 0;
                    }
                    else
                    {
                        posicionProcesador = 1;
                    }

                }
                else //Se está ejecutando el procesador 3
                {
                    posicionProcesador = numDirectorio - 1;
                }

                directorioSolicitado = false;
                directorioLocal = false;
                memLocal = false;

                if (verificarEstadoBloqueFalloCache_LW(numProcesadorLocal, procesadores.ElementAt(posicionProcesador).directorio, procesadores.ElementAt(posicionProcesador).memoriaCompartida, posicionMemoriaCompartida, posicionBloqueDirectorio,  posicionCacheDatos, numBloque, directorioSolicitado, directorioLocal, memLocal))
                {
                    bloqueo = true;
                }
                else
                {
                    bloqueo = false;
                }
            }

            return bloqueo;
        }


        /* Método que indica si en el directorio ya ninguna caché está utilizando el bloque */
        public bool bloqueLibre(int[,] dir, int posicionBloque)
        {
            bool libre = false;
            int contadorColumnas = 2;

            while (contadorColumnas < columnasDirectorio && dir[posicionBloque, contadorColumnas] != 0)
            {
                ++contadorColumnas;
            }

            if (contadorColumnas >= columnasDirectorio)
            {
                libre = true;
            }

            return libre;
        }



        /*Método que indica si una caché tiene un bloque modificado en el directorio */
        public int[] bloqueModificado(int[,] dir, int posicionBloque)
        {
            // bool modificado = false;
            int contadorColumnas = 2;
            int[] bloqueModificado = new int[4];

            if(dir[posicionBloque, 1] == modificado)
            {
                while (contadorColumnas < columnasDirectorio && dir[posicionBloque, contadorColumnas] == 0)
                {
                    ++contadorColumnas;
                }

              /*  Debug.WriteLine("DIRECTORIO");

                for (int j = 0; j < columnasDirectorio; ++j)
                {
                    Debug.Write("Contenido del directorio " + dir[posicionBloque, j] + " ");
                } */


                if (contadorColumnas < columnasDirectorio)
                {
                    bloqueModificado[0] = 1; //Indica que el bloque está modificado
                    bloqueModificado[1] = contadorColumnas - 1; //Se guarda el número del procesador que tiene el bloque modificado
                    if (bloqueModificado[1] == 1) //Se guarda la posición del bloque
                    {
                        bloqueModificado[2] = dir[posicionBloque, 0];
                    }
                    else if (bloqueModificado[1] == 2)
                    {
                        bloqueModificado[2] = (dir[posicionBloque, 0]) - 8;
                    }
                    else
                    {
                        bloqueModificado[2] = (dir[posicionBloque, 0]) - 16;
                    }

                    bloqueModificado[3] = dir[posicionBloque, 0]; //Se guarda el número del bloque
                }
                else
                {
                    bloqueModificado[0] = 0;
                    bloqueModificado[1] = 0;
                    bloqueModificado[2] = 0;
                    bloqueModificado[3] = 0;
                }
            }
            else
            {
                bloqueModificado[0] = 0;
                bloqueModificado[1] = 0;
                bloqueModificado[2] = 0;
                bloqueModificado[3] = 0;
            }


           

            return bloqueModificado;
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
                    if (i == 4)
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
                while (contadorInstrucciones < quantum && ejecucionHilos[filaContextoActual, 1] == 0)
                {
                    contadorInstrucciones++;
                    leerInstruccion();
                }

                //Se copia en el contexto del hilo que se estaba ejecutando el valor de los registros porque se acabó el quantum
                for (contadorContexto = 0; contadorContexto < columnasContexto - 1; ++contadorContexto)
                {
                    contexto[filaContextoActual, contadorContexto] = registros[contadorContexto];
                }

                //Se guarda en RL un -1
                hilosRL[filaContextoActual] = -1;


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

                    /* Se verifica si es la primera vez que se ejecuta el hilo, pues en caso de serlo se debe guardar el valor actual del reloj */
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
