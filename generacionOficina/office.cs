using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization; // Necesario para atributos si los usaras (no estrictamente en este enfoque DTO)

// --- DTOs para Deserialización ---
// Estas clases reflejan la estructura del JSON
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
    // Añadir aquí otras propiedades específicas si otros sensores las necesitan
}

public class ReaderConfig
{
    public string Id { get; set; }
    public string Type { get; set; } // Para identificar qué lector crear
                                     // Añadir aquí otras propiedades específicas si otros lectores las necesitan
}


// --- Clases Principales (Ajustes menores) ---

// --- Sensor Base ---
public abstract class Sensor
{
    public string Id { get; }
    public Room Location { get; } // Mantenemos la referencia al objeto Room
    public abstract string Type { get; }
    public abstract object GetValue();
    public abstract void SimulateUpdate();

    protected Sensor(string id, Room location) // El constructor sigue igual
    {
        Id = id;
        Location = location;
    }

    public override string ToString()
    {
        // Usamos Location.Name ya que Location es un objeto Room
        return $"[{Type}] ID: {Id} @ {Location?.Name ?? "N/A"} -> Valor: {GetValue()}";
    }
}

// --- Reader Base ---
public abstract class Reader
{
    public string Id { get; }
    public Room Location { get; }
    public abstract string Type { get; } // Agregamos Type aquí también para consistencia
    public abstract object GetValue();
    public abstract void SimulateUpdate();

    protected Reader(string id, Room location)
    {
        Id = id;
        Location = location;
    }

    public override string ToString()
    {
        // Usamos Location.Name
        return $"[{Type}] ID: {Id}  {Location?.Name ?? "N/A"} -> Valor: {GetValue()}";
    }
}

// --- Specific Sensor Types ---
public class TemperatureSensor : Sensor
{
    public override string Type => "Temperatura";
    public double CurrentTemperature { get; private set; }
    private Random _random = new Random();
    public string airCon { get; private set; }

    // Constructor ajustado para recibir initialTemp
    public TemperatureSensor(string id, Room location, double initialTemp = 21.0) : base(id, location)
    {
        CurrentTemperature = initialTemp;
        airCon = air(); // Establecer estado inicial del AC
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
        air(); // Actualizar estado del AC
    }
}

public class CardReader : Reader // Asegúrate que hereda de Reader
{
    public override string Type => "Tarjeta"; // Implementa la propiedad abstracta Type
    public bool CardDetected { get; private set; }
    private Random _random = new Random();
    private countStart; // Equivaler countStart al start_count del empleado
    DateTime tiempo = DateTime.Now();

    public CardReader(string id, Room location) : base(id, location) { }

    // El método empleado tenía una dependencia estática a Room.isWork
    // Ahora debería usar la propiedad Location.isWork del objeto Room actual
    public string empleado()
    {
        if (CardDetected)
        {
            // Accede a isWork a través de la propiedad Location que es el Room
            if (this.Location != null && this.Location.isWork)
            {
                if(countStart == 0)
                {
                    // Cambiar start_count a tiempo
                }
                return "Empleado detectado en zona de trabajo";
            }
            else
            {
                if (countStart != 0)
                {
                    // Cambiar horas_trabajo_hoy a horas_trabajo_hoy + tiempo - start_count
                    // start_count = 0
                }
                return "Tarjeta detectada fuera de zona de trabajo";
            }
        }
        else
        {
            return "Empleado no detectado";
        }
    }

    public override object GetValue() => CardDetected ? $"Detectada ({empleado()})" : "No Detectada"; // Muestra estado empleado

    public override void SimulateUpdate()
    {
        // Probabilidad más baja de detectar y más alta de dejar de detectar para simular paso rápido
        if (!CardDetected && _random.NextDouble() < 0.05) // 5% chance to detect
        {
            CardDetected = true;
        }
        else if (CardDetected && _random.NextDouble() < 0.8) // 80% chance to clear detection
        {
            CardDetected = false;
        }
    }
}


public class PrinterSensor : Sensor
{
    public override string Type => "Impresora";
    public bool PrinterDetected { get; private set; } // Simula si alguien está usando la impresora
    private Random _random = new Random();

    public PrinterSensor(string id, Room location) : base(id, location) { }

    public override object GetValue() => PrinterDetected ? "En uso" : "Libre"; // Mensaje más claro

    public override void SimulateUpdate()
    {
        // Probabilidad baja de empezar a usar, probabilidad más alta de dejar de usar
        if (!PrinterDetected && _random.NextDouble() < 0.02) // 2% chance to start using
        {
            PrinterDetected = true;
        }
        else if (PrinterDetected && _random.NextDouble() < 0.20) // 20% chance to stop using
        {
            PrinterDetected = false;
        }
    }
}


public class MotionSensor : Sensor // Sin cambios necesarios aquí
{
    public override string Type => "Movimiento";
    public bool MotionDetected { get; private set; }
    private Random _random = new Random();

    public MotionSensor(string id, Room location) : base(id, location) { }

    public override object GetValue() => MotionDetected ? "Detectado" : "No Detectado";

    public override void SimulateUpdate()
    {
        if (!MotionDetected && _random.NextDouble() < 0.1) // 10% chance to detect
        {
            MotionDetected = true;
        }
        else if (MotionDetected && _random.NextDouble() < 0.3) // 30% chance to stop detecting
        {
            MotionDetected = false;
        }
    }
}

// --- Room/Area ---
public class Room
{
    public string Name { get; }
    public bool isWork { get; }
    public List<Sensor> Sensors { get; } = new List<Sensor>();
    public List<Reader> Readers { get; } = new List<Reader>(); // Cambiado a List<Reader> para generalizar

    public Room(string name, bool iswork)
    {
        Name = name;
        isWork = iswork;
    }

    public void AddSensor(Sensor sensor)
    {
        if (sensor != null) Sensors.Add(sensor);
    }

    public void AddReader(Reader reader) // Cambiado a aceptar Reader genérico
    {
        if (reader != null) Readers.Add(reader);
    }

    public void SimulateUpdateDevices() // Renombrado para incluir ambos
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

    public void DisplayStatus()
    {
        Console.WriteLine($"--- Estado Habitación: {Name} (Trabajo: {isWork}) ---");
        if (!Sensors.Any() && !Readers.Any()) // Comprobar ambas listas
        {
            Console.WriteLine(" (Sin dispositivos)");
            return;
        }
        // Mostrar Sensores
        foreach (var sensor in Sensors)
        {
            if (sensor is TemperatureSensor tempSensor)
            {
                Console.WriteLine($"  {sensor} ({tempSensor.air()})"); // Mostrar estado AC junto al sensor
            }
            else
            {
                Console.WriteLine($"  {sensor}");
            }
        }
        // Mostrar Lectores
        foreach (var reader in Readers)
        {
            Console.WriteLine($"  {reader}"); // El ToString() del CardReader ya incluye info de empleado
        }
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
            // Llamar al método actualizado que simula todos los dispositivos
            room.SimulateUpdateDevices();
        }
    }

    public void DisplayFullStatus() // Sin cambios necesarios aquí, ya llama a Room.DisplayStatus
    {
        Console.Clear();
        Console.WriteLine($"****** ESTADO OFICINA: {Name} ******");
        Console.WriteLine($"Hora: {DateTime.Now}");
        Console.WriteLine("======================================");
        if (!Rooms.Any())
        {
            Console.WriteLine(" (Sin habitaciones definidas)");
            return;
        }
        foreach (var room in Rooms)
        {
            room.DisplayStatus();
            Console.WriteLine();
        }
        Console.WriteLine("======================================");
        Console.WriteLine("Presiona 'Q' para salir...");
    }
}