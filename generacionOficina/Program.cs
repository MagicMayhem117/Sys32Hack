using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks; // Added
using Google.Cloud.AIPlatform.V1; // Added
using Google.Protobuf.WellKnownTypes; // Added for Struct/Value

// Assuming Office, Room, Sensor, Reader and their subclasses are defined elsewhere (e.g., office.cs)

public class Program
{
    // --- Métodos Ayudantes para Crear Objetos desde Config ---
    private static Sensor CreateSensorFromConfig(SensorConfig config, Room room)
    {
        // Comprobación defensiva
        if (room == null || config == null) return null;

        switch (config.Type)
        {
            case "Temperatura":
                // Usar el valor de InitialTemp del JSON, o 21.0 si es null
                return new TemperatureSensor(config.Id, room, config.InitialTemp ?? 21.0);
            case "Movimiento":
                return new MotionSensor(config.Id, room);
            case "Impresora":
                return new PrinterSensor(config.Id, room);
            default:
                Console.WriteLine($"Advertencia: Tipo de sensor desconocido '{config.Type}' con ID '{config.Id}' ignorado.");
                return null;
        }
    }

    private static Reader CreateReaderFromConfig(ReaderConfig config, Room room)
    {
        // Comprobación defensiva
        if (room == null || config == null) return null;

        switch (config.Type)
        {
            case "Tarjeta":
                // Si tuvieras otros tipos de Reader, los añadirías aquí
                return new CardReader(config.Id, room);
            default:
                Console.WriteLine($"Advertencia: Tipo de lector desconocido '{config.Type}' con ID '{config.Id}' ignorado.");
                return null;
        }
    }

    // --- Método para crear oficina por defecto si falla JSON ---
    private static Office CreateDefaultOffice()
    {
        Console.WriteLine("Creando configuración de oficina por defecto...");
        Office defaultOffice = new Office("Oficina Por Defecto");
        Room defaultRoom = new Room("Habitación Default", false);
        defaultRoom.AddSensor(new TemperatureSensor("TEMP-DEF-01", defaultRoom));
        defaultRoom.AddSensor(new MotionSensor("MOT-DEF-01", defaultRoom));
        defaultOffice.AddRoom(defaultRoom);
        return defaultOffice;
    }

    // --- Método para llamar a Gemini API ---
    private static async Task CallGeminiAsync(string prompt)
    {
        // !!! IMPORTANTE: Reemplaza con tu Project ID y Location ID !!!
        string projectId = "809804230041"; // <-- REEMPLAZA ESTO
        string locationId = "us-central1"; // <-- Cambia si usas otra región
        string publisher = "google";
        // Elige el modelo adecuado: 
        // "gemini-1.0-pro-001" para solo texto
        // "gemini-1.0-pro-vision-001" para texto e imagen (multimodal)
        string model = "gemini-1.0-pro-001";

        // Construir el endpoint regional
        var endpoint = $"{locationId}-aiplatform.googleapis.com";

        // Crear el cliente. Usará Application Default Credentials (ADC) si están configuradas.
        // Asegúrate de haber ejecutado 'gcloud auth application-default login'
        PredictionServiceClient client;
        try
        {
             client = new PredictionServiceClientBuilder
             {
                 Endpoint = endpoint
             }.Build();
        }
        catch (Exception ex)
        {
             Console.WriteLine($"\nError creando el cliente de PredictionService: {ex.Message}");
             Console.WriteLine("Verifica que Google Cloud SDK esté instalado y configurado con 'gcloud auth application-default login'.");
             return; // Salir si no se puede crear el cliente
        }


        // Construir la instancia de la solicitud (payload)
        // Usamos Struct/Value de Google.Protobuf.WellKnownTypes para crear un objeto JSON-like
        var instance = new Google.Protobuf.WellKnownTypes.Value
        {
            StructValue = new Google.Protobuf.WellKnownTypes.Struct
            {
                Fields =
                {
                    // Estructura 'content/parts' común para Gemini.
                    { "content", Google.Protobuf.WellKnownTypes.Value.ForStruct(new Google.Protobuf.WellKnownTypes.Struct {
                        Fields = {
                            { "role", Google.Protobuf.WellKnownTypes.Value.ForString("user") },
                            { "parts", Google.Protobuf.WellKnownTypes.Value.ForList(Google.Protobuf.WellKnownTypes.Value.ForStruct(new Google.Protobuf.WellKnownTypes.Struct {
                                Fields = {
                                    { "text", Google.Protobuf.WellKnownTypes.Value.ForString(prompt) }
                                }
                            }))}
                        }
                    })}
                }
            }
        };

        // Construir la solicitud de predicción completa
        var predictRequest = new PredictRequest
        {
            EndpointAsEndpointName = EndpointName.FromProjectLocationPublisherModel(projectId, locationId, publisher, model),
            Instances = { instance },
            // Puedes añadir parámetros como temperatura, max tokens, etc. aquí:
            // Parameters = Google.Protobuf.WellKnownTypes.Value.ForStruct(new Google.Protobuf.WellKnownTypes.Struct { Fields = { { "temperature", Google.Protobuf.WellKnownTypes.Value.ForNumber(0.5) } } })
        };

        try
        {
            Console.WriteLine($"\n--- Enviando prompt a Gemini: '{prompt}' ---");
            PredictResponse response = await client.PredictAsync(predictRequest);

            // Procesar la respuesta
            // La estructura exacta de la respuesta depende del modelo.
            // Para Gemini, suele estar dentro de predictions[0].structValue.fields["candidates"][0]...["content"]["parts"][0]["text"]
            string generatedText = "No se pudo extraer la respuesta de Gemini."; // Default
            if (response.Predictions.Count > 0 &&
                response.Predictions[0].StructValue.Fields.TryGetValue("candidates", out var candidatesValue) && candidatesValue.ListValue != null && candidatesValue.ListValue.Values.Count > 0 &&
                candidatesValue.ListValue.Values[0].StructValue != null && candidatesValue.ListValue.Values[0].StructValue.Fields.TryGetValue("content", out var contentValue) &&
                contentValue.StructValue != null && contentValue.StructValue.Fields.TryGetValue("parts", out var partsValue) && partsValue.ListValue != null && partsValue.ListValue.Values.Count > 0 &&
                partsValue.ListValue.Values[0].StructValue != null && partsValue.ListValue.Values[0].StructValue.Fields.TryGetValue("text", out var textValue))
            {
                generatedText = textValue.StringValue;
            }
            // Fallback por si la estructura es más simple (menos común para Gemini)
            else if (response.Predictions.Count > 0 && response.Predictions[0].StructValue.Fields.TryGetValue("content", out contentValue))
            {
                 generatedText = contentValue.StringValue;
            }


            Console.WriteLine("\nRespuesta de Gemini:");
            Console.WriteLine(generatedText);

        }
        catch (Grpc.Core.RpcException rpcEx)
        {
             Console.WriteLine($"\nError RPC al llamar a la API de Gemini: {rpcEx.Status.StatusCode}");
             Console.WriteLine("Detalles: " + rpcEx.Status.Detail);
             Console.WriteLine("Posibles causas: Project ID ('" + projectId + "') incorrecto o no existe, API 'Vertex AI API' no habilitada en Google Cloud, problemas de permisos/autenticación (revisar ADC), cuota excedida, endpoint ('" + endpoint + "') incorrecto.");
             if (projectId == "TU_PROJECT_ID")
             {
                Console.WriteLine("¡¡¡ No olvides reemplazar 'TU_PROJECT_ID' con tu ID de proyecto real en Program.cs !!!");
             }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError inesperado al llamar a la API de Gemini: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine("Stack Trace: " + ex.StackTrace);
            Console.WriteLine("\nAsegúrate de haber reemplazado 'TU_PROJECT_ID' con tu ID de proyecto real.");
            Console.WriteLine("Verifica también que la API 'Vertex AI API' esté habilitada en tu proyecto de Google Cloud.");
            Console.WriteLine("Y confirma que has configurado las credenciales (ADC) correctamente ejecutando 'gcloud auth application-default login'.");
        }
    }


    // Modificado para ser async Task
    public static async Task Main(string[] args)
    {
        // Asume que config.json está en el directorio de ejecución (bin/Debug/...)
        // Podrías hacerlo más robusto buscando en el directorio del proyecto también.
        string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Office miOficina = null;

        try
        {
            // --- Cargar Configuración desde JSON ---
            Console.WriteLine($"Intentando cargar configuración desde '{configFilePath}'...");
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                OfficeConfig config = JsonSerializer.Deserialize<OfficeConfig>(jsonString, options);

                if (config != null)
                {
                    miOficina = new Office(config.Name ?? "Oficina Sin Nombre");
                    foreach (var roomConfig in config.Rooms ?? new List<RoomConfig>())
                    {
                        Room newRoom = new Room(roomConfig.Name ?? "Habitación Sin Nombre", roomConfig.IsWork);
                        miOficina.AddRoom(newRoom);
                        foreach (var sensorConfig in roomConfig.Sensors ?? new List<SensorConfig>())
                        {
                            Sensor newSensor = CreateSensorFromConfig(sensorConfig, newRoom);
                            newRoom.AddSensor(newSensor); // Método AddSensor debe verificar null
                        }
                        foreach (var readerConfig in roomConfig.Readers ?? new List<ReaderConfig>())
                        {
                            Reader newReader = CreateReaderFromConfig(readerConfig, newRoom);
                            newRoom.AddReader(newReader); // Método AddReader debe verificar null
                        }
                    }
                    Console.WriteLine("Configuración cargada exitosamente.");
                }
                else
                {
                    Console.WriteLine("Error: El archivo JSON está vacío o tiene un formato incorrecto.");
                }
            }
            else
            {
                Console.WriteLine($"Error: Archivo de configuración '{configFilePath}' no encontrado.");
            }
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"Error al procesar el archivo JSON: {jsonEx.Message}");
            if (jsonEx.InnerException != null) Console.WriteLine($"  Inner Exception: {jsonEx.InnerException.Message}");
        }
        catch (FileNotFoundException)
        {
             Console.WriteLine($"Error: Archivo de configuración '{configFilePath}' no encontrado.");
        }
        catch (Exception ex) // Captura otras posibles excepciones (ej. permisos)
        {
            Console.WriteLine($"Ocurrió un error inesperado al cargar la configuración: {ex.GetType().Name} - {ex.Message}");
        }

        // Si la carga falló por cualquier motivo, crear una oficina por defecto
        if (miOficina == null)
        {
            Console.WriteLine("No se pudo cargar la configuración, usando oficina por defecto.");
            miOficina = CreateDefaultOffice();
        }

        // --- Bucle de Simulación ---
        Console.WriteLine("\nIniciando Simulación de Oficina (Presiona 'Q' para salir)...");
        CancellationTokenSource cts = new CancellationTokenSource(); // Para cancelar la simulación limpiamente

        // Ejecutar la simulación en un hilo separado para no bloquear la entrada de teclado
        var simulationTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                 try
                 {
                     miOficina.SimulateUpdateAll();
                     miOficina.DisplayFullStatus();
                     await Task.Delay(2000, cts.Token); // Esperar o salir si se cancela
                 }
                 catch (TaskCanceledException)
                 {
                     break; // Salir del bucle si la tarea fue cancelada
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"\nError durante la simulación: {ex.Message}");
                     // Podrías decidir si continuar o detener la simulación aquí
                     await Task.Delay(2000); // Esperar antes de reintentar o continuar
                 }
            }
            Console.WriteLine("Hilo de simulación detenido.");
        }, cts.Token);

        // Esperar a que se presione 'Q' en el hilo principal
        while (!simulationTask.IsCompleted && !simulationTask.IsFaulted)
        {
             if (Console.KeyAvailable)
             {
                 var key = Console.ReadKey(intercept: true).Key;
                 if (key == ConsoleKey.Q)
                 {
                     Console.WriteLine("\nDeteniendo simulación...");
                     cts.Cancel(); // Señal para que el hilo de simulación termine
                     break;
                 }
             }
             await Task.Delay(100); // Pequeña pausa para no consumir CPU innecesariamente
        }

        // Asegurarse de que la tarea de simulación termine
        try
        {
            await simulationTask;
        }
        catch (TaskCanceledException)
        {
            // Esperado si se presionó Q
        }
        catch (Exception ex)
        {
             Console.WriteLine($"\nError final en la tarea de simulación: {ex.Message}");
        }

        Console.WriteLine("Simulación terminada.");


        // --- Llamada a Gemini --- 
        // Solo intentar llamar a Gemini si la configuración inicial fue exitosa (o se usó default)
        if (miOficina != null)
        {
            string geminiPrompt = "Describe brevemente las ventajas de usar sensores en una oficina inteligente.";
            await CallGeminiAsync(geminiPrompt); // Llamar al método async
        }
        else
        {
             Console.WriteLine("\nNo se pudo inicializar la oficina, omitiendo llamada a Gemini.");
        }


        Console.WriteLine("\nPrograma finalizado. Presiona cualquier tecla para salir.");
        Console.ReadKey(); // Pausar antes de cerrar la consola
    }
}