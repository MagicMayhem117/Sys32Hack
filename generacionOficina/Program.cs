using System.Security.Cryptography.X509Certificates;
using System;
using System.Text.Json;

namespace generacionOficina()
{
    class Program
{
    static void Main(string[] args)
    {
        string ruta = "sensores.json";
        if (File.Exists(ruta))
        {
            string json = File.ReadAllText(ruta);
            List<sensor> sensores = JsonSerializer.Deserialize<List<sensor>>(json);
            foreach (var sensor  in sensores)
            {
                Console.WriteLine("Ubicacion: " + sensor.location);
                Console.WriteLine("ID: " + sensor.id);
                Console.WriteLine("Capacidad: " + sensor.capacity);
                Console.WriteLine("Actualizacion : " + sensor.updateSensor + " segundos");
                Console.WriteLine("Valores: " + sensor.values);
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("El archivo no existe");
        }
        

    }
}
}
