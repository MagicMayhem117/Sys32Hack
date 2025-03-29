using System;
using System.Collections.Generic; // Necesario para List<> si no estaba ya
using System.IO;                  // Necesario para File
using System.Linq;
using System.Text.Json;           // Necesario para JsonSerializer
using System.Threading;
using Google.Cloud.AIPlatform.V1;
using System.Threading.Tasks;


public class Program
{

    public static async Task<string> TextInput(
        string projectId = "748989473373",
        string location = "us-central1",
        string publisher = "google",
        string model = "gemini-1.5-flash-001",
        string prompt_IA = "")
    {
        var clientBuilder = new PredictionServiceClientBuilder
        {
            Endpoint = $"{location}-aiplatform.googleapis.com"
        };

        var predictionServiceClient = clientBuilder.Build();

        try
        {
            string prompt = $"En lenguaje natural, genera un reporte del estado de la oficina y sus recursos, incluyendo los trabajadores, con base en la siguiente información: '{File.ReadAllText("config.json")}', '****** ESTADO OFICINA: Corporativo Central ******\r\nHora: 2025-03-29 08:04:45\r\n======================================\r\n--- Estado Habitación: Recepción (Trabajo: False) ---  [Temperatura] ID: TEMP-REC-01 @ Recepción -> Valor: 15.4°C (calefaccion encendida)\r\n  [Movimiento] ID: MOT-REC-01 @ Recepción -> Valor: Detectado - Las luces están prendidas\r\n  [Tarjeta] ID: CARD-REC-01 @ Recepción -> Valor: Empleado Luis Martínez detectad@ fuera de zona de trabajo\r\n\r\n--- Estado Habitación: Sala de Juntas Alfa (Trabajo: True) ---  [Temperatura] ID: TEMP-SJ-01 @ Sala de Juntas Alfa -> Valor: 24.6°C (aire acondicionado encendido)\r\n  [Movimiento] ID: MOT-SJ-01 @ Sala de Juntas Alfa -> Valor: No Detectado - Las luces están apagadas\r\n  [Impresora] ID: PRN-SJ-01 @ Sala de Juntas Alfa -> Valor: En uso\r\n  [Tarjeta] ID: CARD-SJ-01 @ Sala de Juntas Alfa -> Valor: No Detectada\r\n\r\n--- Estado Habitación: Area Común (Trabajo: False) ---  [Temperatura] ID: TEMP-AC-01 @ Area Común -> Valor: 19.7°C (temperatura estable)\\r\\n  [Movimiento] ID: MOT-AC-01 @ Area Común -> Valor: Detectado - Las luces están prendidas\r\n  [Impresora] ID: PRN-AC-01 @ Area Común -> Valor: En Uso\r\n  [Tarjeta] ID: CARD-AC-01 @ Area Común -> Valor: Empleado Sofia Rodriguez detectad@ fuera de zona de trabajo\r\n\r\n--- Estado Habitación: Cubículos Beta (Trabajo: True) ---  [Movimiento] ID: MOT-CB-01 @ Cubículos Beta -> Valor: No Detectado - Las luces están apagadas\r\n  [Impresora] ID: PRN-CB-01 @ Cubículos Beta -> Valor: Libre\r\n  [Tarjeta] ID: CARD-CB-01 @ Cubículos Beta -> Valor: No Detectada\r\n\r\n======================================'";

            var generateContentRequest = new GenerateContentRequest
            {
                Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
                Contents =
                {
                    new Content
                    {
                        Role = "USER",
                        Parts =
                        {
                            new Part { Text = prompt }
                        }
                    }
                }
            };

            GenerateContentResponse response = await predictionServiceClient.GenerateContentAsync(generateContentRequest);

            if (response?.Candidates?.Count > 0 &&
                response.Candidates[0].Content?.Parts?.Count > 0)
            {
                string responseText = response.Candidates[0].Content.Parts[0].Text;
                Console.WriteLine(responseText);
                return responseText;
            }
            else
            {
                Console.WriteLine("No valid response was received.");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return string.Empty;
        }

    }
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


    public static async Task Main(string[] args)
    {
        string configFilePath = "config.json";
        Office miOficina = null;

        try
        {
            // --- Cargar Configuración desde JSON ---
            Console.WriteLine($"Intentando cargar configuración desde '{configFilePath}'...");
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);

                // Opciones para ser un poco más flexible con el JSON (opcional)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Permite que "name" en JSON mapee a "Name" en C#
                };

                OfficeConfig config = JsonSerializer.Deserialize<OfficeConfig>(jsonString, options);

                if (config != null)
                {
                    miOficina = new Office(config.Name ?? "Oficina Sin Nombre"); // Usar nombre del JSON o default

                    foreach (var roomConfig in config.Rooms ?? new List<RoomConfig>())
                    {
                        // Crear la habitación PRIMERO para poder pasarla a los constructores de sensores/lectores
                        Room newRoom = new Room(roomConfig.Name ?? "Habitación Sin Nombre", roomConfig.IsWork);
                        miOficina.AddRoom(newRoom); // Añadirla a la oficina

                        // Crear y añadir sensores desde la configuración
                        foreach (var sensorConfig in roomConfig.Sensors ?? new List<SensorConfig>())
                        {
                            Sensor newSensor = CreateSensorFromConfig(sensorConfig, newRoom);
                            newRoom.AddSensor(newSensor); // El método Add ya verifica si es null
                        }

                        // Crear y añadir lectores desde la configuración
                        foreach (var readerConfig in roomConfig.Readers ?? new List<ReaderConfig>())
                        {
                            Reader newReader = CreateReaderFromConfig(readerConfig, newRoom);
                            newRoom.AddReader(newReader); // El método Add ya verifica si es null
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

        }
        catch (Exception ex) // Captura otras posibles excepciones (ej. permisos de archivo)
        {
            Console.WriteLine($"Ocurrió un error inesperado al cargar la configuración: {ex.Message}");
        }

        // Si la carga falló por cualquier motivo, crear una oficina por defecto
        if (miOficina == null)
        {
            miOficina = CreateDefaultOffice();
        }

        // --- Bucle de Simulación (sin cambios) ---
        Console.WriteLine("\nIniciando Simulación de Oficina...");
        Thread.Sleep(1500); // Un poco más de tiempo para leer los mensajes de carga

        bool exit = false;
        while (!exit)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Q)
                {
                    exit = true;
                }
            }

            if (!exit) // Solo simular y mostrar si no vamos a salir
            {
                miOficina.SimulateUpdateAll();
                // Console.WriteLine(miOficina.DisplayFullStatus());
                Thread.Sleep(2000);
                string prompt = $"Escribe un reporte con base en la siguiente información: {File.ReadAllText("config.json")}, {miOficina.DisplayFullStatus()}";
                Console.WriteLine(miOficina.DisplayFullStatus());
                await TextInput();


            }
        }

        Console.WriteLine("\nSimulación terminada.");
    }
}