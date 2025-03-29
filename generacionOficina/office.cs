using System;
using System.Collections.Generic;
using System.IO; // Necesario para StreamReader y File handling
using System.Linq;
using Newtonsoft.Json; // Asegúrate de tener este paquete NuGet instalado
using System.Text.Json;

// --- DTOs para Deserialización ---
public class OfficeConfig
{
    public string Name { get; set; }
    public List<RoomConfig> Rooms { get; set; }
}

public class RoomConfig
{
    public string Name { get; set; }
    public bool IsWork { get; set; }
    public List<SensorConfig> Sensors { get; set; }
    public List<ReaderConfig> Readers { get; set; }
}

public class SensorConfig
{
    public string Id { get; set; }
    public string Type { get; set; } // Para identificar qué sensor crear
    public double? InitialTemp { get; set; } // Nullable por si no es un TempSensor
}

public class ReaderConfig
{
    public string Id { get; set; }
    public string Type { get; set; } // Para identificar qué lector crear
}

// --- Clases Principales ---

// --- Sensor Base ---
public abstract class Sensor
{
    public string Id { get; }
    public Room Location { get; }
    public abstract string Type { get; }
    public abstract object GetValue();
    public abstract void SimulateUpdate();

    protected Sensor(string id, Room location)
    {
        Id = id;
        Location = location;
    }

    public override string ToString()
    {
        return $"[{Type}] ID: {Id} @ {Location?.Name ?? "N/A"} -> Valor: {GetValue()}";
    }
}

// --- Reader Base ---
public abstract class Reader
{
    public string Id { get; }
    public Room Location { get; }
    public abstract string Type { get; }
    public abstract object GetValue();
    public abstract void SimulateUpdate();

    protected Reader(string id, Room location)
    {
        Id = id;
        Location = location;
    }

    public override string ToString()
    {
        return $"[{Type}] ID: {Id} @ {Location?.Name ?? "N/A"} -> Valor: {GetValue()}";
    }
}

// --- Specific Sensor Types ---
public class TemperatureSensor : Sensor
{
    public override string Type => "Temperatura";
    public double CurrentTemperature { get; private set; }
    private Random _random = new Random();
    public string airCon { get; private set; }

    public TemperatureSensor(string id, Room location, double initialTemp = 21.0) : base(id, location)
    {
        CurrentTemperature = initialTemp;
        airCon = air();
    }

    public override object GetValue() => $"{CurrentTemperature:F1}°C";

    public string air()
    {
        if (CurrentTemperature < 16)
        {
            airCon = "calefaccion encendida";
        }
        else if (CurrentTemperature > 23)
        {
            airCon = "aire acondicionado encendido";
        }
        else
        {
            airCon = "temperatura estable";
        }
        return airCon;
    }

    public override void SimulateUpdate()
    {
        double change = (_random.NextDouble() * 2.0) - 1.0;
        CurrentTemperature += change * 0.2;
        CurrentTemperature = Math.Clamp(CurrentTemperature, 8.0, 30.0);
        air();
    }
}

// --- Clase Empleado y lógica de carga ---
public class Empleado
{
    public int id { get; set; }
    public string nombre { get; set; }
    // Cambiado a double para mayor precisión con TimeSpan
    public double horas_trabajo_hoy { get; set; }
    public double horas_trabajo_semana { get; set; }
    public double start_count { get; set; }
    // La propiedad start_count parece reemplazada por la lógica de DateTime en CardReader

    public static List<Empleado> LoadEmployees(string filePath)
    {
        // Comprobar si el archivo existe antes de intentar leerlo
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ADVERTENCIA: El archivo de empleados '{filePath}' no se encontró. Se devolverá una lista vacía.");
            return new List<Empleado>(); // Devuelve lista vacía para evitar error
        }
        using (StreamReader r = new StreamReader(filePath))
        {
            string json = r.ReadToEnd();
            // Añadir manejo de errores por si el JSON está mal formado
            try
            {
                List<Empleado> empleados = JsonConvert.DeserializeObject<List<Empleado>>(json);
                return empleados ?? new List<Empleado>(); // Asegurarse de no devolver null
            }
            catch (System.Text.Json.JsonException ex)
            {
                Console.WriteLine($"ERROR: El archivo JSON de empleados '{filePath}' está mal formado. {ex.Message}");
                return new List<Empleado>(); // Devuelve lista vacía
            }
        }
    }

    public static string GetNameById(List<Empleado> empleados, int id)
    {
        Empleado empleado = empleados?.FirstOrDefault(e => e.id == id);
        if (empleado != null)
        {
            return empleado.nombre;
        }
        else
        {
            // Es mejor no lanzar una excepción aquí si puede pasar que un ID no exista temporalmente
            Console.WriteLine($"ADVERTENCIA: Empleado con ID {id} no encontrado en la lista cargada.");
            return $"ID {id} Desconocido"; // O devolver un valor indicativo
        }
    }

    // --- Método para guardar empleados (Ejemplo) ---
    public static void SaveEmployees(string filePath, List<Empleado> empleados)
    {
        try
        {
            string json = JsonConvert.SerializeObject(empleados, Formatting.Indented); // Indented para legibilidad
            File.WriteAllText(filePath, json);
            Console.WriteLine($"INFO: Datos de empleados guardados en {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: No se pudo guardar el archivo de empleados '{filePath}'. {ex.Message}");
        }
    }
}

// --- Card Reader Corregido ---
public class CardReader : Reader
{
    public override string Type => "Tarjeta";
    public bool CardDetected { get; private set; }
    private Random _random = new Random();
    public string nombre = "";
    private int? detectedEmployeeId = null;
    private double countStart = 0.0; // Para rastrear cuándo entró a zona de trabajo
    private double horas_trabajo_hoy = 0.0;

    public static string filePath = "Empleados.json"; // Ruta al archivo JSON
    public List<Empleado> empleados; // Lista de empleados (se carga en constructor)

    public CardReader(string id, Room location) : base(id, location)
    {
        // Cargar empleados de forma segura al crear el lector
        try
        {
            empleados = Empleado.LoadEmployees(filePath);
            Console.WriteLine($"INFO: Cargados {empleados.Count} empleados para lector {id}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: Lector {id}: No se pudo cargar {filePath}. Funcionalidad de empleados limitada. Error: {ex.Message}");
            empleados = new List<Empleado>(); // Inicializar como lista vacía para evitar NullReferenceExceptions
        }
    }

    public override void SimulateUpdate()
    {
        if (!CardDetected && _random.NextDouble() < 0.01) // Simulación de Detección
        {
            CardDetected = true;
            if (empleados != null && empleados.Any())
            {
                int randomIndex = _random.Next(empleados.Count);
                detectedEmployeeId = empleados[randomIndex].id;
                countStart = empleados[randomIndex].start_count;
                nombre = Empleado.GetNameById(empleados, detectedEmployeeId.Value);
                Console.WriteLine($"DEBUG: Lector {Id} detectó al empleado: {nombre} (ID: {detectedEmployeeId.Value})");

                if (this.Location != null && this.Location.isWork) // Iniciar contador SI está en zona de trabajo
                {
                    if (countStart == 0)
                    {
                        string currentCount = DateTime.Now.ToString("HH:mm");
                        int horas = Int32.Parse(currentCount.Substring(0, 2));
                        int minutos = Int32.Parse(currentCount.Substring(3));
                        countStart = horas + (minutos / 60);
                        empleados[randomIndex].start_count = countStart;
                    }
                }
                else
                {
                    string countFinal = DateTime.Now.ToString("HH:mm");
                    int horas = Int32.Parse(countFinal.Substring(0, 2));
                    int minutos = Int32.Parse(countFinal.Substring(3));
                    double horaFinal = horas + (minutos / 60);
                    horas_trabajo_hoy += (horaFinal - countStart);
                    empleados[randomIndex].horas_trabajo_hoy += horas_trabajo_hoy;
                    countStart = 0.0;
                    empleados[randomIndex].start_count = 0.0;
                    Console.WriteLine($"DEBUG: Lector {Id} detectó tarjeta, pero no hay empleados cargados.");
                }
                string jsonString = System.Text.Json.JsonSerializer.Serialize(empleados, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("Empleados.json", jsonString);
            }
        }
        else if (CardDetected) // Simulación de Fin de Detección
        {

            // Limpiar estado actual del lector
            CardDetected = false;
            nombre = "";
            detectedEmployeeId = null;
        }
        
    }

    public string empleado()
    {
        if (CardDetected)
        {
            if (this.Location != null && this.Location.isWork)
            {
                string estadoTiempo = (countStart != 0) ? "(Trabajando)" : "(Entrando)"; // Si countStart tiene valor, está registrando tiempo
                return $"Empleado {nombre} detectad@ en zona de trabajo {estadoTiempo}";
            }
            else
            {
                return $"Empleado {nombre} detectad@ fuera de zona de trabajo";
            }
        }
        else
        {
            return "Empleado no detectado";
        }
    }

    public override object GetValue() => CardDetected ? $"Detectada ({empleado()})" : "No Detectada";
}


public class PrinterSensor : Sensor
{
    public override string Type => "Impresora";
    public bool PrinterDetected { get; private set; }
    private Random _random = new Random();
    private int level = 100;

    public PrinterSensor(string id, Room location) : base(id, location) { }

    public override object GetValue()
    {
        if(PrinterDetected && level > 0)
        {
            return "En uso";
        } else if(level <= 0)
        {
            return "Sin tinta";
        } else
        {
            return "Libre";
        }
    }

    public override void SimulateUpdate()
    {
        if (!PrinterDetected && _random.NextDouble() < 0.02 && level > 0)
        {
            PrinterDetected = true;
        }
        else if (PrinterDetected && _random.NextDouble() < 0.20)
        {
            PrinterDetected = false;
        } else if(level <0)
        {
            PrinterDetected = false;
        }
        if (PrinterDetected)
        {
            level -= 5;
        }
    }
}


public class MotionSensor : Sensor
{
    public override string Type => "Movimiento";
    public bool MotionDetected { get; private set; }
    private Random _random = new Random();
    public string lights = "Las luces están apagadas";
    public bool lights_on { get; private set; }

    public MotionSensor(string id, Room location) : base(id, location) { }

    public override object GetValue() => MotionDetected ? "Detectado" : "No Detectado";

    public override void SimulateUpdate()
    {
        if (!MotionDetected && _random.NextDouble() < 0.1)
        {
            MotionDetected = true;
            lights = "Las luces están prendidas";
            lights_on = true;
        }
        else if (MotionDetected && _random.NextDouble() < 0.1)
        {
            MotionDetected = false;
            lights = "Las luces están apagadas";
            lights_on = false;
        }
    }
}

// --- Room/Area ---
public class Room
{
    public string Name { get; }
    public bool isWork { get; }
    public List<Sensor> Sensors { get; } = new List<Sensor>();
    public List<Reader> Readers { get; } = new List<Reader>();

    public Room(string name, bool iswork)
    {
        Name = name;
        isWork = iswork;
    }

    public void AddSensor(Sensor sensor)
    {
        if (sensor != null) Sensors.Add(sensor);
    }

    public void AddReader(Reader reader)
    {
        if (reader != null) Readers.Add(reader);
    }

    public void SimulateUpdateDevices()
    {
        foreach (var sensor in Sensors)
        {
            sensor.SimulateUpdate();
        }
        foreach (var reader in Readers)
        {
            reader.SimulateUpdate();
        }
    }

    public string DisplayStatus()
    {
        string statusSensores = "";
        statusSensores += $"--- Estado Habitación: {Name} (Trabajo: {isWork}) ---";
        if (!Sensors.Any() && !Readers.Any())
        {
            statusSensores += " (Sin dispositivos)";
            return statusSensores;
        }

        foreach (var sensor in Sensors)
        {
            if (sensor is TemperatureSensor tempSensor)
            {
                statusSensores += $"  {sensor} ({tempSensor.air()})\n";
            } else if(sensor is MotionSensor motSensor)
            {
                statusSensores += $"  {sensor} - {motSensor.lights}\n";
            }
            else
            {
                statusSensores += $"  {sensor}\n";
            }
        }

        foreach (var reader in Readers)
        {
            statusSensores += $"  {reader}\n";
        }

        return statusSensores;
    }
}

// --- Office ---
public class Office
{
    public string Name { get; }
    public List<Room> Rooms { get; } = new List<Room>();

    public Office(string name)
    {
        Name = name;
    }

    public void AddRoom(Room room)
    {
        Rooms.Add(room);
    }

    public void SimulateUpdateAll()
    {
        foreach (var room in Rooms)
        {
            room.SimulateUpdateDevices();
        }
    }

    public string DisplayFullStatus()
    {
        string statusFinal = "";
        // Podrías añadir Console.Clear() aquí si quieres limpiar la pantalla en cada actualización
        statusFinal += $"****** ESTADO OFICINA: {Name} ******\nHora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n======================================\n";
        if (!Rooms.Any())
        {
            statusFinal += " (Sin habitaciones definidas)";
            return statusFinal;
        }
        foreach (var room in Rooms)
        {
            statusFinal += $"{room.DisplayStatus()}\n";
        }
        statusFinal += "======================================\n";

        return statusFinal;
        // Asumiendo que tienes un bucle principal que lee la tecla Q para salir
    }

    public void SaveAllEmployeeData()
    {
        // Necesitamos acceder a la lista de empleados de algún lector.
        // Asumimos que todos los CardReader comparten/acceden a la misma fuente de datos actualizada.
        // Podríamos tomar la lista del primer CardReader encontrado.
        CardReader firstReader = Rooms.SelectMany(r => r.Readers)
                                       .OfType<CardReader>()
                                       .FirstOrDefault();

        if (firstReader != null && firstReader.empleados != null)
        {
            Console.WriteLine("INFO: Guardando estado final de los empleados...");
            Empleado.SaveEmployees(CardReader.filePath, firstReader.empleados);
        }
        else
        {
            Console.WriteLine("ADVERTENCIA: No se encontraron CardReaders o lista de empleados para guardar.");
        }
    }
}
