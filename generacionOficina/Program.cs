using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace generacionOficina
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string ruta = "sensores.json";

            if (File.Exists(ruta))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(ruta);
                    List<Sensor> sensores = JsonSerializer.Deserialize<List<Sensor>>(json);

                    if (sensores != null)
                    {
                        ProcesarSensores(sensores);
                    }
                    else
                    {
                        Console.WriteLine("El archivo JSON está vacío o no es una lista válida de sensores.");
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Error al procesar el archivo JSON: {jsonEx.Message}");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Error al leer el archivo: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"El archivo no existe en la ruta: {Path.GetFullPath(ruta)}");
            }
        }

        static void ProcesarSensores(List<Sensor> sensores)
        {
            foreach (var sensor in sensores)
            {
                Console.WriteLine("Ubicacion: " + sensor.Location);
                Console.WriteLine("ID: " + sensor.Id);
                Console.WriteLine("Capacidad: " + sensor.Capacity);
                Console.WriteLine("Actualizacion: " + sensor.UpdateSensor + " segundos");
                Console.WriteLine("Valores: " + (sensor.Values != null ? string.Join(", ", sensor.Values) : "N/A"));
                Console.WriteLine();
            }
        }
    }

    public class Sensor
    {
        public string Location { get; set; }
        public int Id { get; set; }
        public int Capacity { get; set; }
        public int UpdateSensor { get; set; }
        public List<int> Values { get; set; }
    }
}
