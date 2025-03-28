using System;
using System.Threading;
using System.Linq; // Needed for .Any()

public class Program
{
    public static void Main(string[] args)
    {
        // 1. Crear la Oficina
        Office miOficina = new Office("Corporativo XYZ");

        // 2. Crear Habitaciones
        Room recepcion = new Room("Recepción");
        Room salaJuntas = new Room("Sala de Juntas");
        Room areaComun = new Room("Área Común");

        // 3. Crear y Añadir Sensores a las Habitaciones
        recepcion.AddSensor(new TemperatureSensor("TEMP-REC-01", "Recepción"));
        recepcion.AddSensor(new MotionSensor("MOT-REC-01", "Recepción"));

        salaJuntas.AddSensor(new TemperatureSensor("TEMP-SJ-01", "Sala de Juntas", 22.5));
        salaJuntas.AddSensor(new MotionSensor("MOT-SJ-01", "Sala de Juntas"));
        // Podrías añadir un sensor de ocupación, luz, etc.

        areaComun.AddSensor(new TemperatureSensor("TEMP-AC-01", "Área Común", 20.0));
        areaComun.AddSensor(new MotionSensor("MOT-AC-01", "Área Común"));
        areaComun.AddSensor(new MotionSensor("MOT-AC-02", "Área Común - Cafetería"));


        // 4. Añadir Habitaciones a la Oficina
        miOficina.AddRoom(recepcion);
        miOficina.AddRoom(salaJuntas);
        miOficina.AddRoom(areaComun);


        // 5. Bucle de Simulación e Interacción
        Console.WriteLine("Iniciando Simulación de Oficina...");
        Thread.Sleep(1000);

        bool exit = false;
        while (!exit)
        {
            if (Console.KeyAvailable) // Check if a key was pressed
            {
                var key = Console.ReadKey(intercept: true).Key; // Read key without showing it
                if (key == ConsoleKey.Q)
                {
                    exit = true;
                }
            }

            // Actualizar el estado de todos los sensores
            miOficina.SimulateUpdateAll();

            // Mostrar el estado actual en la consola
            miOficina.DisplayFullStatus();

            // Esperar un poco antes de la siguiente actualización
            if (!exit)
                Thread.Sleep(2000); // Espera 2 segundos
        }

        Console.WriteLine("\nSimulación terminada.");
    }
}