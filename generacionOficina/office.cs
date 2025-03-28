// --- Sensor Base ---
public abstract class Sensor
{
    public string Id { get; }
    public string Location { get; } // E.g., "Sala de Juntas", "Escritorio 5"
    public abstract string Type { get; }
    public abstract object GetValue(); // Object allows different value types
    public abstract void SimulateUpdate(); // Logic to change the value

    protected Sensor(string id, string location)
    {
        Id = id;
        Location = location;
    }

    public override string ToString()
    {
        return $"[{Type}] ID: {Id} @ {Location} -> Valor: {GetValue()}";
    }
}
public abstract class reader
{
    public string Id { get; }
    public string Location { get; } 
    public abstract object GetValue(); 
    public abstract void SimulateUpdate();
    protected reader(string id, string location)
    {
        Id = id;
        Location = location;
    }
    public override string ToString()
    {
        return $"ID: {Id} @ {Location} -> Valor: {GetValue()}";
    }
}

// --- Specific Sensor Types ---
public class TemperatureSensor : Sensor
{
    public override string Type => "Temperatura";
    public double CurrentTemperature { get; private set; }
    private Random _random = new Random();
    public string airCon {  get; private set; }

    public TemperatureSensor(string id, string location, double initialTemp = 21.0) : base(id, location)
    {
        CurrentTemperature = initialTemp;
        airCon = 0;
    }

    public override object GetValue() => $"{CurrentTemperature:F1}°C"; // Format to 1 decimal place

    public override string air()
    {
        if (CurrentTemperature < 16)
        {
            airCon = 'calefaccion encendida';
            return airCon;
        }
        else if(CurrentTemperature>23)
        {
            airCon = 'aire acondicionado encendido';
            return airCon;
        }
        else
        {
            airCon = 'temperatura estable';
            return airCon;
        }        
    }

    public override void SimulateUpdate()
    {
        // Simulate slight fluctuation
        double change = (_random.NextDouble() * 2.0) - 1.0; // Change between -1.0 and +1.0
        CurrentTemperature += change * 0.2; // Small change
        CurrentTemperature = Math.Clamp(CurrentTemperature, 8.0, 30.0); // Keep within bounds
        air();
    }
}
public class Reader : reader
{ 
    public string Type => "Tarjeta";
    public bool CardDetected { get; private set; }
    private Random _random = new Random();
    public Reader(string id, Room location) : base(id, location) { }

    public object GetValue() => CardDetected ? "Detectada" : "No Detectada";

    public void SimulateUpdate()
    {
        // Simulate random card detection (e.g., 10% chance each update)
        if (_random.NextDouble() < 0.1)
        {
            CardDetected = true;
            
            // Optional: Add logic to automatically turn off after a while
        }
        else
        {
            // Have a chance to turn off if already on
            if (CardDetected && _random.NextDouble() < 0.3)
            {
                CardDetected = false;
            }
        }
        // If not triggered, it stays in its current state unless turned off above
    }
}

public class PrinterSensor : Sensor
{
    public override string Type => "Impresora";
    public bool PrinterDetected { get; private set; }
    private Random _random = new Random();

    public PrinterSensor(string id, string location) : base(id, location) { }

    public override object GetValue() => PrinterDetected ? "Detectada" : "No Detectada";

    public override void SimulateUpdate()
    {
        // Simulate random motion detection (e.g., 10% chance each update)
        if (_random.NextDouble() < 0.1)
        {
            PrinterDetected = true;
            // Optional: Add logic to automatically turn off after a while
        }
        else
        {
            // Have a chance to turn off if already on
            if (PrinterDetected && _random.NextDouble() < 0.3)
            {
                PrinterDetected = false;
            }
        }
        // If not triggered, it stays in its current state unless turned off above
    }

}


public class MotionSensor : Sensor
{
    public override string Type => "Movimiento";
    public bool MotionDetected { get; private set; }
    private Random _random = new Random();

    public MotionSensor(string id, string location) : base(id, location) { }

    public override object GetValue() => MotionDetected ? "Detectado" : "No Detectado";

    public override void SimulateUpdate()
    {
        // Simulate random motion detection (e.g., 10% chance each update)
        if (_random.NextDouble() < 0.1)
        {
            MotionDetected = true;
            // Optional: Add logic to automatically turn off after a while
        }
        else
        {
            // Have a chance to turn off if already on
            if (MotionDetected && _random.NextDouble() < 0.3)
            {
                MotionDetected = false;
            }
        }
        // If not triggered, it stays in its current state unless turned off above
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
        Sensors.Add(sensor);
    }

    public void AddReader(Reader reader)
    {
        Readers.Add(reader);
    }

    public void SimulateUpdateSensors()
    {
        foreach (var sensor in Sensors)
        {
            sensor.SimulateUpdate();
        }
    }

    public void DisplayStatus()
    {
        Console.WriteLine($"--- Estado Habitación: {Name} ---");
        if (!Sensors.Any())
        {
            Console.WriteLine(" (Sin sensores)");
            return;
        }
        foreach (var sensor in Sensors)
        {
            Console.WriteLine($"  {sensor}");
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
            room.SimulateUpdateSensors();
        }
    }

    public void DisplayFullStatus()
    {
        Console.Clear(); // Clear console for fresh display
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
            Console.WriteLine(); // Add empty line for spacing
        }
        Console.WriteLine("======================================");
        Console.WriteLine("Presiona 'Q' para salir...");
    }
}