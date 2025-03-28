using System.Collections.Generic; 
using System.Text.Json.Serialization; 


public class Sensor
{
    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("updateSensor")]
    public int UpdateSensor { get; set; }

    [JsonPropertyName("values")]
    public List<double> Values { get; set; }

   
    public Sensor()
    {
        
        this.Location = "";
        this.Id = "";
        this.Capacity = 0;
        this.UpdateSensor = 0;
        this.Values = new List<double>();
    }
}